# Archived Plan Restore

## Summary

- Add a dev-only restore path for archived plans so a page under `Docs/archive/completed/` can be moved back into `Docs/plans/`.
- Keep the UX parallel to the current archive flow: restore is available on the archived plan page itself, not from the archive index.
- Preserve the existing filename, title, body, and `date:` frontmatter when restoring.
- Keep restore strict: if the target active filename already exists, fail with a clear error instead of renaming or overwriting.
- When restored, remove the archived index bullet and append the plan back into `Docs/plans/index.md`.

## Key Changes

### Archive/Restore Helpers

- Extend `Docs/.vitepress/lib/plan-archive-page.mjs` with archived-plan page detection alongside the existing active-plan detection.
  - Keep `plans/index.md` and `archive/completed/index.md` excluded from archive/restore actions.
  - Add a helper to classify plan pages as `active`, `archived`, or neither.
- Extend `Docs/.vitepress/lib/plan-archive.mjs` from one-way archive-only logic into a paired archive/restore helper set.
  - Add `restorePlanFile(docsDir, {url})`.
  - Accept only archived non-index plan pages as restore sources.
  - Move the markdown file from `Docs/archive/completed/` back to `Docs/plans/`.
  - Preserve basename and content exactly; do not rewrite `date:` or rename the file.
  - On restore, remove the matching bullet from `Docs/archive/completed/index.md`.
  - Reuse the existing active plans index maintenance helper so the restored plan is appended back into `## Active Plans`.
  - If `Docs/plans/<same-file>.md` already exists, throw a conflict error and do not modify either file or either index.

### Dev Endpoints and Client Composable

- Extend `Docs/.vitepress/lib/markdown-writer-plugin.mjs` with a new dev-only endpoint:
  - `POST /__vitepress_pm_restore_plan`
- Keep the existing `POST /__vitepress_pm_archive_plan` unchanged except for shared helper usage if needed.
- Extend `Docs/.vitepress/theme/composables/usePlanArchive.ts` into a two-action composable or add a sibling restore composable.
  - Support both `archivePlan(url)` and `restorePlan(url)`.
  - Return the same minimal result shape needed for redirect: `{ title, url }`.
  - Surface non-OK responses as user-visible error text in the existing callout area.

### Page UI and Action Surface

- Update `Docs/.vitepress/theme/components/PlanAuthoringControls.vue` so the callout becomes mode-aware:
  - On active plan pages: keep `Edit Plan` and `Archive Plan`.
  - On archived plan pages: show `Restore Plan`.
  - On `Docs/plans/index.md`: keep `New Plan`.
- Archived plan restore stays dev-only, same as archive/create/edit.
- Restore confirmation copy should explicitly say the plan will move back into Active Plans.
- After restore, redirect the browser to the restored active plan URL under `/plans/...`.
- Do not add restore controls to `Docs/archive/completed/index.md` in v1.

### Index and Sidebar Behavior

- On archive:
  - keep current behavior of removing the active index entry and adding the archived index entry.
- On restore:
  - remove the archived index entry from `Docs/archive/completed/index.md`
  - append the restored plan entry to `Docs/plans/index.md`
- Sidebar behavior should continue to follow filesystem location plus existing plan sorting:
  - archived plans appear under Archive because they live in `archive/completed`
  - restored plans reappear under Plans because they move back into `plans`
- No additional sidebar ordering rule changes are needed beyond the existing `date:` sort for active plans.

## Public Interfaces

- New internal dev endpoint:
  - `POST /__vitepress_pm_restore_plan`
- New server helper:
  - `restorePlanFile(docsDir, {url})`
- New page classification behavior:
  - archived non-index plan pages are restore-eligible
  - active non-index plan pages are archive-eligible
- No changes to the current filename policy, `date:` policy, or static-build read-only behavior.

## Test Plan

- Extend `Scripts/docs/lib/plan-archive.test.mjs` to cover:
  - archived plan pages are recognized as restore-eligible
  - restore moves a plan from `archive/completed/` back to `plans/`
  - restore removes the archived index bullet
  - restore appends the active index bullet
  - restore preserves basename, content, title, and `date:`
  - restore rejects archive index pages
  - restore rejects filename conflicts in `Docs/plans/`
- Extend `Scripts/docs/lib/markdown-writer-plugin.test.mjs` to verify `restorePlanFile` is exported and wired for the restore endpoint.
- Extend `Scripts/docs/lib/archive-plan-ui.test.mjs` to verify:
  - archived plan pages expose `Restore Plan`
  - active plan pages still expose `Archive Plan`
  - the plan archive composable posts to both archive and restore endpoints
- Manual verification in `docs:dev`:
  - archive an active plan and confirm it lands in `/archive/completed/...`
  - open that archived page and restore it
  - confirm redirect to `/plans/...`
  - confirm the plan disappears from `Docs/archive/completed/index.md`
  - confirm it reappears in `Docs/plans/index.md`
  - confirm no restore control appears in the static build

## Assumptions

- V1 restore is only available from an archived plan page, not from the archive index.
- Restore is dev-only and uses the local docs server, same as the rest of the plan authoring workflow.
- Restored plans keep their original filename and `date:`; there is no “restoredAt” metadata in this slice.
- On conflict with an existing active file of the same name, restore fails and leaves all files unchanged.
- Appending restored plans to `Docs/plans/index.md` is intentional and matches the current active-index maintenance style.
