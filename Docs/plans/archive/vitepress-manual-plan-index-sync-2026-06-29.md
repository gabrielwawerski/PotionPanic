---
title: 'VitePress Manual Plan Index Sync'
date: 2026-06-29
status: archived
archivedAt: 2026-08-08
originalPath: 'Docs/archive/completed/vitepress-manual-plan-index-sync-2026-06-29.md'
---

# VitePress Manual Plan Index Sync

## Summary

- Keep the existing behavior where `Docs/plans/*.md` auto-refreshes the sidebar during `npm run docs:dev`.
- Add matching auto-maintenance for [Docs/plans/index.md](../../plans/index.md) so manual plan file adds, deletes, renames, and title changes update the `## Active Plans`
  section without hand-editing the index page.
- Limit source-file mutation to dev-time authoring workflows; static builds should stay read-only.
- Preserve all non-generated prose in [Docs/plans/index.md](../../plans/index.md).

## Key Changes

### Shared plan index sync

- Extend `Docs/.vitepress/lib/plan-writer.mjs` with a filesystem-driven helper such as `syncActivePlansIndex(docsDir)` that:
    - scans `Docs/plans/*.md` except `index.md`
    - reads each plan title from frontmatter `title`, then first `#` heading, then filename fallback
    - reads optional `date:` frontmatter for deterministic ordering
    - rewrites only the `## Active Plans` section in [Docs/plans/index.md](../../plans/index.md)
    - restores the `_No active plans yet._` placeholder when no active plans remain
- Reuse the same plan ordering rules already used by the sidebar so the Plans page and the Plans sidebar do not drift:
    - `index.md` is never listed as an active plan
    - dated plans sort oldest to newest
    - undated plans sort after dated plans
    - ties fall back to label and link for stable output

### Dev-time watcher behavior

- Add a small dev-only watcher path, preferably alongside the existing sidebar HMR flow, for active plan pages under `Docs/plans/`.
- On `add`, `unlink`, or content changes to non-index active plan files:
    - run `syncActivePlansIndex(docsDir)` before sending sidebar updates
    - let the normal VitePress markdown watcher refresh [Docs/plans/index.md](../../plans/index.md)
      after the sync rewrites it
- Ignore:
    - [Docs/plans/index.md](../../plans/index.md) itself to avoid self-trigger loops
    - archived plan paths
    - non-plan docs pages
- Keep static `vitepress build` behavior non-mutating. If index repair is needed outside `docs:dev`, rely on explicit tooling rather than build-time writes in this slice.

### Existing plan writer alignment

- Keep `createPlanFile`, `updatePlanFile`, and archive flows working, but make them call the same full-section sync helper instead of manually upserting or removing a single bullet.
- This avoids divergent logic between:
    - on-page plan authoring
    - archive moves
    - manual filesystem edits during docs development
- Preserve current URL and slug behavior. No automatic plan file renames are introduced.

## Interfaces And Internal Contracts

- New internal helper:
    - `syncActivePlansIndex(docsDir)`
- Existing internal plan helpers should continue to own:
    - title/body parsing
    - active-plan path validation
    - archive integration
- No nav changes, ticket schema changes, or public board API changes are part of this slice.

## Test Plan

- Extend `Scripts/docs/lib/plan-writer.test.mjs` to cover:
    - full-section rebuild from multiple plan files
    - title resolution from frontmatter, heading, and filename fallback
    - placeholder restoration when there are no active plans
    - preservation of surrounding prose in [Docs/plans/index.md](../../plans/index.md)
- Extend `Scripts/docs/lib/sidebar-hmr.test.mjs` or add a focused watcher test to verify:
    - adding `Docs/plans/new-plan.md` rewrites [Docs/plans/index.md](../../plans/index.md)
    - deleting a plan removes its bullet
    - editing a plan title updates the index label
    - non-plan markdown changes do not rewrite the plans index
- Keep or extend `Scripts/docs/lib/plans-index-links.test.mjs` so the synced index only links to files that exist.
- Re-run:
    - `npm test`
    - `npm run docs:build`
- Manual verification in `npm run docs:dev`:
    - create a plan file directly under `Docs/plans/` and confirm it appears in both the sidebar and [Docs/plans/index.md](../../plans/index.md)
    - rename or delete that file and confirm both locations update
    - change the plan title and confirm the index label updates
    - confirm archived plans still stay out of the active index

## Assumptions

- The existing sidebar auto-refresh behavior is already correct and should not be redesigned.
- Auto-sync is only required while the VitePress dev server is running; this slice does not introduce a background watcher outside docs tooling.
- [Docs/plans/index.md](../../plans/index.md) remains the human-readable landing page, but its
  `## Active Plans` section becomes generated from filesystem truth.
- Plan `date:` frontmatter remains the canonical sort signal when present.
