Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'backlog-browser-server.ps1')

$null = Ensure-BacklogBrowserServer -EntryScriptPath $MyInvocation.MyCommand.Path
