---
date: 2026-06-29
---

# Restore `TagEditor` As The Editable Tags UI With Tag Color Editing

## Summary

Restore `TagEditor` for editable ticket tags while keeping the current tag
suggestion catalog and ticket persistence flow intact. The simplest path is to
make `TagEditor` suggestion-aware for tags only, switch `TicketDetail` back to
using it in editable mode, and add minimal color editing for each tag through
the existing board frontmatter `tagColors` map. This work belongs in the
Docboard package so PotionPanic gets the behavior automatically through its
local package dependency.

## Implementation Changes

- Extend `TagEditor` to support optional tag suggestions:
    - accept `options` and optional `normalizeValue` props
    - keep existing chip-style add/remove interaction and lowercase
      normalization for tags
    - show a lightweight filtered suggestion list below the input when focused
    - support click-to-select, Enter to accept, ArrowUp/ArrowDown navigation,
      duplicate prevention, and backspace removal of the last tag
    - preserve freeform tag entry when no suggestion is chosen
    - accept current `tagColors` and an optional `updateTagColor` callback
    - show a native `<input type="color">` swatch beside each editable tag chip
    - keep read-only tag rendering unchanged except for using configured colors

- Update `TicketDetail` tag editing to use `TagEditor` in editable mode again:
    - replace the editable `TokenSuggestionInput` branch in the Tags field with
      `TagEditor`
    - pass the existing `tagSuggestionOptions` values into `TagEditor`
    - pass `tagColors` and `updateTagColor` through from `Board.vue`
    - keep the current `updateTags`/tag patch flow for ticket tag persistence

- Add the smallest Docboard-owned board-frontmatter writer path for colors:
    - add a `useTicketWriter` method for updating board-level `tagColors`
    - add a markdown writer endpoint that updates only the current board page's
      `tagColors` frontmatter
    - call it from `Board.vue` when a tag color changes, then update local
      `tagColors` state so the UI reflects the change immediately
    - do not write tag colors into individual ticket markdown files

- Keep the existing board suggestion catalog unchanged:
    - `suggestions.tags` remains the source of observed/seeded tag suggestions
    - tag colors remain a separate `tagColors` frontmatter map keyed by tag text

- Keep `TokenSuggestionInput` unchanged for non-tag metadata fields:
    - dependencies, documentation, and affected files continue using the current
      token-input UI
    - this keeps the restore scoped only to tags

- Keep PotionPanic host changes minimal:
    - do not add PotionPanic-specific UI code
    - the active board and archive board can each keep their own `tagColors`
      frontmatter; the editor updates the currently open board file only

## Test Plan

- Add or update UI-source tests to verify the Tags field in `TicketDetail.vue`
  uses `TagEditor` for editable tickets instead of `TokenSuggestionInput`.
- Add focused `TagEditor` tests covering:
    - adding a freeform tag
    - selecting a suggested tag
    - preventing duplicate tags
    - backspace removing the last tag when the input is empty
    - suggestion filtering and keyboard selection
    - rendering the native color input for editable tag chips
- Add focused writer tests covering:
    - updating `tagColors` in board frontmatter without changing ticket files
    - preserving existing board frontmatter when a tag color changes
    - ignoring color updates for non-board or out-of-docs paths
- Run `npm test` and `npm run docs:build` to confirm the component compiles and
  the docs site still builds cleanly.

## Assumptions

- “Restore `TagEditor`” means restoring it as the editable tags UI, not
  restoring the old no-suggestions behavior.
- Tag suggestions should remain assistive, not restrictive.
- Only the tags field should change; other token-based metadata editors stay
  as-is.
- Tag colors are board-level metadata, not ticket metadata.
- The first implementation edits colors for the currently open board page only;
  shared/global tag color management can wait until more than one board needs
  central color ownership.
- Use native browser color input instead of adding a color picker dependency.
