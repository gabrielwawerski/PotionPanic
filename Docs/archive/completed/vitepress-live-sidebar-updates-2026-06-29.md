---
date: 2026-06-29
---

# VitePress Live Sidebar Updates During `docs:dev`

## Summary

- Support true live sidebar updates during `npm run docs:dev` without manual
  server restart.
- Keep the current static build path: `themeConfig.sidebar` is still generated
  from the same shared sidebar builder for production and build output.
- Do not automate `themeConfig.nav`.
- Do not rely on auto page reload only as the primary solution; by itself that
  does not reliably refresh a sidebar derived from static site config.
- Preferred approach: add a dev-only sidebar HMR bridge that recomputes
  sidebar data on relevant Markdown changes and patches VitePress site data in
  the browser.

## Key Changes

- Extract the sidebar section registry and shared builder into a reusable
  sidebar source module under `Docs/.vitepress/lib/` so both config-time
  generation and dev-time refresh use the same rules.
- Keep `Docs/.vitepress/config.ts` as the source of truth for static site
  config, but have it import the shared sidebar definition instead of inlining
  the section map.
- Add a new dev-only Vite plugin, separate from `markdown-writer-plugin.mjs`,
  responsible for sidebar refresh behavior:
  - `configResolved` captures the docs root.
  - `handleHotUpdate` watches `.md` content changes in included sidebar
    folders.
  - `configureServer` also listens for `add` and `unlink` so new, renamed, or
    deleted docs pages refresh the sidebar.
  - On a relevant change, recompute the sidebar and send a custom HMR event
    such as `potion-panic:sidebar-update` with payload shaped like the existing
    `themeConfig.sidebar`.
  - Ignore `Docs/.vitepress/**`, `Docs/tickets/**`, and
    `Docs/archive/completed/**` using the same include and exclude rules as the
    static builder.
- Update `Docs/.vitepress/theme/index.ts` to subscribe to the custom HMR event
  in dev and replace `siteData.value.themeConfig.sidebar` with the refreshed
  value.
  - This keeps the default VitePress sidebar components working as-is, because
    they already react to `theme.value.sidebar`.
  - No custom sidebar UI should be introduced.
- Update docs text in onboarding and workflow docs so it says sidebar changes
  appear live during `docs:dev`, while `nav` remains manual.

## Interfaces And Internal Contracts

- No public docs authoring API change beyond the existing optional
  `sidebar: false` frontmatter behavior.
- No change to the `themeConfig.sidebar` shape consumed by VitePress.
- Add one internal dev-only HMR event:
  - `potion-panic:sidebar-update`
  - Payload: the same multi-sidebar object shape used by `themeConfig.sidebar`,
    currently just the `"/"` entry for this repo.

## Test Plan

- Extend docs-script tests to cover the new pure helpers used by the dev
  plugin:
  - relevant-file detection for included folders
  - exclusion of `.vitepress`, `tickets`, and `archive/completed`
  - add, change, and delete path handling
  - shared sidebar payload generation matching the static builder
- Add a dev-plugin test with a fake server object to verify:
  - relevant Markdown changes emit the custom sidebar-update event
  - ignored paths do not emit
- Add a small theme-side unit test for the site-data patch helper if that logic
  is extracted into a pure function.
- Re-run:
  - `npm test`
  - `npm run docs:build`
- Manual dev verification:
  - add a new Markdown file under an included folder and confirm the sidebar
    updates without restarting `docs:dev`
  - rename or delete an included file and confirm the sidebar removes or
    renames it
  - edit `title`, first `#` heading, or `sidebar: false` and confirm the
    sidebar updates live
  - confirm `nav` does not change

## Assumptions

- v1 only needs to refresh the current root sidebar entry (`"/"`); locale-
  specific sidebars are out of scope unless the repo adds locales later.
- Config edits such as changing the sidebar section registry can still use
  VitePress's normal config-restart behavior; live HMR is only for relevant
  Markdown content changes.
- If live sidebar HMR proves unstable in practice, the fallback is automatic
  full browser reload on the same watch events, but not manual server restart.
