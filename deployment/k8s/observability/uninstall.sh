#!/usr/bin/env bash
# Remove observability Helm releases and namespace resources.
set -euo pipefail

NS="observability"

helm uninstall alloy-gateway -n "${NS}" 2>/dev/null || true
helm uninstall alloy -n "${NS}" 2>/dev/null || true
helm uninstall grafana -n "${NS}" 2>/dev/null || true
helm uninstall pyroscope -n "${NS}" 2>/dev/null || true
helm uninstall tempo -n "${NS}" 2>/dev/null || true
helm uninstall loki -n "${NS}" 2>/dev/null || true
helm uninstall mimir -n "${NS}" 2>/dev/null || true

kubectl delete -f "$(dirname "$0")/" -n "${NS}" --ignore-not-found || true
kubectl delete namespace "${NS}" --ignore-not-found

echo "Observability stack removed."
