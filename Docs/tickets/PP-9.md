---
id: 9
title: Decide and align Coordination outage behavior
status: backlog
priority: high
documentation:
  - guides/coordinated-leasing.md
  - onboarding/getting-started.md
  - collaboration/team-workflow.md
  - plans/coordination-save-safety-and-manual-mode.md
affectedFiles:
  - Assets/Scripts/Editor/Coordination
  - Assets/Tests/EditMode/Coordination
  - Docs/guides/coordinated-leasing.md
  - Docs/onboarding/getting-started.md
  - Docs/collaboration/team-workflow.md
  - Tools/CoordinationServer/README.md
tags: []
order: 6
---

## Description

Resolve the known mismatch between the documented outage policy and the current Unity save paths. The explicit local `Disabled` state currently bypasses Coordination save checks, while an enabled client that is offline, reconnecting, or timed out uses the uncoordinated-save confirmation and warning path.

This ticket owns the decision and any approved runtime or documentation change. Evergreen Documentation V2 deliberately preserved the disputed passages and did not claim that either policy is correct.

## Acceptance Criteria

- [ ] Reverify the explicit Disabled and enabled-but-unavailable save behavior against the current Unity source and focused tests.
- [ ] Record the consequences of recommending the enabled fallback, recommending the explicit Disabled opt-out, or changing runtime behavior.
- [x] Obtain an explicit team decision for one consistent policy.
- [ ] Implement any approved runtime and test changes without discarding local Unity work.
- [ ] Update the Unity Coordination Guide, Project Setup, Daily Workflow, and coordination server runbook, including their quick-reference and troubleshooting passages.
- [ ] Run a manual Unity editor smoke that exercises both the explicit Disabled path and the enabled outage or reconnect path.
- [ ] Record observed prompts, warnings, save results, and remaining limits without treating an unperformed path as verified.

## Implementation Plan

Follow the active
[Coordination Save Safety and Manual Mode](../plans/coordination-save-safety-and-manual-mode.md)
implementation plan. It records the approved save-state matrix, persistence contract, test-first implementation order, documentation changes, and manual acceptance procedure.

## Implementation Notes

- 2026-08-09: Current step: Implement. Work continues on
  `fix/coordination-outage-policy` from clean `master` at `359e9e6` or later.
- 2026-08-09: Approved the Current Asset First IMGUI workflow. The active scene or Prefab Stage is the default action target; explicit Project-selection and manual targets stay session-only; inspecting team rows does not replace the action target.
- 2026-08-09: Team rows remain visible as clearly stale, read-only data while Manual, Offline, Reconnecting, or AuthenticationFailed. Lease mutations require Connected state and a complete authoritative snapshot for the current session.
- 2026-08-09: The selected generated mock is directional only. Production must use real Coordination fields and Unity-native controls, remain usable at 430, 560, and 900 pixel widths, and pass a recorded visual comparison.
- 2026-08-09: Approved **Coordinated** and **Manual** as the user-facing modes. Manual mode is an intentional opt-out, not the recommended response to a temporary outage.
- 2026-08-09: Approved the same two-step guarded local-save flow for Manual, Offline, Reconnecting, authentication failure, request timeout, and failed override transport. A connected remote owner still has no direct local-save bypass.
- 2026-08-09: Approved durable, per-asset local warnings that remain until explicit reconciliation. Reconnection, later lease acquisition, and editor restart do not clear them.
- Opened after the Evergreen Documentation V2 rework. No outage-policy passage or Unity runtime behavior was changed or validated by V2.

## Definition of Done

- [x] Policy decision approved and recorded
- [ ] Runtime behavior and documentation agree
- [ ] Automated verification passes
- [ ] Manual Disabled and enabled-outage observations recorded
- [ ] Branch committed and ready for review or merge

## Notes
