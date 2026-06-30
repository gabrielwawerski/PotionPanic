[CmdletBinding()]
param(
  [int]$Port = 6420
)

$ErrorActionPreference = "Stop"

function Test-IsWindowsPlatform {
  return [Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT
}

if (-not (Test-IsWindowsPlatform)) {
  throw "The docs stop command only supports Windows."
}

$processIds = @(
  Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
    Select-Object -ExpandProperty OwningProcess -Unique
)

if ($processIds.Count -eq 0) {
  Write-Host "No process is listening on port $Port."
  exit 0
}

foreach ($processId in $processIds) {
  Stop-Process -Id $processId -Force -ErrorAction Stop
}

$deadline = (Get-Date).AddSeconds(5)

do {
  Start-Sleep -Milliseconds 100
  $remainingProcessIds = @(
    Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
      Select-Object -ExpandProperty OwningProcess -Unique
  )
} while ($remainingProcessIds.Count -gt 0 -and (Get-Date) -lt $deadline)

if ($remainingProcessIds.Count -gt 0) {
  throw "Timed out waiting for port $Port to close."
}

Write-Host "Stopped process(es) on port ${Port}: $($processIds -join ', ')"
