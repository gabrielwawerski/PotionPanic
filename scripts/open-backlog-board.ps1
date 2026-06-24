Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$canonicalLauncher = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) 'backlog-ui.ps1'

if (-not (Test-Path -LiteralPath $canonicalLauncher)) {
    Write-Error "The canonical browser UI launcher was not found at $canonicalLauncher."
    exit 1
}

& $canonicalLauncher
exit $LASTEXITCODE
