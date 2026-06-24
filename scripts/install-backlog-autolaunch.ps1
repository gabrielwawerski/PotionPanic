Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = Split-Path -Parent $scriptDirectory
$launcherScript = Join-Path $scriptDirectory 'backlog-ui.ps1'
$startupDirectory = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Startup'
$shortcutPath = Join-Path $startupDirectory 'PotionPanic - Open Backlog Board.lnk'
$powershellPath = (Get-Command -Name 'powershell.exe' -ErrorAction Stop).Source
$arguments = '-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File "{0}"' -f $launcherScript

if (-not (Test-Path -LiteralPath $launcherScript)) {
    throw "The launcher script was not found at $launcherScript."
}

if (-not (Test-Path -LiteralPath $startupDirectory)) {
    New-Item -ItemType Directory -Path $startupDirectory | Out-Null
}

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $powershellPath
$shortcut.Arguments = $arguments
$shortcut.WorkingDirectory = $repositoryRoot
$shortcut.WindowStyle = 7
$shortcut.Description = 'Open the PotionPanic Backlog board at sign-in.'
$shortcut.IconLocation = "$powershellPath,0"
$shortcut.Save()

Write-Host "Installed Startup shortcut: $shortcutPath"
