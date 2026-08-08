# Coordination Window Actions and Reservation Cancellation

**Current step:** Local implementation and verification complete; production
deployment and two-machine acceptance remain gated.

## Goal

Make Coordination-window claims directly actionable without requiring a
separately typed path. Add an explicit protocol request for cancelling a
reservation while preserving the distinct ownership rules of editing leases.

## Implementation

1. Add the path-only `reservation.cancel` request to Protocol v1. The backend
   authorizes it by developer, deletes only that developer's reservation, and
   returns the existing correlated `lease.released` event.
2. Add client service support, typed row kinds, path-oriented row selection,
   active-stage and Project-selection path sources, contextual actions, and
   deterministic disabled-action guidance.
3. Require confirmation before any window override and re-resolve the current
   claim immediately before sending an action.
4. Update the protocol and release documentation without changing Task 6's
   external acceptance status.

## Proof

- Write and run failing backend protocol, authoritative-state, and WebSocket
  tests before implementing `reservation.cancel`.
- Write and run failing Unity protocol, service, and window view-model tests
  before implementing client and UI behavior.
- Run backend typecheck, full tests, audit, and Wrangler dry run.
- Run focused and full Unity Coordination EditMode suites with Unity
  `6000.5.1f1` without `-quit`.
- Run the root documentation tests and VitePress build.
- Manually smoke the Coordination window. Do not mark Task 6 acceptance rows
  complete without the required two-machine observations.

## Rollout Boundary

The updated Worker must be deployed before the cancellation action is used by
updated clients. Deployment, credentials, tokens, `coordination.json`, and the
two-machine acceptance matrix require their existing separate gates.

## Verification Chronicle

- Backend: typecheck passed, 104/104 tests passed, audit reported zero
  vulnerabilities, and Wrangler `4.120.0` dry run passed.
- Unity `6000.5.1f1`: focused protocol, service, and window-view-model suites
  passed; the final full Coordination suite passed 212/212.
- Documentation: 16/16 tests and the VitePress build passed.
- Manual Play Mode: the new window rendered with zero Console errors. A stale
  IMGUI keyboard-focus transfer found during the first run was fixed and the
  repeat run preserved the blank task context.
- Not verified: live `reservation.cancel`, production deployment, Machine B,
  two-machine acceptance, and filtered Wrangler tail evidence.
