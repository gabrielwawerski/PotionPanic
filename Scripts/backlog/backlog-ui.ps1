Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$replacementScript = Join-Path (Split-Path -Parent $PSScriptRoot) 'docs-ui.ps1'

Write-Warning 'Backlog.md has been retired for this repository. Redirecting to the VitePress docs board.'
& $replacementScript
exit $LASTEXITCODE
