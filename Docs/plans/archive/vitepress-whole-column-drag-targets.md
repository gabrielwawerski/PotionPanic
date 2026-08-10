---
title: 'Whole-Column Drag Targets For Board Columns'
date: 2026-06-29
status: archived
archivedAt: 2026-08-08
originalPath: 'Docs/archive/completed/vitepress-whole-column-drag-targets.md'
---

# Whole-Column Drag Targets For Board Columns

## Summary

- Replace the current narrow drop-strip interaction with column-body hit testing
  in `BoardColumn.vue` and `useDragDrop.ts`.
- Recommended behavior: the full scrollable card area of a column accepts
  dragover, while insertion still follows the cursor’s vertical position
  relative to the cards.
- Keep the current insertion line as precise feedback, and also keep the
  existing column highlight so the active target is obvious across the full
  column.

## Key Changes

- In the column component, move the primary `dragover` / `drop` handling from
  only the thin slot elements to the full `.board-column-cards` container.
- Compute the target insertion index from cursor position instead of only from
  explicit slot hover.
    - Empty column: always target index `0`.
    - Non-empty column: compare the cursor Y position against each rendered
      card’s midpoint and choose the first card whose midpoint is below the
      cursor; if none match, target `tickets.length`.
- Keep the existing top/between/bottom insertion marker rendering, but drive
  `activeDropIndex` from the computed whole-column target index.
- Keep the per-card and final slot elements only if they still help the marker
  render cleanly; they should no longer be the only hit targets.
- In the drag composable, add a helper that accepts either a direct index or a
  container/card geometry payload and resolves that to a stable target index.
- Preserve current reorder semantics in `board-ordering.mjs`: same-column
  downward moves still adjust by one after removal, cross-column moves still
  renumber source and target columns.

## Test Plan

- Add a unit-style UI regression for `BoardColumn.vue` that verifies
  whole-column drag handling is attached to the column body, not just the “Drop
  here” slots.
- Add drag-drop logic tests for:
    - empty column resolves to index `0`
    - cursor above the first card resolves to index `0`
    - cursor between two cards resolves to the index between them
    - cursor below the last card resolves to `tickets.length`
- Re-run the existing reorder tests to confirm the new target-index calculation
  does not change ordering semantics.
- Manual verification in `npm run docs:dev`:
    - drag within a populated column without aiming for the thin gaps
    - drag across columns and drop in upper, middle, and lower parts of the
      target column
    - drag into an empty column
    - confirm the column highlight and insertion line both update continuously
      while hovering

## Assumptions

- Scope is desktop HTML5 drag-and-drop only; no new touch drag behavior is
  added.
- Whole-column drop means nearest insertion position by cursor Y, not
  append-only behavior.
- Visual feedback keeps both signals: full-column target highlight plus precise
  insertion line.
