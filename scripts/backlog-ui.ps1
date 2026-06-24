Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$backlogCommand = Get-Command -Name 'backlog' -ErrorAction SilentlyContinue
if ($null -eq $backlogCommand) {
    Write-Error 'The backlog CLI is not installed. Run `npm i -g backlog.md` or `.\scripts\setup-backlog.ps1` first.'
    exit 1
}

& backlog board

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
