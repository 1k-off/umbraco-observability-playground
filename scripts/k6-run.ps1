<#
.SYNOPSIS
  Run a k6 script from load/k6 (local k6 or Podman grafana/k6).
#>
param(
  [Parameter(Mandatory = $true)]
  [string]$Script,

  [string]$CmsBaseUrl = $env:K6_CMS_BASE_URL,
  [string]$WebBaseUrl = $env:K6_WEB_BASE_URL,
  [switch]$UsePodman,
  [string[]]$ExtraArgs = @()
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$k6Dir = Join-Path $repoRoot 'load\k6'
$scriptPath = Join-Path $k6Dir $Script

if (-not (Test-Path $scriptPath)) {
  throw "k6 script not found: $scriptPath"
}

if (-not $CmsBaseUrl) { $CmsBaseUrl = 'https://demo-cms.1node.tech' }
if (-not $WebBaseUrl) { $WebBaseUrl = 'https://demo-umbr-fe.1node.tech' }

$k6Cmd = Get-Command k6 -ErrorAction SilentlyContinue
if ($UsePodman -or -not $k6Cmd) {
  $podman = Get-Command podman -ErrorAction SilentlyContinue
  if (-not $podman) {
    throw 'k6 not on PATH and podman not found. Install k6 or Podman.'
  }
  Write-Host "Running via podman grafana/k6: $Script"
  $mount = "${k6Dir}:/scripts:ro"
  & podman run --rm -i `
    -v $mount `
    -w /scripts `
    -e "K6_CMS_BASE_URL=$CmsBaseUrl" `
    -e "K6_WEB_BASE_URL=$WebBaseUrl" `
    docker.io/grafana/k6:latest `
    run $Script @ExtraArgs
  exit $LASTEXITCODE
}

Write-Host "Running k6: $Script"
Push-Location $k6Dir
try {
  $env:K6_CMS_BASE_URL = $CmsBaseUrl
  $env:K6_WEB_BASE_URL = $WebBaseUrl
  & k6 run $Script @ExtraArgs
  exit $LASTEXITCODE
}
finally {
  Pop-Location
}
