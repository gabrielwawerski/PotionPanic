---
date: 2026-06-28
---

# VitePress Theme Review - 2026-06-28

## Summary

Reviewed the recent VitePress and theme commits, with emphasis on the board
sidebar restoration work and recent modal/layout changes.

Board sidebar restoration work should be deferred for now. There is still a
board-only alignment mismatch, so that area should not be treated as complete.

## Findings

### 1. Board sidebar is still slightly misaligned on the board page

The board page does not fully match the normal VitePress left sidebar shell.
On `/board` at large desktop width, the navbar title box and divider render at
`272px` while the actual sidebar renders at `262px`. Normal docs pages do not
show that mismatch.

Likely source:

- `Docs/.vitepress/theme/styles/board.css`
- recent rule: `.board-shell-layout .VPNavBar.has-sidebar .title { width:
  var(--vp-sidebar-width); }`

Decision:

- Defer sidebar work for now.

### 2. Ticket modal regressed on narrow/mobile widths

The recent modal sizing changes made the metadata sidebar too dominant on small
viewports. The modal still fits within the viewport, but the left content pane
shrinks to the point where the ticket body becomes hard to use.

Observed examples:

- at `640px` viewport width, the left pane dropped to about `218px`
- at `480px` viewport width, the left pane dropped to about `58px`

Primary source areas:

- `Docs/.vitepress/theme/components/TicketDetail.vue`

Follow-up task:

- Mobile view for the modal should have its metadata sidebar collapsible and/or
  collapsed by default so the main ticket content keeps usable width.

### 3. "Likely Affected Files" suggestions omit `.vitepress` source files

The affected-file suggestion catalog currently skips directories whose names
start with `.`. That drops all `Docs/.vitepress/...` files from suggestions,
including the main theme files that are actively being edited.

Examples missing from suggestions:

- `Docs/.vitepress/theme/Layout.vue`
- `Docs/.vitepress/theme/styles/board.css`

Primary source area:

- `Docs/.vitepress/lib/ticket-suggestions.mjs`

### 4. Read-only board guidance still references a removed script

The published read-only board still tells users to run
`.\Scripts\docs-ui.ps1`, but that script no longer exists.

Primary source area:

- `Docs/.vitepress/theme/components/Board.vue`

## Verified Good

- Sidebar collapse/expand behavior on the board works and persists through
  `localStorage`.
- Non-board docs pages do not get the board toggle.
- Node tests passed: `npm test`
- Docs build passed: `npm run docs:build`
- Runtime console was clean apart from a missing `favicon.ico` 404.

## Scope Note

This note records review findings only. It does not apply fixes.
