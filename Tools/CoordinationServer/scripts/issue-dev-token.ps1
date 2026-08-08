[CmdletBinding()]
param(
  [Parameter(Mandatory = $true, Position = 0)]
  [ValidateNotNullOrEmpty()]
  [string] $DisplayName
)

$secureAdmin = Read-Host 'ADMIN_TOKEN' -AsSecureString
$pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureAdmin)
$exitCode = 1

try {
  $env:ADMIN_TOKEN = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
  & node (Join-Path $PSScriptRoot 'issue-token.mjs') $DisplayName
  $exitCode = $LASTEXITCODE
}
finally {
  Remove-Item Env:ADMIN_TOKEN -ErrorAction SilentlyContinue
  [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
}

exit $exitCode
