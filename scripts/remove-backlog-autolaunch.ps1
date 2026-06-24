Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$startupDirectory = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Startup'
$shortcutPath = Join-Path $startupDirectory 'PotionPanic - Open Backlog Board.lnk'

if (-not (Test-Path -LiteralPath $shortcutPath)) {
    Write-Host "No Startup shortcut found at $shortcutPath"
    exit 0
}

Remove-Item -LiteralPath $shortcutPath
Write-Host "Removed Startup shortcut: $shortcutPath"
