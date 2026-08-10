---
id: 8
title: Restore Docboard package-wiring test baseline
status: done
priority: medium
documentation:
  - .vitepress/config.mts
affectedFiles:
  - Scripts/docs/lib/docboard-package-wiring.test.mjs
  - Docs/.vitepress/config.mts
tags:
  - docs-workflow
order: 5
---

## Description

Restore the root documentation test baseline. The package-wiring test imports
`Docs/.vitepress/project-docs.config.ts`, but the current checked-in managed
configuration is `Docs/.vitepress/config.mts`.

## Acceptance Criteria

- [x] The test validates the current managed Docboard configuration without
  importing a missing compatibility file.
- [x] `npm test` passes with no failed subtests.
- [x] The revised assertions reflect the checked-in navigation, plan archive,
  and theme configuration rather than historical values.

## Implementation Plan

1. Write an assertion against the current managed configuration that fails for
   the missing import or stale navigation contract.
2. Update the test to consume the current exported configuration shape.
3. Run the focused test, then the root `npm test` suite and
   `npm run docs:build`.

## Implementation Notes

Created separately from PP-7 so the coordination feature does not conceal a
pre-existing documentation test failure.

2026-08-06: Replaced the deleted `project-docs.config.ts` import with the
managed `config.mts` export and updated assertions for the current navigation,
plan archive, and managed theme banner. Focused package-wiring tests passed.

## Definition of Done

- [x] Acceptance criteria met
- [x] `npm test` passes
- [x] Documentation build passes
- [ ] Branch committed and ready for review or merge

## Notes
