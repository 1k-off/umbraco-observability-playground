<#
.SYNOPSIS
  Install node-exporter + kube-state-metrics into observability and refresh Alloy RBAC/config.
#>
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$cm = Join-Path $repoRoot 'deployment\k8s\observability\cluster-metrics'
$obs = Join-Path $repoRoot 'deployment\k8s\observability'
$ns = 'observability'

helm repo add prometheus-community https://prometheus-community.github.io/helm-charts 2>$null
helm repo update

Write-Host "==> node-exporter"
helm upgrade --install node-exporter prometheus-community/prometheus-node-exporter `
  -n $ns -f (Join-Path $cm 'values-node-exporter.yaml') --wait --timeout 5m

Write-Host "==> kube-state-metrics"
helm upgrade --install kube-state-metrics prometheus-community/kube-state-metrics `
  -n $ns -f (Join-Path $cm 'values-kube-state-metrics.yaml') --wait --timeout 5m

Write-Host "==> Alloy cAdvisor RBAC"
kubectl apply -f (Join-Path $obs 'alloy\rbac-cadvisor.yaml')

Write-Host "==> Alloy DaemonSet (cadvisor + exporter scrape)"
helm upgrade --install alloy grafana/alloy -n $ns `
  -f (Join-Path $obs 'helm\values-alloy-daemonset.yaml') --wait --timeout 5m

Write-Host "==> Grafana dashboards"
& (Join-Path $repoRoot 'scripts\apply-grafana-dashboards.ps1')

Write-Host "Done. Verify in Explore: node_cpu_seconds_total, kube_pod_status_phase, container_memory_working_set_bytes"
