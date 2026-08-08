# Coordinated Leasing Guide

This guide explains how to use Potion Panic's coordinated leasing system
without needing deep Unity editor scripting or Cloudflare Workers knowledge.

Use it when you are about to edit a shared Unity scene, when you need to issue
a developer token, or when the Coordination window is not behaving as expected.

## What It Does

Coordinated leasing is an advisory safety layer for shared Unity files.

It helps the team answer these questions:

- Who has a coordinated scene open?
- Who is currently editing it?
- Has someone reserved it before starting work?
- Is a save about to overwrite another person's active claim?
- Is the server unavailable, forcing us back to manual coordination?

It does not lock files on disk. Git, Unity, Rider, and the filesystem can still
write files. The system makes conflicts visible and makes risky saves
deliberate.

Manual announcements are still required for protected Unity work.

## Current Protection Scope

The tracked rules live in `coordination.json` at the repository root.

At the time of writing, the configured coordinated rule is:

```json
"Assets/Scenes/**/*.unity"
```

That means the current Unity client coordinates scene files under
`Assets/Scenes/`, including `Assets/Scenes/SampleScene.unity`.

Important prefabs, project settings, packages, and other shared assets still
need manual announcements. They are not automatically protected until they are
added to `coordination.json` and verified.

## The Pieces

### Unity Coordination Window

Open it from Unity:

```text
Window > Potion Panic > Coordination
```

This window shows:

- your authenticated identity
- your current Git branch
- your local task context
- the connection state
- open coordinated assets
- editing leases
- reservations
- uncoordinated-save warnings

It also gives you actions for the selected path:

- `Use active stage`
- `Use Project selection`
- `Reserve`
- `Release editing lease`
- `Cancel reservation`
- `Override...`
- `Copy path`
- `Forget credentials`

### Cloudflare Worker

The Worker is the public HTTPS and WebSocket endpoint. Unity talks to it through
the URL in the repository-root `coordination.json`.

Current configured endpoint:

```text
https://potion-panic-coordination.gabriel-wawerski.workers.dev
```

The Worker has a simple health route:

```text
GET /health
```

That route is unauthenticated. It should return service
`potion-panic-coordination` and a parseable `serverTime`.

### Durable Object

The Durable Object is the authoritative state holder behind the Worker. For
this project, think of it as one small server-owned state room for
`potion-panic`.

It stores:

- developers
- sessions
- active WebSocket connections
- viewing presence
- editing leases
- reservations
- short replay records
- a monotonic state version

The client does not get to decide the developer ID, connection ID, project ID,
or state version. The server derives those from the authenticated request.

### Credentials

There are four credential-like values. Do not mix them up.

| Value | Used by | Stored where | Purpose |
| --- | --- | --- | --- |
| `ADMIN_TOKEN` | operator scripts | Cloudflare secret and current shell only | Issue and revoke developer credentials. |
| `TOKEN_HMAC_KEY` | Worker | Cloudflare secret only | Hash developer and session tokens before storage. |
| developer token | each developer | Windows Credential Manager | Lets Unity create 24-hour sessions. |
| session token | Unity client | memory only | Authenticates the current WebSocket session. |

Never put any token, secret, session value, `Authorization` header, or
Credential Manager content in Git, a ticket, a URL, a log, or chat.

Unity stores developer tokens under this Windows Credential Manager target:

```text
PotionPanic/Coordination/potion-panic/developer-token
```

Local Unity settings live here and must not contain tokens:

```text
UserSettings/PotionPanic/coordination.local.json
```

That file is ignored by Git. Its safe shape is:

```json
{
  "schemaVersion": 1,
  "serverBaseUrlOverride": "",
  "taskContext": "",
  "disabled": false
}
```

## First Setup For A Developer

1. Pull the latest `master`.
2. Open the project with Unity `6000.5.1f1`.
3. Open `Window > Potion Panic > Coordination`.
4. Ask the operator for your one-time developer token through an approved
   secret channel.
5. Paste the token into the Coordination credential prompt.
6. Confirm the Coordination window changes from `Offline` or
   `AuthenticationFailed` to `Connected`.
7. Enter a short task context, such as `PP-7`, `lab blockout`, or
   `ingredient pickup`.

The task context is machine-local. It helps the other person understand why you
are holding a lease.

If authentication fails, use `Forget credentials`, get a newly issued token,
and paste it again. Do not reuse a token that may have been copied into an
unsafe place.

## Normal Scene Editing Workflow

Use this flow before editing `Assets/Scenes/SampleScene.unity` or another
coordinated scene.

1. Pull the latest `master`.
2. Announce the work in the team channel.
3. Open the Coordination window.
4. Confirm `Connection` is `Connected`.
5. Set `Task context` to the task or short reason for the edit.
6. Select the path:
   - click an existing row, or
   - click `Use active stage`, or
   - select the asset in the Project window and click `Use Project selection`,
     or
   - use `Advanced path` only when the previous options do not work.
7. If the path is free, click `Reserve` before you start.
8. Open the scene and make the edit.
9. Save normally.
10. When finished, close the scene or click `Release editing lease` if you own
    the editing lease.
11. Push your branch and tell the other developer what changed.

Opening a coordinated scene publishes presence. Making meaningful changes to a
dirty exclusive scene tries to acquire an editing lease. Reservations last
longer than editing leases and survive a normal connection closing until they
expire, are cancelled, are overridden, or the developer is revoked.

## Reading The Window

### Presence

Presence means someone has a coordinated asset open. It is informational and
non-exclusive.

Presence answers:

```text
Who is looking at this file right now?
```

### Editing Lease

An editing lease means one connection currently owns the edit claim for a path.
For scene files, this is the claim the save guard checks before allowing a
coordinated save.

Editing leases expire if the client disconnects and does not return. Healthy
clients heartbeat to keep their own presence and editing leases alive.

### Reservation

A reservation means a developer has claimed the path before editing. It is
developer-owned, not connection-owned.

Use a reservation when you know you are about to work on a coordinated scene
and want to tell the other person to avoid starting conflicting work.

Cancel a local reservation when you no longer need it. If `Cancel reservation`
fails while other actions work, the production Worker may be older than this
repo's current client. Deploying the updated Worker is a separate operator
approval gate.

### Override

Override transfers a remote claim to you. Use it only after explicit agreement
or when the other developer is unreachable and the team accepts the risk.

Override is not a polite "ask". It is a server-side ownership transfer.

## Save Conflicts

If you try to save a coordinated scene while someone else owns the claim, Unity
should stop and show a conflict dialog.

Choose:

- `Override and save` only after deliberate agreement or an accepted emergency.
- `Cancel save` if you do not want to save now.
- `Keep working` if you need to inspect or copy your local changes first.

During an outage or reconnect problem, Unity may offer
`Save locally without coordination`. That path requires confirmation and records
a memory-only warning in the Coordination window. It does not write server
history and it does not make the local save coordinated after the fact.

When in doubt, preserve local work first, then coordinate manually.

## Outage Workflow

Use this when the Worker is down, the endpoint is wrong, the network is down, or
the Coordination window cannot authenticate.

1. Stop and check whether you are about to edit a protected shared file.
2. Announce the file and the risk manually.
3. In the Coordination window, select `Disabled` if the server is unavailable
   or unhealthy.
4. Keep working only if the team accepts manual coordination for that file.
5. Save locally only after the explicit confirmation prompts.
6. Do not treat the local save as coordinated.
7. Reconnect only after `/health` succeeds and the window can connect again.

Health check from PowerShell:

```powershell
$workerBaseUrl = "https://potion-panic-coordination.gabriel-wawerski.workers.dev"
$health = Invoke-RestMethod -Method Get -Uri "$workerBaseUrl/health"
$health.service
$health.serverTime
```

Expected service:

```text
potion-panic-coordination
```

## Running The Worker Locally

Use local Worker mode when testing server changes. Run commands from
`Tools/CoordinationServer`.

```powershell
cd Tools/CoordinationServer
npm ci
npm run typecheck
npm test
npx wrangler deploy --dry-run
```

The dry run validates the bundle. It does not deploy.

For local development, create local-only secrets and start Wrangler:

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

Then set the local Unity endpoint override in the ignored user settings file:

```json
{
  "schemaVersion": 1,
  "serverBaseUrlOverride": "http://127.0.0.1:8787",
  "taskContext": "",
  "disabled": false
}
```

Do not commit `.dev.vars` or `UserSettings/PotionPanic/coordination.local.json`.

## Operator Workflow

Use this section only if you are responsible for Cloudflare operations.

The full operational source is `Tools/CoordinationServer/README.md`. Prefer
that README for exact deployment and token commands.

### Verify Before Deploying

From `Tools/CoordinationServer`:

```powershell
npm ci
npm run typecheck
npm test
npm audit --audit-level=moderate
npx wrangler deploy --dry-run
```

Also run the root docs checks when documentation changed:

```powershell
cd ..\..
npm test
npm run docs:build
```

### Deploy Manually

Deployments are manual authenticated operator actions. GitHub Actions is
verification-only for the coordination server.

Before deploying:

- confirm `npx wrangler whoami`
- confirm the intended Cloudflare account
- confirm the required secrets exist
- never print or save secret values

Required Worker secrets:

```text
ADMIN_TOKEN
TOKEN_HMAC_KEY
```

After deploying:

1. Copy the exact Worker URL printed by Wrangler.
2. Verify `GET /health`.
3. Confirm the service is `potion-panic-coordination`.
4. Confirm `coordination.json` still points at the verified endpoint, or update
   only `serverBaseUrl` if the endpoint changed.
5. Record the deployment version, date, commands, and remaining blockers in
   the relevant ticket without capturing secrets.

### Issue A Developer Token

Developer tokens are issued by the script in
`Tools/CoordinationServer/scripts/issue-token.mjs`.

The script prints the token once. Deliver it through an approved secret channel.
The developer pastes it into Unity. Do not paste it into a ticket, chat, URL,
log, or shell transcript.

### Revoke A Developer

Revocation removes that developer's server-side access and active sessions. It
also removes that developer's coordination state.

Use the delete route documented in the server README:

```text
DELETE /v1/projects/potion-panic/developers/{developerId}
Authorization: Bearer <ADMIN_TOKEN>
```

Active clients for that developer should stop retrying after revocation.

## Troubleshooting

### The window says `Offline`

Check:

- the endpoint in `coordination.json`
- local endpoint override in `coordination.local.json`
- `/health`
- network connectivity
- whether the Worker was deployed after the current code changes

Click `Reconnect` after fixing the cause.

### The window says `AuthenticationFailed`

The token is missing, invalid, revoked, or the Worker rejected the session
request.

Use `Forget credentials`, ask the operator for a new developer token, and paste
it into the credential prompt.

### Buttons are disabled

Common causes:

- the window is disconnected
- `Disabled` is checked
- no valid path is selected
- the path is outside `Assets/`
- the path does not match a coordinated rule
- the selected claim is not owned by you
- the claim changed since the row was rendered

Use the helper text under the action target. It explains the current reason.

### A claim looks stale

First try `Reconnect`. If the owner has gone offline, wait for expiry before
assuming the claim is gone. Editing leases and presence are short-lived, but
reservations last longer.

Override only after the team accepts the risk.

### Local saves show warnings

The save happened without authoritative coordination. Treat it as a manual
coordination event:

1. tell the other developer
2. preserve the local diff
3. reconnect
4. resolve any conflict deliberately

The warning clears when the affected asset closes or coordination later confirms
local ownership.

## What To Record In Tickets

For coordination-related work, record facts that another developer can verify:

- date and machine role
- branch
- Worker URL used
- whether `/health` passed
- Unity version
- connection state
- affected paths
- whether the save was coordinated or local-only
- commands run
- test counts
- remaining blockers

Do not record:

- tokens
- session values
- `Authorization` headers
- secret values
- Credential Manager contents
- raw unfiltered `wrangler tail` output

## Quick Reference

| Situation | Action |
| --- | --- |
| Starting scene work | Announce, connect, set task context, reserve the path. |
| Someone else owns the path | Coordinate manually; override only deliberately. |
| You own an editing lease | Save normally; release or close when done. |
| You own a reservation | Edit soon or cancel it. |
| Worker is down | Use `Disabled`, announce manually, preserve local work. |
| Token is bad | Forget credentials and ask for a new developer token. |
| Testing backend locally | Use `.dev.vars` and a local endpoint override. |
| Deploying backend | Follow `Tools/CoordinationServer/README.md`; never capture secrets. |
