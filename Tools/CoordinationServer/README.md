# Potion Panic Coordination Server

This Cloudflare Worker and SQLite-backed Durable Object provide advisory file
leasing for the Potion Panic Unity project. The Durable Object stores only
HMAC-SHA-256 digests of developer and 24-hour session tokens.

Run commands in this guide from `Tools/CoordinationServer`.

Developer instructions for using claims inside Unity belong in the
[Unity Coordination Guide](../../Docs/guides/coordinated-leasing.md). This
runbook is for operators who build, deploy, verify, monitor, and administer the
service.

## Architecture and trust model

The Worker exposes the health, authentication, administration, and WebSocket
routes. A project-scoped Durable Object owns the shared connection, presence,
reservation, and editing-lease state in SQLite. Connected Unity editors receive
state changes through the WebSocket protocol.

The system is advisory. A valid claim affects coordination decisions in the
Unity integration, but the Worker cannot lock a repository file, write a scene,
merge a branch, or prove that an unconnected editor did not change an asset.
Git review and pre-edit announcements remain separate controls.

| Credential or value | Purpose | Where it may exist | Consequence if exposed or changed |
| --- | --- | --- | --- |
| `TOKEN_HMAC_KEY` | Produces the HMAC digests stored for developer and session tokens. | Cloudflare secret storage or an ignored local `.dev.vars`. | Exposure weakens every token digest. Rotation invalidates every existing developer token and session. |
| `ADMIN_TOKEN` | Authorizes developer issuance and revocation. | Cloudflare secret storage, approved password manager, or ignored local `.dev.vars`. | Exposure permits credential administration. Rotation affects future administrative calls but does not revoke developers. |
| Developer token | Lets one named developer create sessions. | Approved secret channel and that developer's Windows Credential Manager. | Revocation removes that developer's sessions and live coordination state. |
| Session token | Authorizes one temporary client session. | Unity process memory and server-side HMAC digest. | Expires after 24 hours; revocation of the developer removes it sooner. |
| Worker URL | Identifies the deployed endpoint. It is not a secret. | `coordination.json`, deployment evidence, or an approved local override. | A wrong or unhealthy URL disconnects clients; it does not grant access. |

The service trusts an authenticated developer to report branch, task, open
paths, and edit intent accurately. It trusts Cloudflare and the Durable Object
to serialize shared state. It does not treat user-supplied branch or task text
as authorization.

## Verify the server

```powershell
npm ci
npm run typecheck
npm test
npx wrangler deploy --dry-run
```

The dry run validates the release bundle. It does not deploy the Worker.
Production deployment is a manual action by an authenticated operator.

Expected result: dependency installation, type checking, tests, and the Wrangler
dry run all exit successfully. Stop before deployment if any command fails, if
the Durable Object binding or migration contract is unexpected, or if the
checkout contains unrelated server changes that have not been reviewed.

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

Local mode uses disposable secrets, local Durable Object storage, and an
untracked client endpoint override. It does not validate Cloudflare account
selection, production secrets, the public Worker URL, or production data.
Expected result: Wrangler prints a local URL, `GET /health` returns the service
identity and server time, and a deliberately local developer credential can
open a Unity connection. Never issue a production developer token against the
local server or copy local state into production.

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

Stop the release and leave the repository endpoint unchanged when any of these
conditions applies:

- `whoami` does not prove the intended Cloudflare account;
- the production secrets have not been obtained from the approved store;
- the dry run, type check, or test suite fails;
- the deployment output omits the expected Durable Object binding or export;
- the exact deployed URL fails the health contract;
- the deployed URL differs from the repository endpoint and the client change
  has not been reviewed;
- required deployment evidence would contain a credential or secret.

A successful `wrangler deploy` is necessary but insufficient. Production is
ready for clients only after the exact printed URL passes the health check and
the configured endpoint matches that verified URL.

## Issue and revoke developer tokens

From the repository root, run the token command with the developer's display
name. It reads `serverBaseUrl` from `coordination.json`, verifies the Worker's
`/health` response, prompts for `ADMIN_TOKEN` without echoing it, and calls
`POST /v1/projects/potion-panic/developers`.

```powershell
npm --prefix Tools/CoordinationServer run issue-dev-token -- 'Developer name'
```

The command prints the new developer token once without writing it to disk.
Deliver that token once through an approved secret channel. The developer
pastes it only into `Window > Potion Panic > Coordination`; never put it in a
URL, tracked file, log, ticket, or chat.

Issuance creates a new developer identity and one retrievable token. Losing the
printed value requires a new issuance; the operator cannot recover the original
plaintext from the stored HMAC digest. Do not issue shared team tokens because
claims and revocation must identify one developer.

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

After revocation, the developer token cannot create another session. Existing
sessions are deleted, active sockets close with the revocation state, and that
developer's presence, editing leases, and reservations are removed. Warn the
developer before planned revocation when possible because the server-side state
disappears even if the editor still has unsaved local work.

## Monitor without capturing credentials

```powershell
npx wrangler tail
```

Exclude authorization headers, developer tokens, opaque sessions,
`ADMIN_TOKEN`, `TOKEN_HMAC_KEY`, and Credential Manager contents from saved
logs and acceptance evidence. Retain only necessary timestamps, request IDs,
status codes, event categories, and error codes.

`wrangler tail` shows Worker execution and protocol failures that reach the
service. It does not prove that a Unity editor saved safely, that every client
is connected, or that manual coordination happened. A clean tail is not an
acceptance test. Correlate only sanitized request IDs and timestamps, then use
client-side evidence for editor behavior.

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

## Protocol lifecycle

1. The client calls `POST /v1/projects/potion-panic/sessions` with its developer
   bearer token. The server validates the HMAC digest and returns the opaque
   24-hour session plus developer identity and timing metadata.
2. The client upgrades `/v1/projects/potion-panic/connect` with the session.
   The server assigns the connection identity.
3. The server sends `session.ready`, followed by the current snapshot. Large
   snapshots may be split into chunks that share one snapshot ID.
4. The client publishes `presence.open` and `presence.close` as coordinated
   stages open and close. Presence belongs to the connection.
5. The client uses `lease.reserve` and `reservation.cancel` for developer-owned
   intent. It uses `lease.acquire`, `lease.release`, and `lease.override` for
   exclusive editing transitions.
6. Every client message includes protocol version 1 and a UUID v4 `requestId`.
   Clients do not submit developer identity, connection identity, or state
   version as authority.
7. Heartbeats renew live connection state. A `snapshot.request` reconciles the
   client with the current authoritative state when needed.
8. The server broadcasts presence and lease changes with monotonically updated
   state versions. A denial includes a stable error code and the current lease
   when one exists.
9. A clean close or expired connection removes connection-owned presence and
   editing leases. Developer-owned reservations remain until cancellation,
   override, conversion, expiry, or developer revocation.

The protocol contract lives in `src/protocol.ts`; HTTP routing, session
handling, and Durable Object state are implementation authorities. Update this
section when those contracts change, but keep ordinary Unity procedures in the
developer guide.
