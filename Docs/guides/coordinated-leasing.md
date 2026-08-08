# Coordinated Leasing Guide

Use this guide before editing a coordinated Unity scene. It explains the
Coordination window, the normal scene-editing flow, and what to do when a claim
or the service blocks your work.

Coordinated leasing is an advisory safety layer. It makes activity visible and
makes risky saves deliberate. It does not lock files on disk, replace Git, or
replace a direct announcement to the team.

## Before You Start

The current rule coordinates Unity scenes below <code>Assets/Scenes/</code>,
including <code>Assets/Scenes/SampleScene.unity</code>:

    Assets/Scenes/**/*.unity

Always announce before editing a scene, prefab, ProjectSettings file, or
package file. Prefabs, ProjectSettings, and packages still use manual
coordination unless a verified rule explicitly covers them.

## First-Time Setup

Complete [Project Setup](../onboarding/getting-started.md) first. Then:

1. Pull the latest master and open the project with Unity 6000.5.1f1.
2. Open Window > Potion Panic > Coordination.
3. Get a developer token from the operator through the approved secret channel.
4. Paste it into the credential prompt and confirm Connection is Connected.
5. Enter a short Task context, such as PP-7 or lab blockout.

The task context is local to this machine. It lets others see why you hold a
claim. If authentication fails, choose Forget credentials, obtain a new token
through the approved channel, and enter it again. Never put a token in Git, a
ticket, a URL, a log, or chat.

## Edit a Coordinated Scene

Use this workflow before editing SampleScene.unity or another coordinated scene.

1. Pull the latest master, choose a task, and announce the scene you expect to edit.
2. Open the Coordination window, confirm Connection is Connected, and set the
   Task context.
3. Choose the scene with Use active stage, Use Project selection, a claim row,
   or Advanced path.
4. If the target is free, select Reserve before you begin.
5. Open and edit the scene. Opening it publishes presence; a meaningful change
   to a dirty scene attempts to obtain an editing lease.
6. Save normally. If Unity reports another owner, follow [Save conflicts](#save-conflicts).
7. When finished, close the scene or release your editing lease, cancel an
   unused reservation, push your branch, and communicate the handoff.

## Coordination Window

Open the window from Window > Potion Panic > Coordination.

### Local Status

The top of the window shows your identity, Git branch, and connection state.
Confirm that they represent the right person and branch before claiming a scene.

Task context is a short explanation visible with your claims. Keep it focused on
the task, not a status diary.

Disabled turns off coordination on this machine. Use it only when the service is
unavailable or the team has agreed to manual coordination. It does not delete
local work and does not make later saves coordinated.

### Claims and Presence

| List | Meaning | What to do |
| --- | --- | --- |
| Presence | Someone has the asset open. It is informational and non-exclusive. | Check the owner and communicate before overlapping work. |
| Editing leases | One connection owns the active edit claim. The save guard checks this claim. | Let the owner finish, or use an agreed override. |
| Reservations | A developer has announced an intended edit before starting. | Avoid starting the same work; cancel your own unused reservation. |

Rows show the path, owner, branch, task, and expiry. Local means the row is
yours. Select a row to make it the action target. Local rows offer release or
cancel actions; remote claims offer Override… after confirmation.

Presence and editing leases expire when a client does not stay connected.
Reservations last longer because they represent a developer's intent rather
than one open Unity connection.

### Action Target and Helper Text

The action target is the path affected by the buttons. Choose it from a row,
the active Unity stage, the Project window selection, or a typed path under
Assets/.

Read the helper text below the buttons before assuming a button is broken. It
explains whether the target is outside Assets/, outside the rule, disconnected,
disabled, already owned, or ready to reserve.

## Window Actions

| Control | Purpose and common use case | Effect | Available when |
| --- | --- | --- | --- |
| Use active stage | Target the scene or prefab stage open in Unity. | Replaces the action target with the active stage path. | The active stage is a saved asset under Assets/. |
| Use Project selection | Target an asset selected in the Project window before opening it. | Replaces the action target with the selected asset path. | The Project selection is an asset under Assets/. |
| Advanced path | Enter a target when the Unity selection methods cannot provide it. | Lets you type or correct an Assets/ path. | Always visible; claim actions still require a valid coordinated path. |
| Reconnect | Retry after an outage, endpoint correction, or network problem. | Starts a new connection attempt. | Windows editor, coordination enabled, and not already reconnecting. |
| Reserve | Announce your intended edit before starting work. | Creates your reservation. | Connected, enabled, coordinated target, and no claim exists. |
| Release editing lease | Finish or pause an active edit you own. | Releases your connection's editing lease. | Connected, enabled, and you own the target's editing lease. |
| Cancel reservation | Give up an edit you no longer plan to start. | Removes your reservation. | Connected, enabled, and you own the target's reservation. |
| Override… | Take a remotely owned reservation or editing lease after agreement. | Transfers the remote claim after confirmation. | Connected, enabled, and another developer owns the selected claim. |
| Copy path | Copy the normalized path for an announcement, ticket, or manual check. | Copies the selected Assets/ path. | A valid action target exists, including while disconnected or disabled. |
| Forget credentials | Remove the local developer token after revocation, suspected exposure, or an identity change. | Disconnects and removes the local credential. | Windows editor. |

## Common Situations

### You Need to Reserve a Scene

Select the scene, confirm the helper text says Reserve is available, and reserve
it before editing. A reservation is not a substitute for an announcement. Tell
the team what you are changing and cancel it if the work no longer starts.

### Someone Else Owns the Claim

Read the row's owner, branch, task, and expiry. Contact that developer first.
Use Override… only after explicit agreement or when the team accepts the risk
of taking over an unreachable owner's claim. An override transfers ownership; it
does not request permission.

### Save Conflicts

If another connection owns a coordinated scene when you save, Unity shows a
conflict dialog.

- Choose Override and save only after deliberate agreement or an accepted emergency.
- Choose Cancel save to stop the current save attempt. It leaves the scene's
  local changes unsaved and intact.
- Choose Keep working when you need to inspect or copy those local changes
  first. It also leaves the scene's local changes unsaved and intact.

During an outage or reconnect problem, Unity can offer Save locally without
coordination. It requires confirmation and records a local warning. It does not
create server history or make that save coordinated after the fact.

### The Service Is Unavailable

1. Stop and identify the protected file you are about to edit.
2. Announce the file and risk manually.
3. Select the local Disabled switch if the service is unavailable or unhealthy.
4. Continue only if the team accepts manual coordination for that file.
5. Save locally only after Unity's confirmation prompts.
6. Reconnect after service health is restored. Do not describe the local save
   as coordinated.

## Troubleshooting

### The window says Offline

Check your network and ask the operator to verify the service. Use Reconnect
after the cause is fixed. Do not repeatedly toggle Disabled as a connection
repair attempt.

### The window says AuthenticationFailed

Your token may be missing, invalid, revoked, or unsafe to reuse. Choose Forget
credentials, obtain a new token through the approved channel, and enter it in
Unity.

### Buttons are disabled

Check the helper text. Common reasons are an invalid target, a path outside
Assets/, a path outside the current rule, a disconnected window, Disabled, or a
claim that no longer belongs to you.

### A claim looks stale

Try Reconnect and wait for the claim to expire before assuming it is gone. Use
an override only after the team accepts the risk.

### The window warns about a local save

Tell the other developer, preserve the local diff, reconnect, and resolve any
conflict deliberately. The warning clears when the asset closes or coordination
later confirms local ownership.

## Quick Reference

| Situation | Action |
| --- | --- |
| Starting scene work | Announce, connect, set task context, choose the path, and reserve it. |
| You own an editing lease | Save normally; release it or close the scene when finished. |
| You own a reservation | Edit soon or cancel it. |
| Someone else owns the path | Coordinate directly; override only deliberately. |
| The service is down | Use Disabled, announce manually, and preserve local work. |
| Authentication fails | Forget the credential and obtain a new developer token. |

## Technical Reference

The system is advisory. Git, Unity, Rider, and the filesystem can still write
the file, so announcements remain required for protected Unity work.

The current rule lives in repository-root coordination.json and covers
Assets/Scenes/**/*.unity. Changing rules or the configured endpoint is an
operator task, not a recovery step for ordinary contributors.

Unity stores a developer token only in Windows Credential Manager. It keeps the
session token in memory. The ignored local settings file
UserSettings/PotionPanic/coordination.local.json may contain a task context, the
Disabled choice, and an operator-directed endpoint override; it must never
contain a token or secret.

## Related pages

- [Project Setup](../onboarding/getting-started.md)
- [Daily Workflow](../collaboration/team-workflow.md)
- [Editor Safety](../unity-guides/editor-safety.md)

## For Operators

Use the [Coordination Server README](https://github.com/gabrielwawerski/PotionPanic/blob/master/Tools/CoordinationServer/README.md)
for local Worker development, deployment, health verification, developer-token
issuance and revocation, monitoring, outage recovery, and secret rotation.
