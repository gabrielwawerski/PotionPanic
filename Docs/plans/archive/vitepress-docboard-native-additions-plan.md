---
title: 'VitePress Native Additions In Docboard'
date: 2026-07-02
status: archived
archivedAt: 2026-08-08
originalPath: 'Docs/archive/completed/vitepress-docboard-native-additions-plan.md'
---

# VitePress Native Additions In Docboard

## Summary

Add the cheap VitePress-native wins to the reusable `@gabrielwawerski/docboard`
package where they are project-agnostic: local search by default and an explicit `lastUpdated` passthrough. Keep repo-specific options such as GitHub edit links and favicon/head metadata in host project config. Skip custom search, Algolia/Ask AI, PWA, analytics, i18n, and new plugins until the docs are large enough to prove the need.

## Key Changes

- In `C:\Dev\Docboard`, default VitePress local search from
  `createDocsConfig()` with `themeConfig.search.provider = "local"` when the host project does not provide its own `themeConfig.search`.
- Add `lastUpdated?: boolean` to the Docboard project config types and pass it through from `createDocsConfig()` to the VitePress top-level `lastUpdated`
  option.
- Keep existing `cleanUrls`; Docboard already defaults it to `true`.
- Keep `themeConfig.editLink` host-owned because each consuming repo has a different GitHub URL, branch, docs root, and edit path.
- Keep favicon/head metadata host-owned because it depends on real project branding assets.
- In PotionPanic, opt into `lastUpdated: true` through the Docboard project config and add `fetch-depth: 0` to the docs deploy checkout so Git timestamps are reliable.

## Public API / Interface Impact

- New Docboard config field: `lastUpdated?: boolean`.
- Existing host `themeConfig.search` overrides the Docboard local-search default.
- No Docboard-specific wrapper for VitePress edit links, favicons, analytics, DocSearch, or PWA behavior.

## Test Plan

- In `C:\Dev\Docboard`, extend config tests to verify default local search, host search override, and `lastUpdated` passthrough.
- In `C:\Dev\Docboard`, run `npm test` and `npm run test:types`.
- In `C:\Dev\PotionPanic`, run `npm run docs:build`.
- Run `npm run docs:dev:local`, open `/PotionPanic/`, and verify Ctrl/Cmd+K finds project docs, plans, and tickets.
- Check one normal doc page for the last-updated footer.
- Check GitHub Pages after deploy to confirm timestamps are per-file, not all identical.

## Assumptions

- Target the installed VitePress `1.6.4`, not the `2.0.0-alpha` docs features.
- Active tickets and archived docs being searchable is acceptable for v1.
- Edit links can be added later in PotionPanic host config if wanted, but they are not a reusable Docboard package default.
