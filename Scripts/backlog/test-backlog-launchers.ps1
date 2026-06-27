Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-True {
    param(
        [Parameter(Mandatory = $true)]
        [bool]$Condition,
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Assert-Equal {
    param(
        [Parameter(Mandatory = $true)]
        $Expected,
        [Parameter(Mandatory = $true)]
        $Actual,
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    if ($Expected -ne $Actual) {
        throw "$Message Expected: <$Expected>. Actual: <$Actual>."
    }
}

function Assert-FileContains {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Pattern,
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    $content = Get-Content -Raw -LiteralPath $Path
    if ($content -notmatch $Pattern) {
        throw $Message
    }
}

function Invoke-LauncherScenario {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ScriptPath,
        [Parameter(Mandatory = $true)]
        [bool]$InitiallyReady
    )

    $tempDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("PotionPanic-BacklogLauncherTest-" + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $tempDirectory | Out-Null

    $fakeBacklogPath = Join-Path $tempDirectory 'backlog.ps1'
    Set-Content -LiteralPath $fakeBacklogPath -Value 'exit 0'

    $global:BacklogLauncherTestState = [pscustomobject]@{
        BrowserOpenCalls = 0
        ServerReady = $InitiallyReady
        ServerStartCalls = 0
        SleepCalls = 0
        StartProcessCalls = [System.Collections.Generic.List[object]]::new()
        WebRequestCalls = [System.Collections.Generic.List[string]]::new()
    }
    $global:BacklogLauncherFakeCommand = Microsoft.PowerShell.Core\Get-Command -Name $fakeBacklogPath

    try {
        function global:Get-Command {
            param(
                [Parameter(Mandatory = $true)]
                [string]$Name
            )

            switch ($Name) {
                'backlog' {
                    return $global:BacklogLauncherFakeCommand
                }
                'powershell.exe' {
                    return Microsoft.PowerShell.Core\Get-Command -Name 'powershell.exe'
                }
                default {
                    return Microsoft.PowerShell.Core\Get-Command -Name $Name
                }
            }
        }

        function global:Invoke-WebRequest {
            param(
                [Parameter(Mandatory = $true)]
                [string]$Uri,
                [switch]$UseBasicParsing,
                [int]$TimeoutSec
            )

            $null = $UseBasicParsing
            $null = $TimeoutSec
            $global:BacklogLauncherTestState.WebRequestCalls.Add($Uri)

            if ($global:BacklogLauncherTestState.ServerReady) {
                return [pscustomobject]@{ StatusCode = 200 }
            }

            throw "Not ready: $Uri"
        }

        function global:Start-Process {
            param(
                [Parameter(Mandatory = $true)]
                [string]$FilePath,
                [object]$ArgumentList,
                [string]$WorkingDirectory,
                [System.Diagnostics.ProcessWindowStyle]$WindowStyle,
                [switch]$PassThru
            )

            $call = [pscustomobject]@{
                ArgumentList = @($ArgumentList)
                FilePath = $FilePath
                PassThru = $PassThru.IsPresent
                WindowStyle = $WindowStyle
                WorkingDirectory = $WorkingDirectory
            }
            $global:BacklogLauncherTestState.StartProcessCalls.Add($call)

            if ($FilePath -like 'http*') {
                $global:BacklogLauncherTestState.BrowserOpenCalls++
            } else {
                $global:BacklogLauncherTestState.ServerReady = $true
                $global:BacklogLauncherTestState.ServerStartCalls++
            }

            if ($PassThru) {
                return [pscustomobject]@{ HasExited = $false }
            }
        }

        function global:Start-Sleep {
            param(
                [int]$Milliseconds
            )

            $null = $Milliseconds
            $global:BacklogLauncherTestState.SleepCalls++
        }

        & $ScriptPath
        return $global:BacklogLauncherTestState
    } finally {
        Remove-Item Function:\global\Get-Command -ErrorAction SilentlyContinue
        Remove-Item Function:\global\Invoke-WebRequest -ErrorAction SilentlyContinue
        Remove-Item Function:\global\Start-Process -ErrorAction SilentlyContinue
        Remove-Item Function:\global\Start-Sleep -ErrorAction SilentlyContinue
        Remove-Variable -Name BacklogLauncherFakeCommand -Scope Global -ErrorAction SilentlyContinue
        Remove-Variable -Name BacklogLauncherTestState -Scope Global -ErrorAction SilentlyContinue

        if (Test-Path -LiteralPath $tempDirectory) {
            Remove-Item -LiteralPath $tempDirectory -Recurse -Force
        }
    }
}

$backlogAutolaunchPath = Join-Path $PSScriptRoot 'backlog-autolaunch.ps1'
$backlogUiPath = Join-Path $PSScriptRoot 'backlog-ui.ps1'
$installScriptPath = Join-Path $PSScriptRoot 'install-backlog-autolaunch.ps1'
$removeScriptPath = Join-Path $PSScriptRoot 'remove-backlog-autolaunch.ps1'

Assert-True -Condition (Test-Path -LiteralPath $backlogUiPath) -Message 'The manual Backlog UI launcher should exist.'
Assert-True -Condition (Test-Path -LiteralPath $backlogAutolaunchPath) -Message 'The server-only autolaunch script should exist.'

$manualState = Invoke-LauncherScenario -ScriptPath $backlogUiPath -InitiallyReady:$false
Assert-Equal -Expected 1 -Actual $manualState.ServerStartCalls -Message 'The manual launcher should start the Backlog browser server when it is not ready.'
Assert-Equal -Expected 1 -Actual $manualState.BrowserOpenCalls -Message 'The manual launcher should open the Backlog browser URL after the server is ready.'

$autolaunchState = Invoke-LauncherScenario -ScriptPath $backlogAutolaunchPath -InitiallyReady:$false
Assert-Equal -Expected 1 -Actual $autolaunchState.ServerStartCalls -Message 'The autolaunch script should start the Backlog browser server when it is not ready.'
Assert-Equal -Expected 0 -Actual $autolaunchState.BrowserOpenCalls -Message 'The autolaunch script must not open the browser automatically.'

Assert-FileContains -Path $installScriptPath -Pattern 'backlog-autolaunch\.ps1' -Message 'The autolaunch installer should target the server-only launcher script.'
Assert-FileContains -Path $removeScriptPath -Pattern 'PotionPanic - Open Backlog Board\.lnk' -Message 'The autolaunch remover should still be able to clean up the legacy shortcut name.'

Write-Host 'All backlog launcher checks passed.'
