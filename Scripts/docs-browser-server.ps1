Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-NpmCommandPath
{
    $commands = @('npm.cmd', 'npm')

    foreach ($commandName in $commands)
    {
        $command = Get-Command -Name $commandName -ErrorAction SilentlyContinue
        if ($null -ne $command)
        {
            return $command.Source
        }
    }

    throw 'npm is required for the VitePress docs workflow. Run `.\Scripts\setup-docs.ps1` first.'
}

function Test-DocsUiReady
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$Url
    )

    try
    {
        $null = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 2
        return $true
    }
    catch
    {
        return $false
    }
}

function Start-DocsBrowserServer
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$NpmPath,
        [Parameter(Mandatory = $true)]
        [string]$RepositoryRoot
    )

    return Start-Process `
        -FilePath $NpmPath `
        -ArgumentList @('run', 'docs:dev') `
        -WorkingDirectory $RepositoryRoot `
        -WindowStyle Hidden `
        -PassThru
}

function Get-DocsBrowserContext
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$EntryScriptPath
    )

    $repositoryRoot = Split-Path -Parent (Split-Path -Parent $EntryScriptPath)
    $packageJsonPath = Join-Path $repositoryRoot 'package.json'

    if (-not (Test-Path -LiteralPath $packageJsonPath))
    {
        throw "package.json was not found at $packageJsonPath."
    }

    return [pscustomobject]@{
        NpmPath = Get-NpmCommandPath
        RepositoryRoot = $repositoryRoot
        Url = 'http://127.0.0.1:6420/board'
    }
}

function Ensure-DocsBrowserServer
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$EntryScriptPath
    )

    $context = Get-DocsBrowserContext -EntryScriptPath $EntryScriptPath

    if (-not (Test-DocsUiReady -Url $context.Url))
    {
        $process = Start-DocsBrowserServer `
            -NpmPath $context.NpmPath `
            -RepositoryRoot $context.RepositoryRoot

        $deadline = (Get-Date).AddSeconds(25)
        while ((Get-Date) -lt $deadline)
        {
            Start-Sleep -Milliseconds 500

            if (Test-DocsUiReady -Url $context.Url)
            {
                break
            }

            if ($process.HasExited)
            {
                throw "The VitePress docs server exited before $( $context.Url ) became available."
            }
        }

        if (-not (Test-DocsUiReady -Url $context.Url))
        {
            throw "Timed out waiting for the VitePress docs UI at $( $context.Url )."
        }
    }

    return $context
}
