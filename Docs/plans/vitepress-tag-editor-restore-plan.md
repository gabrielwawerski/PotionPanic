---
date: 2026-06-29
---

# Restore `TagEditor` As The Editable Tags UI

## Summary

Restore `TagEditor` for editable ticket tags while keeping the current tag
suggestion catalog and persistence flow intact. The simplest path is to make
`TagEditor` suggestion-aware for tags only, then switch `TicketDetail` back to
using it in editable mode. Leave `TokenSuggestionInput` in place for
dependencies, documentation, and affected files.

## Implementation Changes

- Extend `TagEditor` to support optional tag suggestions:
  - accept `options` and optional `normalizeValue` props
  - keep existing chip-style add/remove interaction and lowercase
    normalization for tags
  - show a lightweight filtered suggestion list below the input when focused
  - support click-to-select, Enter to accept, ArrowUp/ArrowDown navigation,
    duplicate prevention, and backspace removal of the last tag
  - preserve freeform tag entry when no suggestion is chosen

- Update `TicketDetail` tag editing to use `TagEditor` in editable mode again:
  - replace the editable `TokenSuggestionInput` branch in the Tags field with
    `TagEditor`
  - pass the existing `tagSuggestionOptions` values into `TagEditor`
  - keep the current `updateTags`/tag patch flow so persistence behavior does
    not change

- Do not change the existing board suggestion catalog or writer endpoints:
  - `suggestions.tags` remains the source of observed/seeded tag suggestions
  - `Board.vue`, `useTicketWriter.ts`, and the markdown writer plugin stay
    functionally unchanged for tag persistence

- Keep `TokenSuggestionInput` unchanged for non-tag metadata fields:
  - dependencies, documentation, and affected files continue using the current
    token-input UI
  - this keeps the restore scoped only to tags

## Test Plan

- Add or update UI-source tests to verify the Tags field in `TicketDetail.vue`
  uses `TagEditor` for editable tickets instead of `TokenSuggestionInput`.
- Add focused `TagEditor` tests covering:
  - adding a freeform tag
  - selecting a suggested tag
  - preventing duplicate tags
  - backspace removing the last tag when the input is empty
  - suggestion filtering and keyboard selection
- Run `npm test` and `npm run docs:build` to confirm the component compiles and
  the docs site still builds cleanly.

## Assumptions

- “Restore `TagEditor`” means restoring it as the editable tags UI, not
  restoring the old no-suggestions behavior.
- Tag suggestions should remain assistive, not restrictive.
- Only the tags field should change; other token-based metadata editors stay
  as-is.
