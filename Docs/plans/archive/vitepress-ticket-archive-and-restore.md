---
title: 'VitePress Ticket Archive And Restore'
date: 2026-06-29
status: archived
archivedAt: 2026-08-08
originalPath: 'Docs/archive/completed/vitepress-ticket-archive-and-restore.md'
---

# VitePress Ticket Archive And Restore

## Summary

Replace the missing ticket delete flow with a reversible archive workflow.

Archived tickets will move out of the active ticket directory into a dedicated
archive ticket directory, stay readable through a separate archive board page,
and support restore back into the active board. No hard-delete path will be
added.

## Key Changes

### Board Behavior And Docs Structure

- Keep the active board at [Docs/board.md](../../board.md).
- Add a dedicated archive board page at [Docs/archive/board.md](../../archive/board.md).
- Store active tickets in `Docs/tickets/` and archived tickets in
  `Docs/archive/tickets/`.
- Link the new archive board from [Docs/archive/index.md](../index.md) and the VitePress
  sidebar so it is discoverable from the existing Archive section.
- Treat the archive board as read-only except for a restore action.

### Board Configuration And Runtime Interfaces

- Extend board frontmatter handling with:
  - `boardMode?: "active" | "archive"` with default `"active"`.
  - `archiveTicketsDir?: string` on active boards.
  - `restoreTicketsDir?: string` on archive boards.
- Extend `Ticket` in `Docs/.vitepress/theme/types.ts` with optional
  `archivedAt?: string`.
- Extend dependency suggestion data so each dependency option can carry its
  canonical URL and optional archived state.
  - Recommended shape: `dependencies: Array<{ label: string; value: string; url?: string; archived?: boolean }>`

### Theme And UI Flow

- In `Docs/.vitepress/theme/components/Board.vue`, add mode-aware actions:
  - Active board: show `Archive` for persisted tickets.
  - Archive board: show `Restore`.
  - Static build: keep all mutating actions hidden, same as current read-only
    behavior.
- In `Docs/.vitepress/theme/components/TicketDetail.vue`, add a danger-zone
  action area:
  - `Archive` button on active persisted tickets.
  - `Restore` button on archived tickets.
  - Both actions require explicit confirmation before proceeding.
- Keep archived tickets viewable in the same modal layout, but disable
  create/edit controls on the archive board.
- Do not add a `Delete` button or any irreversible UI action.

### Writer/Plugin Behavior

- In `Docs/.vitepress/lib/markdown-writer-plugin.mjs`, add move-based archive
  operations rather than delete operations.
- Add two dev-server endpoints:
  - `POST /__vitepress_pm_archive`
  - `POST /__vitepress_pm_restore`
- Archive operation:
  - Resolve the current markdown file from its ticket URL.
  - Move it from the active tickets dir to the archive tickets dir.
  - Preserve slug, ID, title, body, status, priority, tags, assignee,
    milestone, dependencies, documentation, and affected files.
  - Add or update `archivedAt` with an ISO timestamp.
- Restore operation:
  - Move the same file back to the active tickets dir.
  - Preserve existing `status` so restore returns the ticket to its previous
    column.
  - Remove `archivedAt`.
- Do not renumber or rename tickets during archive or restore.
- Keep create/update behavior unchanged outside of the new archive/restore
  flows.

### Link Resolution

- Update dependency/reference resolution so `PP-x` links resolve:
  1. to the active ticket if it exists there,
  2. otherwise to the archived ticket URL.
- Generate that resolution from the scanned ticket catalogs instead of assuming
  `/tickets/<id>.html`.
- Keep archived tickets out of active dependency suggestions if desired for UX,
  but do not break existing archived references.

## Test Plan

- Add unit coverage in `Scripts/docs/lib/markdown-writer-plugin.test.mjs` for:
  - archiving a ticket moves the file to the archive ticket directory,
  - archiving preserves slug, ID, status, metadata, and body,
  - archiving writes `archivedAt`,
  - restoring moves the file back to the active ticket directory,
  - restoring clears `archivedAt`,
  - restore preserves prior status and slug.
- Add lib/runtime coverage for dependency resolution:
  - active ticket URL wins when both active and archived candidates are
    possible,
  - archived ticket URL is returned when only the archived copy exists.
- Run existing docs-related tests plus a manual smoke check in local dev:
  - open active board, archive a ticket, verify it disappears from active
    board,
  - open archive board, verify the ticket appears there in its prior status
    column,
  - restore it, verify it returns to the active board,
  - confirm no archive/restore controls appear in the static read-only build.

## Assumptions

- No permanent delete endpoint, button, or CLI flow is included in this slice.
- Archive/restore is a board-only feature available through the local
  VitePress dev server, not the static site.
- Archived tickets use the same column schema as active tickets; their
  existing `status` is preserved instead of being rewritten.
- Existing `Docs/archive/completed/` pages stay untouched; the new archive
  ticket board is additive and does not migrate old archive content.
- No archive reason picker, no actor tracking, and no bulk archive/restore
  flow are included in this first implementation.
