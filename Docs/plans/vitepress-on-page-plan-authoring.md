---
date: 2026-06-29
---

# VitePress On-Page Plan Authoring

## Summary

- Add dev-only plan authoring for active plan docs so plans can be created from `Docs/plans/index.md` and edited from individual `Docs/plans/*.md` pages.
- Use a raw markdown body editor with a separate title field, not a ticket-style structured section editor.
- Keep static builds and archived plans read-only.
- Auto-maintain the `## Active Plans` list in `Docs/plans/index.md` and keep the existing archive flow.
- Sort the Plans sidebar section by plan `date:` ascending, with `plans/index.md` always first, so newer plans appear later.

## Implementation Changes

### Theme and UI

- Replace the footer-only plan action with a single plan authoring control mounted from `Docs/.vitepress/theme/Layout.vue`.
- Add a shared plan authoring component pair under `Docs/.vitepress/theme/components/`:
  - `PlanAuthoringControls.vue` decides what to show based on `page.relativePath`.
  - `PlanEditorModal.vue` handles create/edit UI.
- Behavior:
  - On `/plans/`, show `New Plan`.
  - On active `/plans/<slug>` pages, show `Edit Plan` and the existing `Archive Plan`.
  - On archived pages and non-plan pages, show nothing new.
- The modal should expose:
  - `Title`
  - read-only filename/URL preview
  - raw markdown `Body`
  - read-only or hidden `Date` field in v1, auto-set by the system on create
  - `Save` / `Cancel`
- New plans should start from a fixed template body:
  - `## Summary`
  - `## Key Changes`
  - `## Public Interface Changes`
  - `## Test Plan`
  - `## Assumptions`
- Add a dedicated composable such as `Docs/.vitepress/theme/composables/usePlanWriter.ts` for plan load/create/update requests. Keep `usePlanArchive.ts` separate unless duplication becomes trivial to merge.

### Writer, Metadata, and Persistence

- Add a new helper module under `Docs/.vitepress/lib/`, preferably `plan-writer.mjs`, to own:
  - plan path validation under `Docs/plans/`
  - title/body extraction and serialization via `gray-matter`
  - stable slug generation from title
  - collision handling with `-2`, `-3`, etc.
  - active plans index maintenance
  - canonical plan date normalization
- Extend `Docs/.vitepress/lib/markdown-writer-plugin.mjs` with dev-only endpoints:
  - `GET /__vitepress_pm_plan?url=...`
  - `POST /__vitepress_pm_create_plan`
  - `POST /__vitepress_pm_update_plan`
- Endpoint behavior:
  - `GET` returns `{ title, body, url, filePath, date }` for active plan pages only.
  - `create` writes `Docs/plans/<title-slug>.md`, sets `date: YYYY-MM-DD`, returns the new URL, and updates `Docs/plans/index.md`.
  - `update` preserves the existing `date`, preserves any unrelated frontmatter, rewrites the first H1 from `title`, rewrites the markdown body, keeps the slug stable, and updates the matching `Docs/plans/index.md` label if the title changed.
- Treat the first `# ...` heading as the canonical plan title. If an older file lacks one, inject it on save.
- Treat `date:` frontmatter as the canonical sort key for plan pages.
- For existing plan files that lack `date:`, add a one-time backfill as part of this slice:
  - use an explicit `YYYY-MM-DD` value in frontmatter
  - infer from an existing filename date when available
  - otherwise set the date intentionally during migration rather than relying on filesystem timestamps
- Extend `Docs/.vitepress/lib/plan-archive.mjs` so archiving also removes that plan’s bullet from `Docs/plans/index.md` while continuing to add it to `Docs/archive/completed/index.md`.

### Sidebar Ordering and Index Rules

- Update `Docs/.vitepress/lib/sidebar.mjs` so the Plans section sorts auto-discovered plan pages by:
  1. `index.md` first
  2. then `date:` ascending
  3. then link/text as a stable tiebreaker
- Keep non-Plan sections on their current behavior unless explicitly changed later.
- Auto-maintain only the `## Active Plans` section in `Docs/plans/index.md`.
- Preserve all other prose in `Docs/plans/index.md`.
- Creation appends a new bullet if missing.
- Edit updates the existing bullet text for the same plan URL.
- Archive removes the bullet from `## Active Plans`.
- Do not rename existing plan files automatically.

## Public Interfaces

- New internal dev endpoints:
  - `GET /__vitepress_pm_plan`
  - `POST /__vitepress_pm_create_plan`
  - `POST /__vitepress_pm_update_plan`
- New plan metadata contract:
  - active plan pages use `date: YYYY-MM-DD` frontmatter as the canonical sort date
  - on-page-created plans write that field automatically
  - manually added plans should include it to participate in deterministic sidebar ordering
- New plan creation rule:
  - filenames are auto-generated from title slugs under `Docs/plans/`
  - the slug stays fixed after creation
- No ticket schema, board frontmatter, or archived-plan restore behavior changes in this slice.

## Test Plan

- Add unit coverage for the new plan helper module:
  - title/body extraction with and without frontmatter
  - H1 injection for legacy files
  - slug generation and collision suffixes
  - create/update writes
  - `date:` write-on-create and preserve-on-update behavior
  - `Docs/plans/index.md` add/update/remove behavior
- Extend sidebar tests to verify the Plans section sorts:
  - `plans/index.md` first
  - then dated plans oldest to newest
  - tie-breaking remains stable for equal dates
  - non-Plan sections keep current ordering
- Add a sidebar HMR test proving that manually adding a dated plan file under `Docs/plans/` updates the sidebar order live during `docs:dev`.
- Extend archive tests to verify plan archive now also removes the active index entry.
- Add UI/source tests for:
  - `Layout.vue` rendering the authoring control on `/plans/` and active plan pages
  - modal create/edit flows wiring to the new endpoints
  - read-only/static mode hiding mutating controls
- Manual verification:
  - create a plan from `/plans/`
  - confirm file creation, `date:` frontmatter, redirect, sidebar position, and active index entry
  - manually add a dated plan file under `Docs/plans/` and confirm the sidebar reorders with `index.md` still first
  - edit title/body on an existing plan and confirm the date stays unchanged
  - archive the plan and confirm redirect plus active/archive index updates
  - run `npm test` and `npm run docs:build`

## Assumptions

- V1 scope is active plans only: create from `Docs/plans/index.md`, edit on `Docs/plans/*.md`.
- Archived plans remain read-only and keep the current one-way archive flow.
- Raw markdown body editing is intentional; no structured section editor or live preview is included.
- The current plan index page remains authoritative and should not drift from actual active plan files.
- `date:` frontmatter is the single source of truth for plan ordering; filesystem modified time is out of scope.
