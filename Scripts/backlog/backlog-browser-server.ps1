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

function Get-BacklogBrowserContext {
    param(
        [Parameter(Mandatory = $true)]
        [string]$EntryScriptPath
    )

    $backlogCommand = Get-Command -Name 'backlog' -ErrorAction SilentlyContinue
    if ($null -eq $backlogCommand) {
        throw 'The backlog CLI is not installed. Run `npm i -g backlog.md` or `.\scripts\setup-backlog.ps1` first.'
    }

    $scriptDirectory = Split-Path -Parent $EntryScriptPath
    $repositoryRoot = Split-Path -Parent $scriptDirectory
    $configPath = Join-Path $repositoryRoot 'backlog.config.yml'
    $port = Get-BacklogUiPort -ConfigPath $configPath

    return [pscustomobject]@{
        BacklogCommand = $backlogCommand
        Port = $port
        RepositoryRoot = $repositoryRoot
        Url = "http://localhost:$port"
    }
}

function Ensure-BacklogBrowserServer {
    param(
        [Parameter(Mandatory = $true)]
        [string]$EntryScriptPath
    )

    $context = Get-BacklogBrowserContext -EntryScriptPath $EntryScriptPath
    if (-not (Test-BacklogUiReady -Url $context.Url)) {
        $process = Start-BacklogBrowserServer `
            -BacklogCommand $context.BacklogCommand `
            -RepositoryRoot $context.RepositoryRoot `
            -Port $context.Port

        $deadline = (Get-Date).AddSeconds(15)
        while ((Get-Date) -lt $deadline) {
            Start-Sleep -Milliseconds 500

            if (Test-BacklogUiReady -Url $context.Url) {
                break
            }

            if ($process.HasExited) {
                throw "The Backlog browser server exited before $($context.Url) became available."
            }
        }

        if (-not (Test-BacklogUiReady -Url $context.Url)) {
            throw "Timed out waiting for the Backlog browser UI at $($context.Url)."
        }
    }

    return $context
}
