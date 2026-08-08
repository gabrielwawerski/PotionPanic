---
title: 'VitePress Ticket Filename With Title Plan'
date: 2026-06-29
status: archived
archivedAt: 2026-08-08
originalPath: 'Docs/archive/completed/vitepress-ticket-filename-with-title-plan.md'
---

# VitePress Ticket Filename With Title Plan

## Summary
- Change new ticket filenames from ID-only (`PP-7.md`) to ID plus title slug (`PP-7-add-assignee-support-to-the-vitepress-board.md`).
- Keep all existing active and archived ticket filenames unchanged.
- Keep the filename stable after creation; editing the ticket title updates frontmatter only and does not rename the file or URL.

## Key Changes
- Add a single slug helper in the markdown writer flow to build filenames as `<display-id>-<title-slug>`.
  - `display-id` stays in the current format from `ticketPrefix` + numeric `id`.
  - `title-slug` uses lowercase kebab-case, strips non-alphanumeric separators, collapses repeats, and falls back to `ticket` if the title slug would be empty.
- Update ticket creation in `Docs/.vitepress/lib/markdown-writer-plugin.mjs` so `createTicketFile()` writes the new filename shape and returns the matching `url`.
- Relax ticket identity validation in the same module.
  - Treat both `PP-7` and `PP-7-any-suffix` as valid for ticket `id: 7`.
  - Do not require the suffix to match the current title, because title edits must not trigger validation noise.
  - Continue flagging missing IDs, duplicate IDs, and filenames whose leading ID token does not match the frontmatter ID.
- Update identity repair behavior.
  - Preserve legacy files with no suffix when they are otherwise valid.
  - When a repair must rename a malformed file, generate a valid filename using the fixed display ID plus either the existing suffix when one is already present or a slugified current title when there is no reusable suffix.
- Update link resolution in `Docs/.vitepress/lib/ticket-links.mjs`.
  - Keep dependency values as ticket IDs like `PP-2`.
  - Prefer scanned ticket URL maps for resolving those IDs to the real titled filename.
  - Stop guessing `/tickets/<ID>.html` for unresolved ID-only references; return no link instead of a likely broken one.
  - Keep explicit slug/path references working if a full ticket path is already supplied.
- Keep archive/restore behavior basename-preserving, so titled filenames move between `tickets` and `archive/tickets` unchanged.
- Update CLI and any user-facing examples that assume ID-only filenames, mainly `Scripts/docs/create-ticket.mjs` output expectations and ticket-related docs/tests.

## Interfaces And Types
- No new board frontmatter fields are needed.
- Keep the `Ticket` runtime shape unchanged unless implementation needs an internal-only helper field; the public board payload can continue exposing `id`, `title`, and `url`.
- Keep dependency metadata format unchanged: stored values remain ticket IDs, suggestion labels remain `PP-2 - Title`, and suggestion URLs point to the actual titled page.

## Test Plan
- Add or update unit tests for:
  - ticket creation writes `PP-<id>-<title-slug>.md` and returns `/tickets/PP-<id>-<title-slug>.html`
  - validation accepts legacy `PP-7.md`
  - validation accepts titled `PP-7-some-title.md`
  - validation does not fail after a ticket title changes but the filename stays on the original suffix
  - validation still flags malformed leading IDs and duplicate/missing IDs
  - fix flow preserves legacy no-suffix files when valid and repairs malformed files into valid titled filenames
  - archive/restore preserves titled basenames
  - ticket link resolution uses provided URL maps for ID-only dependencies and returns `null` when an ID cannot be resolved
- Run the existing Node test files covering:
  - `Scripts/docs/lib/markdown-writer-plugin.test.mjs`
  - `Scripts/docs/lib/ticket-links.test.mjs`
  - any suggestion/sidebar tests that assert hard-coded `/tickets/PP-<id>.html`
- Manual verification in `npm run docs:dev`:
  - create a new ticket from the board and confirm the file is created with the titled filename
  - edit that ticket’s title and confirm the file path shown in the modal stays unchanged
  - confirm dependency chips still navigate correctly for existing and newly created tickets
  - archive and restore a titled ticket and confirm the same basename is preserved

## Assumptions
- Scope is forward-only: existing tickets are not mass-renamed.
- Filename/title coupling is creation-time only; URL stability is preferred over keeping the suffix synchronized with later title edits.
- Dependency references remain ID-based, not filename-based.
