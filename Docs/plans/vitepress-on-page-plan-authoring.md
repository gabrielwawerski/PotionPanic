# VitePress On-Page Plan Authoring

## Summary

- Add dev-only plan authoring for active plan docs so plans can be created from `Docs/plans/index.md` and edited from individual `Docs/plans/*.md` pages.
- Use a raw markdown body editor with a separate title field, not a ticket-style structured section editor.
- Keep static builds and archived plans read-only.
- Auto-maintain the `## Active Plans` list in `Docs/plans/index.md` and keep the existing archive flow.

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
  - `Save` / `Cancel`
- New plans should start from a fixed template body:
  - `## Summary`
  - `## Key Changes`
  - `## Public Interface Changes`
  - `## Test Plan`
  - `## Assumptions`
- Add a dedicated composable such as `Docs/.vitepress/theme/composables/usePlanWriter.ts` for plan load/create/update requests. Keep `usePlanArchive.ts` separate unless duplication becomes trivial to merge.

### Writer and Persistence

- Add a new helper module under `Docs/.vitepress/lib/`, preferably `plan-writer.mjs`, to own:
  - plan path validation under `Docs/plans/`
  - title/body extraction and serialization via `gray-matter`
  - stable slug generation from title
  - collision handling with `-2`, `-3`, etc.
  - active plans index maintenance
- Extend `Docs/.vitepress/lib/markdown-writer-plugin.mjs` with dev-only endpoints:
  - `GET /__vitepress_pm_plan?url=...`
  - `POST /__vitepress_pm_create_plan`
  - `POST /__vitepress_pm_update_plan`
- Endpoint behavior:
  - `GET` returns `{ title, body, url, filePath }` for active plan pages only.
  - `create` writes `Docs/plans/<title-slug>.md`, returns the new URL, and updates `Docs/plans/index.md`.
  - `update` preserves any existing frontmatter, rewrites the first H1 from `title`, rewrites the markdown body, keeps the slug stable, and updates the matching `Docs/plans/index.md` label if the title changed.
- Treat the first `# ...` heading as the canonical plan title. If an older file lacks one, inject it on save.
- Extend `Docs/.vitepress/lib/plan-archive.mjs` so archiving also removes that plan’s bullet from `Docs/plans/index.md` while continuing to add it to `Docs/archive/completed/index.md`.

### Index Rules

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
  - `Docs/plans/index.md` add/update/remove behavior
- Extend archive tests to verify plan archive now also removes the active index entry.
- Add UI/source tests for:
  - `Layout.vue` rendering the authoring control on `/plans/` and active plan pages
  - modal create/edit flows wiring to the new endpoints
  - read-only/static mode hiding mutating controls
- Manual verification:
  - create a plan from `/plans/`
  - confirm file creation, redirect, sidebar presence, and active index entry
  - edit title/body on the new page and confirm the page refreshes correctly in `docs:dev`
  - archive the plan and confirm redirect plus active/archive index updates
  - run `npm test` and `npm run docs:build`

## Assumptions

- V1 scope is active plans only: create from `Docs/plans/index.md`, edit on `Docs/plans/*.md`.
- Archived plans remain read-only and keep the current one-way archive flow.
- Raw markdown body editing is intentional; no structured section editor or live preview is included.
- The current plan index page remains authoritative and should not drift from actual active plan files.
