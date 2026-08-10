---
title: 'Coordination Window Actions and Reservation Cancellation'
status: archived
archivedAt: 2026-08-08
originalPath: 'Docs/plans/coordinated-file-leasing-window-actions.md'
---

# Coordination Window Actions and Reservation Cancellation

## Summary

Implement the follow-up on a short-lived `feature/coordination-window-actions`
branch. Add path-oriented row selection, convenient Unity path sources,
contextual actions, actionable validation text, and a dedicated
`reservation.cancel` protocol request.

Keep protocol version 1. Deploy the backend before distributing the updated
Unity client, but treat production deployment as a separate approval gate. Task
6 and PP-7 remain open until the deferred two-machine acceptance matrix is
completed.

## Protocol and Backend

- Add `reservation.cancel` as a path-only client message requiring
  `protocolVersion`, UUID-v4 `requestId`, and `path`. Reject context fields such
  as `branch` and `task`.
- Handle cancellation inside the existing authoritative transaction and replay
  machinery:
    - Find the reservation by canonical path.
    - Authorize by `developer_id`, allowing cancellation from a different
      connection or recreated session belonging to the same developer.
    - Delete only the reservation and advance `stateVersion` exactly once.
    - Return and broadcast the existing `lease.released` envelope with the
      reservation ID as `leaseId`; include the requester’s `requestId`.
    - Deny missing or foreign reservations with `reservation_not_owned` and the
      current effective claim.
- Extend `reservationReleasedChanges` to accept an optional request ID,
  preserving existing expiry and revocation behavior.
- Keep editing-lease release unchanged: `lease.release` remains connection-owned
  and cannot cancel reservations.
- Update the TypeScript and C# protocol catalogues and add
  `TryCancelReservation` to `ICoordinationWindowService` and
  `CoordinationService`.

## Unity Window

- Model each row with an explicit kind: presence, editing lease, or reservation.
- Make selection path-oriented:
    - Clicking any row sets the current action target.
    - Every row with that path receives the selected treatment.
    - Keep the target selected after release or cancellation so it can
      immediately be reserved again.
- Replace the primary editable field with an action-target section:
    - Display the normalized current target.
    - Add `Use active stage`, preferring the open Prefab Stage and otherwise
      using `SceneManager.GetActiveScene().path`.
    - Add `Use Project selection`, resolving `Selection.activeObject` through
      `AssetDatabase.GetAssetPath`.
    - Keep editable manual input inside a collapsed, non-persisted
      `Advanced path` foldout.
    - Normalize all three sources through `CoordinationPathMatcher`.
- Retain target-level actions with explicit labels:
    - `Reserve`
    - `Release editing lease`
    - `Cancel reservation`
    - `Override…`
    - `Copy path`
- Add row actions:
    - Presence: `Copy path`
    - Local editing lease: `Release editing lease`, `Copy path`
    - Local reservation: `Cancel reservation`, `Copy path`
    - Remote editing lease or reservation: `Override…`, `Copy path`
- Route row actions through the view-model after first selecting their path.
  Re-resolve current authoritative state before sending so stale rows cannot
  trigger an incorrect operation.
- Require confirmation for both row-level and target-level `Override…`. Show the
  path and current owner. If the claim changes before submission, rely on the
  server’s authoritative denial and refresh normally.
- Add an injected path-source abstraction and an injected override-confirmation
  abstraction so view-model behavior remains unit-testable without static Unity
  APIs.
- Show one contextual HelpBox beneath the target:
    - Empty: instruct the user to select a row, active stage, Project asset, or
      advanced path.
    - Invalid/outside `Assets/`: explain that an asset under `Assets/` is
      required.
    - Unmatched rule: explain that the path is not coordinated.
    - Disabled or disconnected: explain why mutations are unavailable and that
      copying still works.
    - Unclaimed: state that reservation is available.
    - Local editing/reservation: identify the applicable release or cancellation
      action.
    - Remote claim: identify its owner and state that override requires
      confirmation.
- Use text labels and a visible selected style, without relying only on color.
  Unsaved scenes, folders, empty selections, and non-asset objects must produce
  helper text rather than exceptions.

## Tests and Documentation

- Write failing tests before production changes.
- Backend tests:
    - Parse and reject malformed `reservation.cancel` envelopes.
    - Same-developer cancellation succeeds across connections and recreated
      sessions.
    - Foreign or missing reservations are denied without mutation.
    - Success removes the reservation, advances state once, correlates the
      requester response, broadcasts once, and replays idempotently.
    - Existing expiry, revocation, editing release, reservation restoration,
      override, and capacity behavior remain intact.
- Unity tests:
    - Service serializes `reservation.cancel` and completes its request exactly
      once on `lease.released`.
    - Row kinds, path-level selection, highlighting state, and every contextual
      action are correct.
    - Target-level action enablement distinguishes local editing, local
      reservation, remote claim, and free path.
    - Active-stage and Project-selection results populate and normalize the
      target.
    - Empty, invalid, uncoordinated, disabled, disconnected, local, and remote
      helper states have deterministic messages.
    - Override cancellation sends nothing; confirmation sends one request.
- Verification gates:
    - From `Tools/CoordinationServer`: focused Vitest files,
      `npm run typecheck`, `npm test`, `npm audit`, and Wrangler dry run.
    - Read the Cloudflare and Wrangler skills before running Wrangler. Do not
      deploy during this implementation without separate approval.
    - Run focused Unity EditMode suites for protocol, service, and window
      view-model, then the full Coordination suite with Unity `6000.5.1f1` and
      without `-quit`.
    - Manually smoke active scene, Prefab Stage, Project selection, advanced
      input, row selection, disabled explanations, copy, release, cancellation,
      and override confirmation.
    - Run root `npm test` and `npm run docs:build`.
- Save this implementation plan under `Docs/plans/`, update the protocol table
  in `coordinated-file-leasing-system.md`, refine the Task 6 matrix to record
  explicit reservation cancellation separately from cancelling a save conflict,
  and append factual results and remaining deployment/acceptance blockers to
  PP-7.
- Do not mark any Task 6 acceptance row complete from automated tests or a
  one-machine smoke. No scene, prefab, project settings, credentials, tokens,
  secrets, or `coordination.json` changes are required.

## Rollout Assumptions

- The additive request remains protocol version 1; old clients continue working,
  but the new cancellation button requires the updated Worker.
- Production rollout order is backend deployment, health and deployment
  verification, then updated Unity clients.
- Deployment, Wrangler tail collection, and two-machine acceptance require their
  existing explicit gates.
- Stage and Project-selection choices are transient and are not stored in user
  settings.
