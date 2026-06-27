Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'backlog-browser-server.ps1')

$context = Ensure-BacklogBrowserServer -EntryScriptPath $MyInvocation.MyCommand.Path
Start-Process -FilePath $context.Url
