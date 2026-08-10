# Unity Coordination Guide

Use this guide before editing a coordinated Unity scene. It explains what the
Coordination system tracks, what Unity handles automatically, and what you must
do when two developers may touch the same asset.

The system is an advisory safety layer. It does not lock files on disk, stop Git
from accepting a conflicting change, or replace a direct announcement to the
team. It makes overlapping work visible and makes unsafe saves deliberate.

Operator procedures such as deployment, token issuance, revocation, and secret
rotation belong in the
[Coordination Server runbook](https://github.com/gabrielwawerski/PotionPanic/blob/master/Tools/CoordinationServer/README.md).

## The problem it solves

Unity stores a scene as one serialized asset. Two developers can change
different GameObjects in the same scene and still produce competing edits to the
same YAML file. Git may report a conflict only after both developers have spent
time working. Resolving that conflict by hand can disconnect Inspector
references or discard valid scene data.

Coordination moves the warning earlier. Before and during an edit, it tells the
team who has a scene open, who intends to edit it, and which live Unity
connection owns the current editing claim.

## Current rule scope

The active rule in repository-root `coordination.json` covers:

```text
Assets/Scenes/**/*.unity
```

This includes `Assets/Scenes/SampleScene.unity`. Prefabs, ProjectSettings, and
packages are not covered by that rule. Always announce before editing a scene,
prefab, ProjectSettings file, or package file. Assets outside the rule still use
manual coordination unless a verified rule says otherwise.

## Mental model

The system tracks several related states. They do not all have the same owner or
lifetime.

| State                | Meaning                                                                                  | Owner         | Lifetime                                                                   |
|----------------------|------------------------------------------------------------------------------------------|---------------|----------------------------------------------------------------------------|
| Developer credential | Long-lived proof that a named developer may request a session.                           | Developer     | Until the operator revokes it or the developer forgets it locally.         |
| Session              | Temporary authorization returned after the developer credential is accepted.             | Developer     | 24 hours. Unity keeps it in memory only.                                   |
| Connection           | One live Unity Editor WebSocket connection.                                              | Unity process | Until it disconnects or reconnects.                                        |
| Presence             | A connection currently has a coordinated asset open. It is informational, not exclusive. | Connection    | Refreshed by 30-second heartbeats and removed when the connection closes.  |
| Reservation          | A developer intends to edit an asset soon. It is exclusive.                              | Developer     | 30 minutes unless cancelled, overridden, or converted when editing begins. |
| Editing lease        | One live connection owns the active edit claim checked during saves. It is exclusive.    | Connection    | 120 seconds, renewed while the connection remains healthy.                 |

The ownership distinction matters. A reservation survives the loss of one
connection because it represents the developer's intent. Presence and editing
leases belong to a specific connection and disappear when that connection
closes. If your own reservation becomes an editing lease, the server can restore
the reservation when that editing connection closes, provided the reservation
has not expired or been cancelled.

### State changes during normal work

| Event                          | What Unity or the server does                                                                                             | What teammates can infer                                                                |
|--------------------------------|---------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------|
| Connect                        | Exchanges the developer credential for a 24-hour session when needed, opens a connection, and loads the current snapshot. | This editor can publish current coordination state.                                     |
| Open a coordinated scene       | Publishes presence for the active stage.                                                                                  | The scene is open, but the developer may only be inspecting it.                         |
| Reserve                        | Creates a developer-owned exclusive intention to edit.                                                                    | Others should not begin overlapping work.                                               |
| Make a meaningful scene change | Marks the scene dirty and attempts to acquire or convert to an editing lease.                                             | This connection is actively editing the scene.                                          |
| Heartbeat                      | Refreshes the connection and its short-lived state every 30 seconds.                                                      | Current presence and editing claims remain live.                                        |
| Save                           | Checks current ownership before Unity writes a coordinated scene.                                                         | A normal save had a valid local claim at the check point.                               |
| Close the scene                | Releases presence and the connection-owned editing lease.                                                                 | The active edit ended; a developer-owned reservation may remain.                        |
| Reconnect                      | Creates a new connection and republishes the relevant local stage state.                                                  | Old connection state can expire or be removed; current open work becomes visible again. |
| Override                       | Transfers a remote reservation or editing claim after confirmation.                                                       | The previous owner no longer controls that claim.                                       |
| Release editing lease          | Ends a local active editing claim without closing the scene.                                                              | The path may become available, or the earlier reservation may remain.                   |
| Cancel reservation             | Removes your intended-edit claim.                                                                                         | You no longer intend to start or resume that edit.                                      |

An expiry is a recovery mechanism, not a scheduling promise. Do not wait for a
timer and silently take over someone else's work. Contact the owner first.

## First-time setup

Complete [Project Setup](../onboarding/getting-started.md) first. Then:

1. Pull the latest `master` and open the project with Unity 6000.5.1f1.
2. Open `Window > Potion Panic > Coordination`.
3. Obtain a developer token from the operator through the approved secret
   channel.
4. Paste it into the credential prompt.
5. Confirm that Connection reports `Connected` and that the displayed identity
   is yours.
6. Enter a short Task context such as `PP-7 scene smoke` or `lab blockout`.

The task context is local metadata shown with your claims. It should tell
another developer why you are using the asset. It is not a progress diary and
does not affect authorization.

If authentication fails, choose `Forget credentials`, obtain a new token through
the approved channel, and enter it again. Never put a credential or session
token in Git, a URL, a ticket, a log, or chat.

## What Unity handles automatically

When Coordination is enabled and connected, the editor integration:

- authenticates and opens the live connection;
- sends 30-second heartbeats;
- publishes presence when a coordinated stage opens;
- attempts to obtain an editing lease after a meaningful change makes the scene
  dirty;
- checks the claim before saving a coordinated scene;
- releases connection-owned presence and editing state when the stage closes;
- republishes relevant local stage state after reconnecting.

These automatic actions reduce bookkeeping. They do not announce the scope of
your task, create a Git branch, reserve an asset before you start, or decide
whether an override is socially safe.

## What you must do explicitly

Before editing a coordinated scene:

1. Check the working tree and update `master` as described in
   [Daily Workflow](../collaboration/team-workflow.md).
2. Announce the scene and intended change through the team channel.
3. Open the Coordination window and verify your identity, branch, connection,
   and task context.
4. Select the scene and reserve it if it is free.
5. Contact the owner if any remote reservation or editing lease exists.

After editing:

1. Save only after the Coordination check permits the save or you have made an
   explicit conflict decision.
2. Run the required Unity and Git verification for the change.
3. Close the scene or release your editing lease when active editing ends.
4. Cancel an unused reservation when you no longer intend to continue.
5. Push and announce the handoff, including the protected asset and evidence.

Manual announcements and server claims coexist because they answer different
questions. An announcement describes the task, likely duration, related files,
and handoff. A claim records a precise asset and current machine state. Either
one alone leaves important context missing.

## Worked example: two developers and one scene

Rin and Sol both need `Assets/Scenes/SampleScene.unity`.

1. Rin announces that she will adjust the brewing-station layout, selects the
   scene in the Coordination window, and chooses `Reserve`. The server records a
   30-minute developer-owned reservation for Rin.
2. Sol opens the project to inspect a separate task. Sol sees Rin's reservation
   before starting scene work and chooses a script-only task instead.
3. Rin opens the scene. Unity publishes presence. When Rin moves the brewing
   station, the scene becomes dirty and Rin's connection obtains the 120-second
   editing lease. Her reservation is represented by the active edit.
4. Heartbeats renew Rin's live connection state every 30 seconds. Rin saves. The
   save guard confirms that her connection owns the editing lease, so Unity
   writes the scene normally.
5. Rin closes the scene. Unity releases her presence and connection-owned
   editing lease. If the original reservation is still valid, it can remain for
   Rin because she may intend to reopen the scene.
6. Rin finishes the task, cancels the reservation, pushes the branch, and
   announces the handoff with test evidence.
7. Sol refreshes the Coordination state, sees that the path is free, announces
   the next edit, and creates a new reservation.

If Rin's editor disconnects during step 4, Sol should not treat the missing
heartbeat as permission to work. Rin may still have unsaved local changes. Sol
contacts Rin, and Rin reconnects so Unity republishes the open stage. They use
an override only if they explicitly agree to transfer ownership.

## Coordination window

Open the window from `Window > Potion Panic > Coordination`.

### Local status

The top of the window shows your identity, Git branch, and connection state.
Confirm that they represent the right person and branch before claiming a scene.

Task context is a short explanation visible with your claims. Keep it focused on
the task, not a status diary.

Disabled turns off coordination on this machine. Use it only when the service is
unavailable or the team has agreed to manual coordination. It does not delete
local work and does not make later saves coordinated.

### Claims and presence

| List           | Meaning                                                                      | What to do                                                        |
|----------------|------------------------------------------------------------------------------|-------------------------------------------------------------------|
| Presence       | Someone has the asset open. It is informational and non-exclusive.           | Check the owner and communicate before overlapping work.          |
| Editing leases | One connection owns the active edit claim. The save guard checks this claim. | Let the owner finish, or use an agreed override.                  |
| Reservations   | A developer has announced an intended edit before starting.                  | Avoid starting the same work; cancel your own unused reservation. |

Rows show the path, owner, branch, task, and expiry. `Local` means the state is
yours. Select a row to make its path the action target. Local rows offer release
or cancel actions. Remote exclusive claims offer `Override…` after confirmation.

### Action target and helper text

The action target is the path affected by the buttons. Set it from a row, the
active Unity stage, the Project window selection, or a typed path below
`Assets/`.

Read the helper text below the buttons before assuming a button is broken. It
explains whether the target is outside `Assets/`, outside the active rule,
disconnected, disabled, already owned, or ready to reserve.

## Window actions

| Control               | When to use it                                                                                | Result                                                                   |
|-----------------------|-----------------------------------------------------------------------------------------------|--------------------------------------------------------------------------|
| Use active stage      | A scene or prefab stage is already open.                                                      | Uses the saved asset path of the active stage.                           |
| Use Project selection | You selected an asset before opening it.                                                      | Uses the selected asset path.                                            |
| Advanced path         | Unity selection cannot provide the intended asset.                                            | Lets you enter or correct a path below `Assets/`.                        |
| Reconnect             | The endpoint or network problem has been corrected.                                           | Starts a new connection and republishes relevant state.                  |
| Reserve               | You intend to begin work on a free coordinated path.                                          | Creates a developer-owned reservation.                                   |
| Release editing lease | You own the active edit but are pausing or finishing it.                                      | Releases the connection-owned editing claim.                             |
| Cancel reservation    | You no longer intend to start or resume the edit.                                             | Removes your developer-owned reservation.                                |
| Override…             | The remote owner has agreed to transfer the claim, or the team accepts an emergency takeover. | Transfers the exclusive claim after confirmation.                        |
| Copy path             | You need the normalized path for an announcement or ticket.                                   | Copies the selected `Assets/` path, even while disconnected or disabled. |
| Forget credentials    | A credential was revoked, may be exposed, or belongs to the wrong identity.                   | Disconnects and removes the local developer credential.                  |

## Save conflicts

Before Unity writes a coordinated scene, the save guard checks the current
server state. If another connection owns the editing lease, Unity presents a
conflict decision instead of treating the save as routine.

- Choose `Override and save` only after deliberate agreement or an accepted
  emergency. The takeover changes server ownership before the save proceeds.
- Choose `Cancel save` to stop this save attempt. The unsaved scene changes stay
  in the local editor.
- Choose `Keep working` when you need to inspect, copy, or separate the local
  changes first. The scene also remains unsaved.

Do not close Unity or discard changes merely because a save was blocked. First
preserve the local work, identify the remote owner, and agree on which branch
will keep each change.

During an outage or reconnect problem, Unity can offer Save locally without
coordination. It requires confirmation and records a local warning. It does not
create server history or make that save coordinated after the fact.

## Common situations

### You need to reserve a scene

Select the scene, confirm the helper text says `Reserve` is available, and
reserve it before editing. Tell the team what you are changing. Cancel the
reservation if the work no longer starts.

### Someone else owns the claim

Read the row's owner, branch, task, and expiry. Contact that developer first. An
override transfers ownership immediately after confirmation; it does not request
permission or merge either developer's local changes.

### You only need to inspect a scene

Opening the scene publishes presence, which is non-exclusive. Avoid making
Inspector changes while another developer owns the reservation or editing lease.
Unity can mark a scene dirty through small Inspector actions that seemed like
inspection.

### Your editor reconnects

Wait for Connection to report `Connected`, then confirm the open stage and
claims appear correctly. A new connection has a new connection identity. Do not
assume that a lease owned by the old connection belongs to the new one until the
current snapshot and local state agree.

### A claim expired unexpectedly

Check whether the owner is still working offline or has unsaved changes. Expiry
only says that the server stopped receiving valid renewal for that state. It
does not prove the local edit disappeared.

### The service is unavailable

1. Stop and identify the protected file you are about to edit.
2. Announce the file and risk manually.
3. Select the local Disabled switch if the service is unavailable or unhealthy.
4. Continue only if the team accepts manual coordination for that file.
5. Save locally only after Unity's confirmation prompts.
6. Reconnect after service health is restored. Do not describe the local save as
   coordinated.

## Troubleshooting

### The window says Offline

Check your network and ask the operator to verify the service. Use Reconnect
after the cause is fixed. Do not repeatedly toggle Disabled as a connection
repair attempt.

### The window says AuthenticationFailed

The developer credential may be missing, invalid, revoked, or unsafe to reuse.
Choose `Forget credentials`, obtain a new token through the approved channel,
and enter it in Unity. Revocation also invalidates existing sessions and closes
the affected live connections, so reconnecting with the same token will not
repair it.

### Buttons are disabled

Read the helper text and check, in order:

1. the target is a saved path below `Assets/`;
2. the path matches the active coordination rule;
3. Coordination is enabled;
4. Connection reports `Connected`;
5. the action is valid for the current owner and claim type.

### A claim looks stale

Refresh or reconnect, contact the named owner, and allow normal expiry to clean
up abandoned state. Use an override only after the team accepts the risk.

### The window warns about a local save

Tell the other developer, preserve the local diff, reconnect, and resolve any
conflict deliberately. The warning clears when the asset closes or coordination
later confirms local ownership.

### The path is outside the rule

The server is not malfunctioning. Use manual coordination for that file. Do not
change `coordination.json` merely to make a one-off button available; rule
changes affect the team and require their own reviewed task.

## Credentials and local settings

Unity stores the developer credential only in Windows Credential Manager. It
keeps the 24-hour session token in process memory. Closing the editor removes
that in-memory session from the client, though the server record remains valid
until expiry or revocation.

The ignored local file
`UserSettings/PotionPanic/coordination.local.json` may contain:

- task context;
- the local Disabled choice;
- an operator-directed server endpoint override.

It must never contain a developer credential, session token, administrative
secret, or HMAC key. The repository's `coordination.json` owns the shared
project ID, server endpoint, rules, and heartbeat interval. A local endpoint
override is for operator-directed development or recovery, not a convenient way
to escape a shared rule.

## Quick reference

| Situation                  | Action                                                                  |
|----------------------------|-------------------------------------------------------------------------|
| Starting scene work        | Announce, connect, set task context, choose the path, and reserve it.   |
| Scene open but unchanged   | Presence is visible; avoid accidental Inspector edits.                  |
| Scene becomes dirty        | Confirm that your connection obtains the editing lease.                 |
| You own an editing lease   | Save normally; release it or close the scene when finished.             |
| You own a reservation      | Edit soon or cancel it.                                                 |
| Someone else owns the path | Contact them; override only after a deliberate transfer decision.       |
| Connection returns         | Wait for the fresh snapshot and verify the current claim before saving. |
| The service is down        | Use Disabled, announce manually, and preserve local work.               |
| Authentication fails       | Forget the credential and obtain a new developer token.                 |

## Related pages

- [Project Setup](../onboarding/getting-started.md)
- [Daily Workflow](../collaboration/team-workflow.md)
- [Editor Safety](unity/editor-safety.md)
- [Coordination Server runbook](https://github.com/gabrielwawerski/PotionPanic/blob/master/Tools/CoordinationServer/README.md)
