# Archived Plan Restore

## Summary

- Add a dev-only restore path in the reusable `C:\Dev\Docboard` package so an
  archived plan page can be moved back into the active plans directory.
- Keep the UX parallel to the current archive flow: restore is available on the
  archived plan page itself, not from the archive index.
- Preserve the existing filename and raw markdown content, including title,
  body, and `date:` frontmatter.
- Keep restore strict: if the target active filename already exists, fail with a
  clear error and leave source, target, and indexes unchanged.
- Honor host-configured plan paths through the existing `plans` options instead
  of hardcoding only `plans/` and `archive/completed/`.

## Key Changes

### Archive/Restore Helpers

- Extend `C:\Dev\Docboard\src\node\plan-archive-page.mjs` with archived-plan
  page detection alongside the existing active-plan detection.
    - Keep active and archive index pages excluded from archive/restore actions.
    - Add `isRestorablePlanPage(relativePath, plans)` or a small equivalent
      classifier for `active`, `archived`, or neither.
- Extend `C:\Dev\Docboard\src\theme\plan-pages.ts` with the same restore
  eligibility behavior for client-side controls.
- Extend `C:\Dev\Docboard\src\node\plan-archive.mjs` from one-way archive-only
  logic into a paired archive/restore helper set.
    - Add `restorePlanFile(docsDir, {plans, url})`.
    - Accept only archived non-index plan pages as restore sources.
    - Move the markdown file from `paths.archiveDir` back to `paths.activeDir`.
    - Preserve basename and raw file content exactly; do not rewrite
      frontmatter.
    - Remove the matching archived index bullet after a successful move.
    - Rebuild the active plans index through the existing active-index sync
      helper so current date/sidebar sorting rules stay unchanged.
    - If the active target file already exists, throw a conflict error before
      any file or index mutation.
- Export `restorePlanFile` through `C:\Dev\Docboard\src\node\index.mjs` and the
  existing plan-archive module exports.

### Dev Endpoint and Client Composable

- Extend `C:\Dev\Docboard\src\node\markdown-writer-plugin.mjs` with a new
  dev-server endpoint:
    - `POST /__vitepress_pm_restore_plan`
- Keep `POST /__vitepress_pm_archive_plan` behavior unchanged except for shared
  helper reuse if needed.
- Send the existing sidebar HMR update after a successful restore, same as
  archive/create/update plan flows.
- Extend `C:\Dev\Docboard\src\theme\composables\usePlanArchive.ts` to return
  both `archivePlan(url)` and `restorePlan(url)`.
    - Post restore requests to `/__vitepress_pm_restore_plan`.
    - Return the same redirect result shape as archive: `{ title, url }`.
    - Surface non-OK responses through the existing `error` ref.

### Page UI and Action Surface

- Update `C:\Dev\Docboard\src\theme\components\PlanAuthoringControls.vue` so the
  callout is mode-aware:
    - Active plan pages: keep `Edit Plan` and `Archive Plan`.
    - Archived plan pages: show `Restore Plan`.
    - Active plans index page: keep `New Plan`.
- Derive all page eligibility from `page.value.relativePath` plus
  `themeOptions.plans`.
- Restore confirmation copy should explicitly say the plan will move back into
  Active Plans.
- After restore, redirect to the restored active plan URL under the configured
  active plan path.
- Do not add restore controls to the archived plans index in v1.

### Index and Sidebar Behavior

- On archive, keep current behavior: remove the active index entry and add the
  archived index entry.
- On restore:
    - remove the archived index entry from `paths.archiveIndex`
    - rebuild `paths.activeIndex` using the existing active plan index sync
- Sidebar behavior should continue to follow filesystem location plus existing
  sorting:
    - archived plans appear under Archive because they live in
      `paths.archiveDir`
    - restored plans reappear under Plans because they move back into
      `paths.activeDir`
- No sidebar ordering rule changes are needed.

## Public Interfaces

- New internal dev endpoint:
    - `POST /__vitepress_pm_restore_plan`
- New server helper:
    - `restorePlanFile(docsDir, {plans, url})`
- New page eligibility helper:
    - archived non-index plan pages are restore-eligible
    - active non-index plan pages remain archive-eligible
- No changes to filename policy, `date:` policy, static-build read-only
  behavior, or ticket archive/restore behavior.

## Test Plan

- Extend `C:\Dev\Docboard\tests\node\plan-workflow.test.mjs` to cover:
    - archived plan pages are recognized as restore-eligible
    - restore moves a plan from the configured archive dir back to the
      configured active dir
    - restore removes the archived index bullet
    - restore rebuilds the active index entry
    - restore preserves basename and raw markdown content
    - restore rejects archive index pages
    - restore rejects active filename conflicts without modifying files or
      indexes
    - custom `plans.activeDir`, `plans.activeIndex`, `plans.archiveDir`, and
      `plans.archiveIndex` paths work
- Extend `C:\Dev\Docboard\tests\node\sidebar-workflow.test.mjs` to verify the
  restore endpoint is wired and sends the same sidebar update event shape as the
  archive endpoint.
- Extend `C:\Dev\Docboard\tests\shared\plan-navigation.test.mjs` to verify:
    - restore redirects preserve `import.meta.env.BASE_URL`
    - archived plan pages expose restore eligibility
    - active plan pages still expose archive eligibility
    - the plan archive composable includes both archive and restore endpoint
      calls
- Run:
    - `npm test` in `C:\Dev\Docboard`
    - `npm run test:types` in `C:\Dev\Docboard`
    - `npm run docs:build` in `C:\Dev\PotionPanic`
- Manual verification in `C:\Dev\PotionPanic`:
    - start `npm run docs:dev:local`
    - archive an active plan and confirm it lands under the configured archive
      URL
    - open that archived page and restore it
    - confirm redirect to the configured active plan URL
    - confirm it disappears from the archive index and reappears in the active
      plans index/sidebar
    - confirm no restore control appears in a static build

## Assumptions

- V1 restore is only available from an archived plan page, not from the archive
  index.
- Restore is dev-only and uses the local docs server, same as the rest of the
  plan authoring workflow.
- Restored plans keep their original filename and `date:`; there is no
  `restoredAt` metadata in this slice.
- Conflict handling is all-or-nothing: if the active target exists, no file or
  index is changed.
- The active index should be rebuilt by the existing sync helper, not manually
  appended, so current sorting behavior remains the source of truth.
