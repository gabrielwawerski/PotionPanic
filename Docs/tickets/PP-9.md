---
id: 9
title: Decide and align Coordination outage behavior
status: backlog
priority: high
documentation:
  - guides/coordinated-leasing.md
  - onboarding/getting-started.md
  - collaboration/team-workflow.md
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

Resolve the known mismatch between the documented outage policy and the current
Unity save paths. The explicit local `Disabled` state currently bypasses
Coordination save checks, while an enabled client that is offline,
reconnecting, or timed out uses the uncoordinated-save confirmation and warning
path.

This ticket owns the decision and any approved runtime or documentation change.
Evergreen Documentation V2 deliberately preserved the disputed passages and did
not claim that either policy is correct.

## Acceptance Criteria

- [ ] Reverify the explicit Disabled and enabled-but-unavailable save behavior
  against the current Unity source and focused tests.
- [ ] Record the consequences of recommending the enabled fallback,
  recommending the explicit Disabled opt-out, or changing runtime behavior.
- [ ] Obtain an explicit team decision for one consistent policy.
- [ ] Implement any approved runtime and test changes without discarding local
  Unity work.
- [ ] Update the Unity Coordination Guide, Project Setup, Daily Workflow, and
  coordination server runbook, including their quick-reference and
  troubleshooting passages.
- [ ] Run a manual Unity editor smoke that exercises both the explicit Disabled
  path and the enabled outage or reconnect path.
- [ ] Record observed prompts, warnings, save results, and remaining limits
  without treating an unperformed path as verified.

## Implementation Plan

1. Audit the current save guard, resume coordinator, local settings, warning
   state, and focused EditMode tests.
2. Write a short decision record comparing the three policy options and their
   failure consequences.
3. Obtain approval before changing behavior or guidance.
4. Implement the approved runtime and test changes, if any.
5. Align all four evergreen and operator documents with the verified behavior.
6. Run focused EditMode tests, the full Coordination EditMode suite, root docs
   tests, the VitePress build, and the two-path manual editor smoke.

## Implementation Notes

Opened after the Evergreen Documentation V2 rework. No outage-policy passage or
Unity runtime behavior was changed or validated by V2.

## Definition of Done

- [ ] Policy decision approved and recorded
- [ ] Runtime behavior and documentation agree
- [ ] Automated verification passes
- [ ] Manual Disabled and enabled-outage observations recorded
- [ ] Branch committed and ready for review or merge

## Notes
