---
id: 7
title: Implement coordinated Unity file leasing
status: todo
priority: high
documentation:
  - plans/coordinated-file-leasing-system.md
  - onboarding/getting-started.md
  - collaboration/team-workflow.md
  - guides/unity/editor-safety.md
affectedFiles:
  - coordination.json
  - Assets/Scripts/Editor/Coordination
  - Assets/Tests/EditMode/Coordination
  - Assets/Tests/EditMode/PotionPanic.EditModeTests.asmdef
  - Tools/CoordinationServer
  - .github/workflows/coordination-server.yml
tags: []
order: 1
assignee: Codex
---

## Description

Implement the tracked coordination program for advisory Unity scene and
selected-prefab presence, leases, reservations, conflict-safe saving, and a
Cloudflare Durable Object backend. Execute one linked plan slice per Codex
session, in dependency order.

## Acceptance Criteria

- [ ] The accepted behavior and verification criteria in
  [`../plans/coordinated-file-leasing-system.md`](../plans/coordinated-file-leasing-system.md)
  are met.
- [ ] Two Windows Unity editors can coordinate from different networks without
  exposing developer or session tokens.
- [ ] Offline mode preserves local work and manual collaboration remains the
  documented fallback.

## Implementation Plan

Follow the nine linked implementation slices in dependency order. Each session
must record its commit hash, verification output, and handoff result in
Implementation Notes. Do not combine slices or mark the ticket complete before
the release-acceptance slice records two-machine evidence.

## Implementation Notes

2026-08-06: Plan split into nine independent Codex session slices. PP-8
restored the root documentation test baseline; `npm test` and
`npm run docs:build` pass. No coordination server or Unity client
implementation has started.

2026-08-06: Slice 01 foundations committed as
`d6563ed868b6b359024ba1ec179c683f5a452313`
(`feat(coordination): scaffold backend and configuration`). Commands passed:
`Tools/CoordinationServer`: `npm ci`, `npm run typecheck`, `npm test` (49),
and `npx wrangler deploy --dry-run`; repository root: `npm test` (11) and
`npm run docs:build`. The Unity Coordination EditMode command could not run:
Unity 6000.5.1f1 exited with code 198 before compilation because no valid
Editor license is activated. `npm ci` also reported four Worker dependency
audit findings (three moderate, one high); no dependency upgrade was made in
this slice.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Backend and Unity verification completed
- [ ] Two-machine acceptance evidence recorded
- [ ] Required evergreen documentation updated after release acceptance
- [ ] Branch committed and ready for review or merge

## Notes
