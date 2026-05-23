#!/usr/bin/env bash
# Deploy observability stack (Phase 4). Requires: kubectl, helm 3.2+, cluster access.
# Usage: ./install.sh [--skip-helm]   (from deployment/k8s/observability/)
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
NS="observability"
SKIP_HELM=false

for arg in "$@"; do
  case "$arg" in
    --skip-helm) SKIP_HELM=true ;;
    *) echo "Unknown arg: $arg"; exit 1 ;;
  esac
done

echo "==> Namespace"
kubectl apply -f "${ROOT}/namespace.yaml"

echo "==> Secrets (edit minio/secret.yaml, openmaxio/secret.yaml, grafana/secret.yaml before first run)"
kubectl apply -f "${ROOT}/minio/secret.yaml" -n "${NS}"
kubectl apply -f "${ROOT}/openmaxio/secret.yaml" -n "${NS}"
kubectl apply -f "${ROOT}/grafana/secret.yaml" -n "${NS}"

echo "==> MinIO"
kubectl apply -f "${ROOT}/minio/pvc.yaml" -n "${NS}"
kubectl apply -f "${ROOT}/minio/deployment.yaml" -n "${NS}"
kubectl apply -f "${ROOT}/minio/service.yaml" -n "${NS}"

echo "==> Wait for MinIO"
kubectl -n "${NS}" wait --for=condition=available deployment/minio --timeout=300s

echo "==> Create S3 buckets"
kubectl delete job/minio-create-buckets -n "${NS}" --ignore-not-found
kubectl apply -f "${ROOT}/minio/job-buckets.yaml" -n "${NS}"
kubectl -n "${NS}" wait --for=condition=complete job/minio-create-buckets --timeout=120s

echo "==> OpenMaxIO Console (MinIO UI)"
kubectl apply -f "${ROOT}/openmaxio/deployment.yaml" -n "${NS}"
kubectl apply -f "${ROOT}/openmaxio/service.yaml" -n "${NS}"
kubectl apply -f "${ROOT}/openmaxio/ingressroute.yaml" -n "${NS}"
kubectl -n "${NS}" wait --for=condition=available deployment/openmaxio --timeout=300s

if [[ "${SKIP_HELM}" == "true" ]]; then
  echo "==> Skipping Helm (--skip-helm)"
  exit 0
fi

echo "==> Helm repos"
helm repo add grafana https://grafana.github.io/helm-charts
helm repo add grafana-community https://grafana-community.github.io/helm-charts
helm repo update

HELM_DIR="${ROOT}/helm"

echo "==> Mimir"
helm upgrade --install mimir grafana/mimir-distributed \
  -n "${NS}" \
  -f "${HELM_DIR}/values-mimir.yaml" \
  --wait --timeout 15m

echo "==> Loki"
helm upgrade --install loki grafana/loki \
  -n "${NS}" \
  -f "${HELM_DIR}/values-loki.yaml" \
  --wait --timeout 10m

echo "==> Tempo"
helm upgrade --install tempo grafana-community/tempo \
  -n "${NS}" \
  -f "${HELM_DIR}/values-tempo.yaml" \
  --wait --timeout 10m

echo "==> Pyroscope"
helm upgrade --install pyroscope grafana/pyroscope \
  -n "${NS}" \
  -f "${HELM_DIR}/values-pyroscope.yaml" \
  --wait --timeout 10m

echo "==> Grafana"
helm upgrade --install grafana grafana-community/grafana \
  -n "${NS}" \
  -f "${HELM_DIR}/values-grafana.yaml" \
  --wait --timeout 10m

echo "==> Alloy (DaemonSet + gateway)"
helm upgrade --install alloy grafana/alloy \
  -n "${NS}" \
  -f "${HELM_DIR}/values-alloy-daemonset.yaml" \
  --wait --timeout 5m

helm upgrade --install alloy-gateway grafana/alloy \
  -n "${NS}" \
  -f "${HELM_DIR}/values-alloy-gateway.yaml" \
  --wait --timeout 5m

kubectl apply -f "${ROOT}/alloy/gateway-service.yaml" -n "${NS}"

echo "==> Grafana IngressRoute"
kubectl apply -f "${ROOT}/grafana/ingressroute.yaml" -n "${NS}"

echo ""
echo "Done. Verify:"
echo "  kubectl -n ${NS} get pods"
echo "  kubectl -n ${NS} port-forward svc/grafana 3000:80"
echo "  curl -sS http://localhost:3000/api/health"
echo ""
echo "Public URLs (after DNS/nginx): https://demo-grafana.1node.tech, https://demo-openmaxio.1node.tech"
