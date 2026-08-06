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

Implement the tracked coordination plan for advisory Unity scene and selected
prefab presence, leases, reservations, conflict-safe saving, and a Cloudflare
Durable Object backend.

## Acceptance Criteria

- [ ] The accepted behavior and verification criteria in
  [`../plans/coordinated-file-leasing-system.md`](../plans/coordinated-file-leasing-system.md)
  are met.
- [ ] Two Windows Unity editors can coordinate from different networks without
  exposing developer or session tokens.
- [ ] Offline mode preserves local work and manual collaboration remains the
  documented fallback.

## Implementation Plan

Follow the active implementation plan task by task. Record commit hashes,
verification output, deployment details, and two-machine acceptance evidence in
Implementation Notes.

## Implementation Notes

2026-08-06: Plan revised before implementation. PP-8 restored the root
documentation test baseline; `npm test` and `npm run docs:build` pass. No
coordination server or Unity client implementation has started.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Backend and Unity verification completed
- [ ] Two-machine acceptance evidence recorded
- [ ] Required evergreen documentation updated after release acceptance
- [ ] Branch committed and ready for review or merge

## Notes
