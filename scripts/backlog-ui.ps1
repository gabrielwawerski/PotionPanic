Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-BacklogUiPort {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConfigPath
    )

    if (-not (Test-Path -LiteralPath $ConfigPath)) {
        return 6420
    }

    $match = Select-String -Path $ConfigPath -Pattern '^\s*default_port:\s*(\d+)\s*$'
    if ($null -eq $match) {
        return 6420
    }

    return [int]$match.Matches[0].Groups[1].Value
}

function Test-BacklogUiReady {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Url
    )

    try {
        $null = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 2
        return $true
    } catch {
        return $false
    }
}

function Start-BacklogBrowserServer {
    param(
        [Parameter(Mandatory = $true)]
        [System.Management.Automation.CommandInfo]$BacklogCommand,
        [Parameter(Mandatory = $true)]
        [string]$RepositoryRoot,
        [Parameter(Mandatory = $true)]
        [int]$Port
    )

    $backlogPath = $BacklogCommand.Source
    $backlogExtension = [System.IO.Path]::GetExtension($backlogPath)
    if ($BacklogCommand.CommandType -eq [System.Management.Automation.CommandTypes]::ExternalScript -or $backlogExtension -ieq '.ps1') {
        $powershellPath = (Get-Command -Name 'powershell.exe' -ErrorAction Stop).Source
        return Start-Process `
            -FilePath $powershellPath `
            -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $backlogPath, 'browser', '--no-open', '--port', $Port) `
            -WorkingDirectory $RepositoryRoot `
            -WindowStyle Hidden `
            -PassThru
    }

    return Start-Process `
        -FilePath $backlogPath `
        -ArgumentList @('browser', '--no-open', '--port', $Port) `
        -WorkingDirectory $RepositoryRoot `
        -WindowStyle Hidden `
        -PassThru
}

$backlogCommand = Get-Command -Name 'backlog' -ErrorAction SilentlyContinue
if ($null -eq $backlogCommand) {
    Write-Error 'The backlog CLI is not installed. Run `npm i -g backlog.md` or `.\scripts\setup-backlog.ps1` first.'
    exit 1
}

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = Split-Path -Parent $scriptDirectory
$configPath = Join-Path $repositoryRoot 'backlog.config.yml'
$port = Get-BacklogUiPort -ConfigPath $configPath
$url = "http://localhost:$port"

if (-not (Test-BacklogUiReady -Url $url)) {
    $process = Start-BacklogBrowserServer `
        -BacklogCommand $backlogCommand `
        -RepositoryRoot $repositoryRoot `
        -Port $port

    $deadline = (Get-Date).AddSeconds(15)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 500

        if (Test-BacklogUiReady -Url $url) {
            break
        }

        if ($process.HasExited) {
            throw "The Backlog browser server exited before $url became available."
        }
    }

    if (-not (Test-BacklogUiReady -Url $url)) {
        throw "Timed out waiting for the Backlog browser UI at $url."
    }
}

Start-Process -FilePath $url
