[CmdletBinding()]
param(
  [switch]$Uninstall,
  [string]$StartupFolderPath
)

$ErrorActionPreference = "Stop"

$canonicalShortcutName = "PotionPanic - Start Docs Server.lnk"
$legacyShortcutNames = @(
  "PotionPanic - Start Backlog Server.lnk",
  "PotionPanic - Open Backlog Board.lnk"
)
$allShortcutNames = @($canonicalShortcutName) + $legacyShortcutNames

function Test-IsWindowsPlatform {
  return [Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT
}

function Get-DocsRepoRoot {
  return (Resolve-Path (Join-Path $PSScriptRoot "..\\..")).Path
}

function Get-StartupDirectoryPath {
  param(
    [string]$ConfiguredPath
  )

  if ([string]::IsNullOrWhiteSpace($ConfiguredPath)) {
    return [Environment]::GetFolderPath("Startup")
  }

  return [IO.Path]::GetFullPath($ConfiguredPath)
}

function Get-EncodedStartupCommand {
  param(
    [string]$RepoRoot
  )

  $escapedRepoRoot = $RepoRoot.Replace("'", "''")
  $command = @'
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath '__REPO_ROOT__'
$npmCommand = (Get-Command npm.cmd -ErrorAction Stop).Source
& $npmCommand 'run' 'docs:dev'
'@.Trim().Replace("__REPO_ROOT__", $escapedRepoRoot)

  return [Convert]::ToBase64String(
    [Text.Encoding]::Unicode.GetBytes($command)
  )
}

function Remove-DocsStartupShortcuts {
  param(
    [string]$StartupPath
  )

  foreach ($shortcutName in $allShortcutNames) {
    $shortcutPath = Join-Path $StartupPath $shortcutName
    if (Test-Path -LiteralPath $shortcutPath) {
      Remove-Item -LiteralPath $shortcutPath -Force
    }
  }
}

function Install-DocsStartupShortcut {
  param(
    [string]$StartupPath,
    [string]$RepoRoot
  )

  New-Item -ItemType Directory -Path $StartupPath -Force | Out-Null
  Remove-DocsStartupShortcuts -StartupPath $StartupPath

  $encodedCommand = Get-EncodedStartupCommand -RepoRoot $RepoRoot
  $powershellPath = (Get-Command powershell.exe -ErrorAction Stop).Source
  $shortcutPath = Join-Path $StartupPath $canonicalShortcutName
  $shell = New-Object -ComObject WScript.Shell
  $shortcut = $shell.CreateShortcut($shortcutPath)

  $shortcut.TargetPath = $powershellPath
  $shortcut.Arguments = "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -EncodedCommand $encodedCommand"
  $shortcut.Description = "Starts the PotionPanic docs server at Windows sign-in."
  $shortcut.WorkingDirectory = $RepoRoot
  $shortcut.Save()
}

if (-not (Test-IsWindowsPlatform)) {
  throw "The docs Windows startup installer only supports Windows."
}

$startupPath = Get-StartupDirectoryPath -ConfiguredPath $StartupFolderPath

if ($Uninstall) {
  if (Test-Path -LiteralPath $startupPath) {
    Remove-DocsStartupShortcuts -StartupPath $startupPath
  }
  Write-Host "Removed docs startup shortcut(s) from $startupPath"
  exit 0
}

$repoRoot = Get-DocsRepoRoot
Install-DocsStartupShortcut -StartupPath $startupPath -RepoRoot $repoRoot
Write-Host "Installed docs startup shortcut at $(Join-Path $startupPath $canonicalShortcutName)"
