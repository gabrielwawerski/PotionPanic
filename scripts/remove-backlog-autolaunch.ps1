Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$startupDirectory = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Startup'
$shortcutPaths = @(
    (Join-Path $startupDirectory 'PotionPanic - Start Backlog Server.lnk'),
    (Join-Path $startupDirectory 'PotionPanic - Open Backlog Board.lnk')
)
$removedShortcut = $false

foreach ($shortcutPath in $shortcutPaths) {
    if (-not (Test-Path -LiteralPath $shortcutPath)) {
        continue
    }

    Remove-Item -LiteralPath $shortcutPath
    Write-Host "Removed Startup shortcut: $shortcutPath"
    $removedShortcut = $true
}

if (-not $removedShortcut) {
    Write-Host "No Startup shortcut found at any known autolaunch path in $startupDirectory"
}
