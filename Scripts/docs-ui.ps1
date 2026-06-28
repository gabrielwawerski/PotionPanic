Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'docs-browser-server.ps1')

$context = Ensure-DocsBrowserServer -EntryScriptPath $MyInvocation.MyCommand.Path
Start-Process -FilePath $context.Url
