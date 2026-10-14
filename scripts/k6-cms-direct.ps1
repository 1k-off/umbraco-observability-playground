param(
  [string]$CmsBaseUrl = $env:K6_CMS_BASE_URL,
  [switch]$UsePodman
)
& "$PSScriptRoot\k6-run.ps1" -Script 'cms-direct.js' -CmsBaseUrl $CmsBaseUrl -UsePodman:$UsePodman
