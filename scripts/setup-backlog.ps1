Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Test-CommandAvailable {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    return $null -ne (Get-Command -Name $Name -ErrorAction SilentlyContinue)
}

if (-not (Test-CommandAvailable -Name 'backlog')) {
    if (-not (Test-CommandAvailable -Name 'npm')) {
        throw "npm is required to install backlog.md. Install Node.js and npm, then rerun this script."
    }

    Write-Host "Installing backlog.md globally with npm..."
    & npm i -g backlog.md

    if ($LASTEXITCODE -ne 0) {
        throw "npm i -g backlog.md failed with exit code $LASTEXITCODE."
    }
} else {
    Write-Host "The backlog CLI is already available."
}

Write-Host ""
Write-Host "Add the shared MCP server manually with one of these commands:"
Write-Host "  codex mcp add backlog backlog mcp start"
Write-Host "  gemini mcp add backlog -- backlog mcp start"
