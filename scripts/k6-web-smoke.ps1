param(
  [string]$WebBaseUrl = $env:K6_WEB_BASE_URL,
  [switch]$UsePodman
)
& "$PSScriptRoot\k6-run.ps1" -Script 'web-smoke.js' -WebBaseUrl $WebBaseUrl -UsePodman:$UsePodman
