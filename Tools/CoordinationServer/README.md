# Potion Panic Coordination Server

This Cloudflare Worker and SQLite-backed Durable Object provide advisory file
leasing for the Potion Panic Unity project. The Durable Object stores only
HMAC-SHA-256 digests of developer and 24-hour session tokens.

Run commands in this guide from `Tools/CoordinationServer`.

## Verify the server

```powershell
npm ci
npm run typecheck
npm test
npx wrangler deploy --dry-run
```

The dry run validates the release bundle. It does not deploy the Worker.
Production deployment is a manual action by an authenticated operator.

## Run locally

Create independent local-only secrets. Do not reuse production values.

```powershell
Copy-Item .dev.vars.example .dev.vars
function New-UrlSafeSecret {
  $bytes = [System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
  try {
    return [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
  }
  finally {
    [Array]::Clear($bytes, 0, $bytes.Length)
  }
}
$localHmac = New-UrlSafeSecret
$localAdmin = New-UrlSafeSecret
@("TOKEN_HMAC_KEY=$localHmac", "ADMIN_TOKEN=$localAdmin") | Set-Content .dev.vars
Remove-Variable localHmac, localAdmin
npx wrangler dev --local
```

`.dev.vars.example` contains empty `TOKEN_HMAC_KEY` and `ADMIN_TOKEN`
assignments. `.dev.vars` is ignored and must never be committed. To connect
Unity to this local Worker, set the untracked endpoint override in
`UserSettings/PotionPanic/coordination.local.json` to
`http://127.0.0.1:8787`.

## Deploy manually

Generate two independent 256-bit URL-safe values in the approved password
manager. `TOKEN_HMAC_KEY` hashes developer and session tokens. `ADMIN_TOKEN`
authorizes developer issuance and revocation and is not a developer credential.

Authenticate, confirm the selected account, and enter both values through
hidden prompts. The temporary secrets file stays outside the repository, is
used for one atomic deployment, and is removed in `finally`.

```powershell
npx wrangler login
npx wrangler whoami
$hmacSecure = Read-Host 'TOKEN_HMAC_KEY from password manager' -AsSecureString
$adminSecure = Read-Host 'ADMIN_TOKEN from password manager' -AsSecureString
$hmacPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($hmacSecure)
$adminPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($adminSecure)
$secretFile = Join-Path $env:TEMP ("potion-panic-secrets-{0}.env" -f [guid]::NewGuid())
try {
  $hmacValue = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($hmacPointer)
  $adminValue = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($adminPointer)
  @("TOKEN_HMAC_KEY=$hmacValue", "ADMIN_TOKEN=$adminValue") | Set-Content $secretFile
  npx wrangler deploy --strict --secrets-file $secretFile
  if ($LASTEXITCODE -ne 0) { throw 'Wrangler deployment failed.' }
}
finally {
  Remove-Item -LiteralPath $secretFile -Force -ErrorAction SilentlyContinue
  Remove-Variable hmacValue, adminValue -ErrorAction SilentlyContinue
  [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($hmacPointer)
  [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($adminPointer)
}
npx wrangler secret list
npx wrangler deployments list
```

If `npx wrangler whoami` lists multiple accounts, record the selected account
ID in `wrangler.jsonc` before deploying. Confirm or create that account's
`workers.dev` subdomain. Do not put either secret in Git, a URL, a ticket,
command history, or captured output.

A successful deploy must report the `COORDINATION_OBJECT` binding and the
SQLite `CoordinationObject` export. Wrangler's successful deploy output is the
source of truth for the Worker URL. Verify that exact URL before changing
client configuration:

```powershell
$workerBaseUrl = Read-Host 'Paste the exact Worker URL printed by wrangler deploy'
$health = Invoke-RestMethod -Method Get -Uri "$workerBaseUrl/health"
if ($health.service -ne 'potion-panic-coordination') {
  throw 'Unexpected coordination service response.'
}
[DateTimeOffset]::Parse($health.serverTime) | Out-Null
```

`GET /health` must return HTTP 200, service `potion-panic-coordination`, and a
parseable `serverTime`. The repository currently configures
`https://potion-panic-coordination.gabriel-wawerski.workers.dev` in
`coordination.json`. Keep that value only while it matches verified deployment
output and passes the health check. After a future deployment changes the URL,
update only `serverBaseUrl` after the new endpoint passes this verification.

Record the deployment evidence in the relevant ticket without secrets:

- deployment date and version
- exact Worker URL
- health-check result
- verification commands and test results
- remaining blockers or manual follow-up

## Issue and revoke developer tokens

Use the still-valid administrative secret only for the current shell. The
issuance script calls `POST /v1/projects/potion-panic/developers` and prints the
new developer token once without writing it to disk.

```powershell
$secureAdmin = Read-Host 'ADMIN_TOKEN' -AsSecureString
$pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureAdmin)
try {
  $env:ADMIN_TOKEN = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
  $workerBaseUrl = Read-Host 'Paste the exact Worker URL printed by wrangler deploy'
  node scripts/issue-token.mjs $workerBaseUrl 'Developer name'
}
finally {
  Remove-Item Env:ADMIN_TOKEN -ErrorAction SilentlyContinue
  [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
}
```

Use only the URL from verified deployment output. Deliver the printed token
once through an approved secret channel. The developer pastes it only into
`Window > Potion Panic > Coordination`; never put it in a URL, tracked file,
log, ticket, or chat.

To revoke a developer, call the administrative delete route with the developer
ID. Revocation deletes that developer's sessions, closes active sockets with
the documented revocation state, and removes connection-scoped coordination
state.

```text
DELETE /v1/projects/potion-panic/developers/{developerId}
Authorization: Bearer <ADMIN_TOKEN>
```

`POST /v1/projects/potion-panic/sessions` exchanges a bearer developer token
for an opaque 24-hour session. It returns developer identity, server time,
lease and reservation TTLs, and state version. It never creates or returns a
connection ID. `GET /health` remains unauthenticated.

## Monitor without capturing credentials

```powershell
npx wrangler tail
```

Exclude authorization headers, developer tokens, opaque sessions,
`ADMIN_TOKEN`, `TOKEN_HMAC_KEY`, and Credential Manager contents from saved
logs and acceptance evidence. Retain only necessary timestamps, request IDs,
status codes, event categories, and error codes.

## Handle an outage

If health fails or the configured server is missing or invalid, select
`Disabled` in the Unity Coordination window. The local switch does not delete
work. Preserve local changes, announce protected-file edits through the manual
collaboration channel, and reconnect only after `/health` succeeds. Advisory
leases never replace pre-edit announcements.

## Rotate secrets

Rotating `ADMIN_TOKEN` changes authorization for future administrative calls.
It does not invalidate developer credentials by itself.

Rotating `TOKEN_HMAC_KEY` invalidates every stored developer-token and
session-token HMAC. Before rotating it, record the developer IDs without their
tokens. Set the new HMAC key, use the still-valid administrative secret to
revoke the old developer records, and issue every developer a new token. Treat
session creation as unavailable until the credentials have been reissued and
provisioned.

## WebSocket behavior

Authenticated clients connect to
`/v1/projects/potion-panic/connect`. A successful upgrade sends
`session.ready`, then the current snapshot. The server assigns `connectionId`;
clients never send identity, connection ID, or state version. Every client
message needs a UUID v4 `requestId`.

The server broadcasts state changes and stores socket metadata in Durable
Object hibernation attachments. Close the client cleanly when finished. The
server then releases connection-scoped presence and editing leases, while
reservations remain until expiry.
