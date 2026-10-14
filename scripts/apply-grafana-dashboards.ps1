<#
.SYNOPSIS
  Create/update Grafana dashboard ConfigMaps for the sidecar (label grafana_dashboard=1).
  Does not re-encode JSON (avoids PowerShell ConvertTo-Json corruption).
#>
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$dir = Join-Path $repoRoot 'observability\grafana\dashboards'
$ns = 'observability'

if (-not (Test-Path $dir)) {
  throw "Dashboard dir not found: $dir"
}

Get-ChildItem -Path $dir -Filter '*.json' | ForEach-Object {
  $base = $_.BaseName
  $cmName = "grafana-dashboard-$base"
  Write-Host "Applying ConfigMap/$cmName"

  # Apply raw file bytes via kubectl (no PowerShell JSON round-trip)
  kubectl create configmap $cmName `
    -n $ns `
    --from-file="$($_.Name)=$($_.FullName)" `
    --dry-run=client -o yaml |
    kubectl apply -f -

  kubectl label configmap $cmName -n $ns grafana_dashboard=1 --overwrite | Out-Null
  kubectl annotate configmap $cmName -n $ns grafana_folder=Demo --overwrite | Out-Null
}

Write-Host "Done. Sidecar watches label grafana_dashboard=1 in namespace $ns."
