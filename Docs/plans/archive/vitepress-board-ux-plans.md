---
title: 'VitePress Board UX Plans'
date: 2026-06-28
status: archived
archivedAt: 2026-08-08
originalPath: 'Docs/archive/completed/vitepress-board-ux-plans.md'
---

# VitePress Board UX Plans

## Summary

Recommended implementation order:

1. Build a shared assisted-entry system for metadata fields, then use it for
   `tags`, `milestone`, `dependencies`, and `documentation`.
2. Implement per-column manual ticket ordering as a separate slice afterward.

This order keeps the reusable modal-input work together, avoids one-off tag UX,
and isolates the riskier persistence change for drag ordering.

## Key Changes

### Shared Suggestion Infrastructure

- Add a board-level suggestion catalog that merges observed values from current
  tickets with optional curated seeds from [Docs/board.md](../../board.md).
- Add one reusable modal input component for assisted entry:
  - single-value combobox mode for `milestone`
  - multi-value chip/token mode for `tags`, `dependencies`, and
    `documentation`
- Support keyboard navigation, click-to-select, Enter-to-accept, duplicate
  prevention, backspace chip removal, and read-only mode.
- Keep all of these fields assistive, not restrictive: selecting a suggestion
  is fast, but custom text is still allowed.

### Task 2: Assisted Tags

- Replace the current free-text-only `TagEditor` flow with a token editor that
  suggests existing matching tags as the user types.
- Use two suggestion sources:
  - observed tags from loaded tickets
  - optional `ticketFieldSuggestions.tags` seeds from [Docs/board.md](../../board.md)
- Keep stored values as the same lowercase string tags already used today.
- Do not add a separate tag-management screen or enforce a closed tag
  vocabulary.

### Task 3: Assisted Milestone, Dependencies, And Documentation

- `milestone`:
  - switch from plain text input to a combobox
  - suggest existing milestone values from tickets, milestone page slugs such
    as `m-0`, and optional `ticketFieldSuggestions.milestones` seeds
  - store one string value exactly as today
- `dependencies`:
  - switch from plain textarea to a multi-select token editor
  - suggest current ticket IDs with human-readable labels like
    `PP-2 - Replace SampleScene...`
  - store the dependency value as the ticket ID string, not the label
  - hide or disable selecting the current ticket itself
  - still allow custom values for edge cases
- `documentation`:
  - switch from plain textarea to a multi-select token editor
  - suggest observed documentation refs plus discovered VitePress doc paths and
    optional `ticketFieldSuggestions.documentation` seeds
  - store repo-relative doc paths, preserving current link-building behavior
  - still allow custom values for non-site files or unusual references
- Build the suggestion catalog in the local docs plugin so it works in dev and
  in the built site, rather than recomputing everything only in the client.

### Task 1: Per-Column Manual Ordering

- Add a numeric `order` field to ticket frontmatter and `Ticket` data.
- Change board sorting rules:
  - primary sort: `order` within each status column
  - fallback for legacy tickets without `order`: current ID order
- Replace the current column-only drag-drop with insertion-based drag-drop so a
  ticket can be dropped above or below another ticket, not only into a column.
- When moving within a column, renumber only that column's visible persisted
  order.
- When moving across columns, insert at the chosen position in the target
  column or append to the end if dropped on empty column space.
- Persist reorder changes with a batch update path in the markdown writer
  plugin, because one move usually changes multiple tickets' `order` values.
- New tickets should get the next `order` at the end of their starting column.
- Status-only changes that do not specify an insertion point should append to
  the end of the target column.

## Public Interface Changes

- Add `order?: number` to ticket frontmatter and runtime `Ticket` objects.
- Add board frontmatter support for `ticketFieldSuggestions.tags`,
  `ticketFieldSuggestions.milestones`,
  `ticketFieldSuggestions.dependencies`, and
  `ticketFieldSuggestions.documentation`.
- Add a generated suggestion-catalog asset or endpoint for the board, sourced
  by the local markdown writer/plugin.
- Add a batch ticket-update write path for multi-ticket reorder persistence.

## Test Plan

- Tags: suggested tags appear from observed values and board seeds; keyboard
  and click selection work; freeform tag creation still works; duplicates are
  not added.
- Milestone: combobox suggests existing and seeded milestone values; custom
  milestone text still persists; clearing the value removes frontmatter
  cleanly.
- Dependencies: suggestions show ticket ID plus title; selecting a dependency
  stores only the ticket ID string; duplicate and self-dependency selection is
  blocked in the UI; custom dependency text still persists.
- Documentation: suggestions include known docs paths; selected docs keep link
  resolution working; custom non-site references still persist as plain text
  entries.
- Suggestion catalog: works in local dev and built docs output; includes
  configured seeds even on a nearly empty board.
- Ordering: legacy tickets without `order` still render in stable ID order;
  drag within one column persists the new order after reload; drag across
  columns persists both new status and new position; filtered or tag-filtered
  views do not corrupt stored order; empty-column drop appends correctly; batch
  write updates all affected tickets consistently.

## Assumptions

- Suggestion UX is meant to accelerate entry, not force strict controlled
  vocabularies.
- [Docs/board.md](../../board.md) remains the single board-level configuration source.
- Documentation suggestions should prioritize VitePress-site docs; non-site
  files remain allowed via custom entry.
- Manual ordering is per status column, not one board-wide global sequence.
- The assisted-entry work for tasks 2 and 3 should be implemented as one shared
  slice, not four separate one-off field rewrites.
