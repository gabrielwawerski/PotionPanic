# Potion Panic Coordination Server

Cloudflare Worker and SQLite-backed Durable Object foundation for advisory
editing coordination. Slice 02 adds revocable developer tokens and opaque
24-hour sessions. The Durable Object stores only HMAC-SHA-256 digests of both
token types.

## Commands

Run these commands from this directory:

```powershell
npm ci
npm run typecheck
npm test
npx wrangler deploy --dry-run
```

Configure these production secrets with Wrangler before using authenticated
routes:

```powershell
npx wrangler secret put TOKEN_HMAC_KEY
npx wrangler secret put ADMIN_TOKEN
```

`TOKEN_HMAC_KEY` hashes developer and session tokens. `ADMIN_TOKEN` authorizes
developer issuance and revocation; it is not a developer credential. Use a
high-entropy value for each secret and keep both outside Git.

## Issue and rotate developer tokens

Set `ADMIN_TOKEN` only in the invoking shell, then issue a token:

```powershell
$env:ADMIN_TOKEN = '<administrator token>'
node scripts/issue-token.mjs https://coordination.example.workers.dev "Developer name"
Remove-Item Env:ADMIN_TOKEN
```

The script calls `POST /v1/projects/potion-panic/developers`, prints the new
developer token once, and does not write it to disk. Deliver that token through
an approved secret channel. Do not place it in a URL, Git-tracked file, log, or
ticket.

To rotate a developer token, revoke the developer and issue a new developer
record. Revocation deletes that developer's sessions immediately. Slice 04
will also close the developer's active WebSockets. Use the administrative
delete route with the same `ADMIN_TOKEN`:

```text
DELETE /v1/projects/potion-panic/developers/{developerId}
Authorization: Bearer <ADMIN_TOKEN>
```

`POST /v1/projects/potion-panic/sessions` exchanges a bearer developer token
for a 24-hour opaque session. It returns developer identity, server time, the
lease and reservation TTLs, and state version; it never creates or returns a
connection ID. `GET /health` remains unauthenticated.

## Local WebSocket operation

Start the Worker in one terminal after setting local-only secrets in
`.dev.vars`, then create a session in a second terminal. Do not put either
token in the WebSocket URL.

```powershell
npx wrangler dev --local

$session = Invoke-RestMethod `
  -Method Post `
  -Uri 'http://127.0.0.1:8787/v1/projects/potion-panic/sessions' `
  -Headers @{ Authorization = 'Bearer <developer token>' }

npx wscat -c 'ws://127.0.0.1:8787/v1/projects/potion-panic/connect' `
  -H "Authorization: Bearer $($session.sessionToken)"
```

The successful upgrade immediately sends `session.ready`, then the current
`snapshot`. The server assigns `connectionId`; clients never send it, project
or developer identity, or a state version. Every client message needs a UUID
v4 `requestId`, for example:

```json
{
  "protocolVersion": 1,
  "type": "presence.open",
  "requestId": "11111111-1111-4111-8111-111111111111",
  "path": "Assets/Scenes/SampleScene.unity",
  "branch": "feature/example",
  "task": "PP-7"
}
```

The server broadcasts resulting state changes to connected clients. It keeps
the socket metadata in Durable Object hibernation attachments, so a dormant
connection remains usable after the object wakes. Close the client cleanly
when finished; the server releases connection-scoped presence and editing
leases, while reservations remain until their expiry.

Copy `.dev.vars.example` to `.dev.vars` only when a later slice requires local
secrets. Never commit `.dev.vars`.
