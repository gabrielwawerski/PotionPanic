Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$replacementScript = Join-Path (Split-Path -Parent $PSScriptRoot) 'setup-docs.ps1'

Write-Warning 'Backlog.md has been retired for this repository. Redirecting to the VitePress docs setup workflow.'
& $replacementScript
exit $LASTEXITCODE
