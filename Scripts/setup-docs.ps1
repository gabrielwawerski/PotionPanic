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

    throw 'npm is required for the VitePress docs workflow. Install Node.js and npm, then rerun this script.'
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$packageJsonPath = Join-Path $repositoryRoot 'package.json'

if (-not (Test-Path -LiteralPath $packageJsonPath))
{
    throw "package.json was not found at $packageJsonPath."
}

$npmPath = Get-NpmCommandPath

Write-Host 'Installing VitePress docs dependencies...'
& $npmPath install

if ($LASTEXITCODE -ne 0)
{
    throw "npm install failed with exit code $LASTEXITCODE."
}

Write-Host ''
Write-Host 'Docs tooling is ready.'
Write-Host 'Start the editable board with:'
Write-Host '  .\Scripts\docs-ui.ps1'
Write-Host 'Or run the dev server manually with:'
Write-Host '  npm run docs:dev'
