# Deploy observability stack (Phase 4). Requires kubectl, helm 3.2+.
# Usage: .\install.ps1 [-SkipHelm]
param(
    [switch]$SkipHelm
)

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot
$Ns = "observability"

Write-Host "==> Namespace"
kubectl apply -f "$Root/namespace.yaml"

Write-Host "==> Secrets (edit minio/secret.yaml, openmaxio/secret.yaml, grafana/secret.yaml before first run)"
kubectl apply -f "$Root/minio/secret.yaml" -n $Ns
kubectl apply -f "$Root/openmaxio/secret.yaml" -n $Ns
kubectl apply -f "$Root/grafana/secret.yaml" -n $Ns

Write-Host "==> MinIO"
kubectl apply -f "$Root/minio/pvc.yaml" -n $Ns
kubectl apply -f "$Root/minio/deployment.yaml" -n $Ns
kubectl apply -f "$Root/minio/service.yaml" -n $Ns

Write-Host "==> Wait for MinIO"
kubectl -n $Ns wait --for=condition=available deployment/minio --timeout=300s

Write-Host "==> Create S3 buckets"
kubectl delete job/minio-create-buckets -n $Ns --ignore-not-found 2>$null
kubectl apply -f "$Root/minio/job-buckets.yaml" -n $Ns
kubectl -n $Ns wait --for=condition=complete job/minio-create-buckets --timeout=120s

Write-Host "==> OpenMaxIO Console (MinIO UI)"
kubectl apply -f "$Root/openmaxio/deployment.yaml" -n $Ns
kubectl apply -f "$Root/openmaxio/service.yaml" -n $Ns
kubectl apply -f "$Root/openmaxio/ingressroute.yaml" -n $Ns
kubectl -n $Ns wait --for=condition=available deployment/openmaxio --timeout=300s

if ($SkipHelm) {
    Write-Host "==> Skipping Helm (-SkipHelm)"
    exit 0
}

Write-Host "==> Helm repos"
helm repo add grafana https://grafana.github.io/helm-charts 2>$null
helm repo add grafana-community https://grafana-community.github.io/helm-charts 2>$null
helm repo update

$HelmDir = Join-Path $Root "helm"

Write-Host "==> Mimir"
helm upgrade --install mimir grafana/mimir-distributed -n $Ns -f "$HelmDir/values-mimir.yaml" --wait --timeout 15m

Write-Host "==> Loki"
helm upgrade --install loki grafana/loki -n $Ns -f "$HelmDir/values-loki.yaml" --wait --timeout 10m

Write-Host "==> Tempo"
helm upgrade --install tempo grafana-community/tempo -n $Ns -f "$HelmDir/values-tempo.yaml" --wait --timeout 10m

Write-Host "==> Pyroscope"
helm upgrade --install pyroscope grafana/pyroscope -n $Ns -f "$HelmDir/values-pyroscope.yaml" --wait --timeout 10m

Write-Host "==> Grafana"
helm upgrade --install grafana grafana-community/grafana -n $Ns -f "$HelmDir/values-grafana.yaml" --wait --timeout 10m

Write-Host "==> Alloy"
helm upgrade --install alloy grafana/alloy -n $Ns -f "$HelmDir/values-alloy-daemonset.yaml" --wait --timeout 5m
helm upgrade --install alloy-gateway grafana/alloy -n $Ns -f "$HelmDir/values-alloy-gateway.yaml" --wait --timeout 5m

kubectl apply -f "$Root/alloy/gateway-service.yaml" -n $Ns
kubectl apply -f "$Root/grafana/ingressroute.yaml" -n $Ns

Write-Host ""
Write-Host "Done. Verify: kubectl -n $Ns get pods"
Write-Host "Public URLs (after DNS/nginx): https://demo-grafana.1node.tech, https://demo-openmaxio.1node.tech"
