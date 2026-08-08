---
title: 'Private Reusable VitePress Docs Package Plan'
date: 2026-06-30
status: archived
archivedAt: 2026-08-08
originalPath: 'Docs/archive/completed/vitepress-private-reusable-package-plan-2026-06-30.md'
---

# Private Reusable VitePress Docs Package Plan

## Executive Summary

Extract the current VitePress docs tooling into a dedicated private package
repo, consumed by PotionPanic and future projects as a pinned private Git
dependency. The package should own the custom theme, markdown-backed
board/ticket/plan workflow, Vite plugins, and CLI helpers; PotionPanic should
keep only docs content, board page frontmatter, and thin VitePress adapter
files.

This plan assumes the chosen direction is one full package, in a separate
private repo, consumed via Git rather than a private npm registry. The
migration should preserve current behavior first, then clean up naming and
boundaries once PotionPanic is running through the package.

## Locked Decisions

- Package name: `Docboard`
- Scoped package import path: `@gabrielwawerski/docboard`
- Repo strategy: create `Docboard` as a new repository, not a GitHub fork of
  `McBrideMusings/vitepress-project-management`
- Distribution: private Git dependency from a dedicated private repo
- Git pinning: immutable tags or releases
- CLI integration: direct package bins in `package.json`
- PotionPanic path conventions stay unchanged during the initial
  implementation:
  - `Docs`
  - `plans`
  - `archive/completed`
  - `tickets`
  - `archive/tickets`
- Navigation direction for PotionPanic: workflow-first grouped navigation
- PotionPanic topbar group label for onboarding and workflow: `Handbook`
- PotionPanic keeps the sidebar section label `Unity Guides`
- Docboard package defaults use the more general section label `Guides`

## Provenance And Attribution

### Repository Lineage Decision

- `Docboard` should be created as a new repository rather than a GitHub fork
- The implementation should still preserve clear provenance to
  `McBrideMusings/vitepress-project-management`
- The reason for avoiding the GitHub fork model is that `Docboard` now has a
  broader scope, different package identity, and different long-term product
  direction than the upstream package

### README Attribution Wording

Use this wording in the `Docboard` repository README:

```md
## Provenance

Docboard was originally derived from
[`McBrideMusings/vitepress-project-management`](https://github.com/McBrideMusings/vitepress-project-management)
and has since been expanded into a broader private package for reusable
VitePress-based documentation, planning, and workflow tooling.

The original upstream project provided the starting point for the board/theme
and markdown-backed project-management workflow. Docboard continues from that
foundation under its own package identity and roadmap.
```

### Notice File

Create a small `NOTICE` file in the `Docboard` repo root with this content:

```text
Docboard includes work originally derived from:

  McBrideMusings/vitepress-project-management
  https://github.com/McBrideMusings/vitepress-project-management

Docboard has been modified and expanded beyond the original upstream package.
See README.md for current project scope and provenance details.
```

### License Handling

- Preserve the upstream MIT license notice in the new repository
- If the repository contains upstream-derived code, keep the MIT license text
  in `LICENSE`
- If needed, add a short note in `NOTICE` or README that portions were derived
  from the upstream package and later modified
- Treat provenance and license carry-forward as part of the initial repo
  scaffold, not as a follow-up cleanup task

## Inventory Of Reusable Modules

- `Docs/.vitepress/lib/markdown-writer-plugin.mjs`,
  `plan-writer.mjs`, `plan-archive.mjs`, `plan-common.mjs`,
  `plan-archive-page.mjs`
  - reusable markdown-backed CRUD, archive, index-sync, and dev-server
    middleware for tickets and plans
- `Docs/.vitepress/lib/sidebar.mjs`, `sidebar-hmr-plugin.mjs`,
  `sidebar-site-data.mjs`
  - reusable sidebar generation and live sidebar refresh
- `Docs/.vitepress/lib/ticket-sections.mjs`, `ticket-links.mjs`,
  `ticket-metadata.mjs`, `ticket-suggestions.mjs`, `board-ordering.mjs`,
  `board-notice.mjs`, `ticket-detail-layout.mjs`, `page-path-copy.mjs`,
  `board-shell.mjs`
  - reusable parsing, suggestion, ordering, responsive-layout, and board-shell
    helpers
- `Docs/.vitepress/theme/Layout.vue`, `theme/components/**`,
  `theme/composables/**`, `theme/styles/board.css`, `theme/types.ts`,
  `theme/index.ts`
  - reusable VitePress theme bundle
- `Scripts/docs/lib/docs-ui-launcher.mjs`, `Scripts/docs/open-board.mjs`,
  `Scripts/docs/create-ticket.mjs`
  - reusable CLI and developer-UX helpers that should become package bins
- Most non-startup tests under `Scripts/docs/lib/*.test.mjs`
  - reusable package characterization tests

## Inventory Of PotionPanic-Specific Parts

- All content under `Docs/**/*.md`
- [Docs/board.md](../../board.md) and [Docs/archive/board.md](../../archive/board.md) frontmatter
  - `board`, `boardMode`, `ticketsDir`, `archiveTicketsDir`,
    `restoreTicketsDir`, `ticketPrefix`, `defaultColumn`, `ticketSections`,
    `columns`, `demo`
- PotionPanic site identity and information architecture
  - title, description, GitHub URL, nav labels, sidebar sections, docs root
    label `Docs`, and current plan/archive directory conventions
- Host docs bootstrapping and usage docs in `README.md`
- Host content integrity tests such as
  `Scripts/docs/lib/plans-index-links.test.mjs`
- Startup scripts and startup-script tests remain out of scope

## Proposed Package Architecture

### Recommended Architecture

- Create a dedicated private repo with one package, internally split into
  `src/node/**`, `src/shared/**`, `src/theme/**`, `bin/**`, and `tests/**`
- Keep Node-facing entrypoints in runnable ESM `.mjs` so Git-based consumption
  does not require a publish/build step
- Keep theme code as source `.ts` and `.vue`
- Declare `vitepress` and `vue` as peer dependencies
- Keep `gray-matter` and similar Node utilities as normal package dependencies
- Keep low-level helpers internal; expose a small public API

### Alternatives And Tradeoffs

#### Chosen

- Dedicated full package repo plus private Git dependency
  - best reuse boundary and cleanest ownership split
  - tradeoff: broader package scope and Git-based release discipline

#### Alternative

- Same-repo workspace package first
  - lower extraction friction
  - tradeoff: second migration later and weaker reuse boundary

#### Alternative

- Split foundation package plus PM addon
  - better modularity for mixed-use projects
  - tradeoff: more API and versioning overhead now

## Proposed Public API Shape

### Host Project Config

```ts
// Docs/.vitepress/project-docs.config.ts
import {defineDocsProject} from "@gabrielwawerski/docboard";

export default defineDocsProject({
  title: "Potion Panic",
  description: "Shared project docs and task board for Potion Panic.",
  repoUrl: "https://github.com/gabrielwawerski/PotionPanic",
  docsRoot: "Docs",
  pagePathPrefix: "Docs",
  nav: [
    {text: "Home", link: "/"},
    {
      text: "Work",
      items: [
        {text: "Board", link: "/board"},
        {text: "Plans", link: "/plans/"},
        {text: "Milestones", link: "/milestones/"},
      ],
    },
    {
      text: "Project",
      items: [
        {text: "Game Design", link: "/project/game-design"},
        {text: "MVP Scope", link: "/project/mvp-scope"},
        {text: "Technical Architecture", link: "/project/technical-architecture"},
      ],
    },
    {
      text: "Guides",
      items: [
        {text: "Guides", link: "/guides/unity/"},
        {text: "Runtime Architecture", link: "/guides/unity/runtime-architecture"},
        {
          text: "Coding And Implementation",
          link: "/guides/unity/coding-and-implementation",
        },
        {text: "Editor Safety", link: "/guides/unity/editor-safety"},
        {
          text: "Presentation Workflows",
          link: "/guides/unity/presentation-workflows",
        },
      ],
    },
    {
      text: "Handbook",
      items: [
        {text: "Getting Started", link: "/onboarding/getting-started"},
        {text: "Workflow", link: "/collaboration/team-workflow"},
      ],
    },
    {text: "Archive", link: "/archive/"},
  ],
  socialLinks: [
    {icon: "github", link: "https://github.com/gabrielwawerski/PotionPanic"},
  ],
  sidebarExcludedDirs: ["tickets", "archive/tickets"],
  sidebarSections: [
    // PotionPanic-owned sidebar definition using:
    // Start Here / Active Work / Project Truth / Unity Guides / Archive
  ],
  plans: {
    activeDir: "plans",
    activeIndex: "plans/index.md",
    archiveDir: "archive/completed",
    archiveIndex: "archive/completed/index.md",
  },
});
```

### VitePress Config Adapter

```ts
// Docs/.vitepress/config.ts
import {fileURLToPath} from "node:url";
import project from "./project-docs.config";
import {createDocsConfig} from "@gabrielwawerski/docboard";

const docsDir = fileURLToPath(new URL("..", import.meta.url));

export default createDocsConfig(project, {docsDir});
```

### Theme Adapter

```ts
// Docs/.vitepress/theme/index.ts
export {projectManagementTheme as default} from "@gabrielwawerski/docboard/theme";
```

### CLI Shape

```bash
docboard open-board --url http://127.0.0.1:6420/board --script docs:dev
docboard create-ticket --board Docs/board.md --dir Docs/tickets
```

### Config Options To Preserve In V1

- Board-page frontmatter remains the main runtime contract:
  - `board`
  - `boardMode`
  - `ticketsDir`
  - `archiveTicketsDir`
  - `restoreTicketsDir`
  - `ticketPrefix`
  - `defaultColumn`
  - `ticketSections`
  - `columns`
  - `demo`
- Preserve the current live sidebar refresh behavior during extraction:
  - sidebar content changes should still rebuild sidebar data from config
  - the dev HMR path should keep updating only `themeConfig.sidebar`
  - `buildSidebarThemeConfig(...)` and `isSidebarContentPath(...)` should stay
    usable by the HMR plugin, even if they start consuming host-owned config
- Internal package naming should replace `potion-panic:*` event names, plugin
  names, and localStorage keys with generic `docboard:*` equivalents after the
  initial host refactor preserves the current live-update contract

## Proposed Navigation Structure

### PotionPanic Topbar

- `Home`
- `Work`
  - `Board`
  - `Plans`
  - `Milestones`
- `Project`
  - `Game Design`
  - `MVP Scope`
  - `Technical Architecture`
- `Guides`
  - `Guides`
  - `Runtime Architecture`
  - `Coding And Implementation`
  - `Editor Safety`
  - `Presentation Workflows`
- `Handbook`
  - `Getting Started`
  - `Workflow`
- `Archive`

### PotionPanic Sidebar

- `Start Here`
  - `Docs Home`
  - `Getting Started`
  - `Team Workflow`
- `Active Work`
  - `Board`
  - `Implementation Plans`
  - `Milestones`
- `Project Truth`
  - `Game Design`
  - `MVP Scope`
  - `Technical Architecture`
  - `Game Design And Psychology`
- `Unity Guides`
  - `Guides`
  - `Runtime Architecture`
  - `Coding And Implementation`
  - `Editor Safety`
  - `Presentation Workflows`
- `Archive`
  - `Archive`
  - `Archive Board`
  - `Archived Plans`

### Docboard Package Defaults

- Keep the same overall grouped navigation pattern as the PotionPanic example
- Use the general section label `Guides` in package defaults instead of
  `Unity Guides`
- Keep project-specific labels and item choices host-owned so non-Unity repos
  can rename or replace sections without forking Docboard internals

## Proposed Migration Strategy For PotionPanic

- Keep all `Docs/**/*.md` content in place
- Add a host-owned `Docs/.vitepress/project-docs.config.ts`
- Replace local implementation files with thin adapters for config and theme
- Add the private Git dependency to PotionPanic
- Keep `vitepress` and `vue` installed locally in the host repo
- Switch `package.json` scripts to direct `docboard` package bins after package
  integration
- Do not rename `Docs`, `plans`, `archive/completed`, `tickets`, or
  `archive/tickets` during the initial extraction
- Preserve current behavior first; cleanup comes after PotionPanic is running
  through the package

## Risks And Mitigation

- Tight coupling to current `Docs/`, `plans/`, and archive path conventions
  - mitigation: make these explicit host config options with current values as
    defaults
- Theme and plugin runtime coupling through custom HMR events and internal
  endpoints
  - mitigation: keep both sides inside the package and do not expose endpoints
    or events as public API; preserve the existing sidebar update payload shape
    during the first extraction step, then rename internals only after both
    emitter and listener move under package ownership
- Git dependency operational friction
  - mitigation: pin to tags, document update workflow, avoid requiring a
    build/publish step for installs
- Host test coverage collapse after moving unit tests out
  - mitigation: move logic tests to the package repo and keep a small number of
    thin host integration checks
- Hidden PotionPanic assumptions inside reusable code
  - mitigation: extract all site identity and sidebar content into host config
    before the repo split
- Startup-script churn
  - mitigation: keep startup scripts out of scope for this work

## Step-By-Step Implementation Plan

### Phase 1: Extract Host Config Without Changing Ownership

- Add `Docs/.vitepress/project-docs.config.ts` in PotionPanic
- Move PotionPanic-specific nav, sidebar, and site metadata there, using the
  locked workflow-first navigation structure
- Refactor local sidebar generation to consume host config instead of owning
  `SIDEBAR_SECTIONS`
- Preserve the current sidebar HMR contract in this phase:
  - keep the same dev update flow of rebuild -> emit sidebar payload -> replace
    only `themeConfig.sidebar`
  - do not rename custom sidebar update events yet while emitter and listener
    still live in the host repo
- Review gate: no behavior change, same board and docs pages still work locally

### Phase 2: Scaffold The Package Repo

- Create the private repo, package manifest, exports map, peer dependencies,
  CLI entrypoint, and test fixture site
- Create the repo as a new repository, not a GitHub fork
- Add provenance and licensing files during scaffold:
  - `README.md` provenance section using the locked wording
  - `NOTICE` file using the locked wording
  - `LICENSE` carrying forward the upstream MIT notice
- Copy reusable pure helpers and their unit tests first
- Review gate: package tests run in isolation against a fixture docs tree

### Phase 3: Move Node Workflow Logic Into The Package

- Move markdown writer, plan CRUD and archive logic, sidebar HMR, and related
  internals into `src/node/**`
- Preserve current middleware routes and filesystem behavior in v1
- Review gate: fixture supports create, update, archive, and restore ticket
  flows plus create, edit, and archive plan flows

### Phase 4: Move The Custom Theme Into The Package

- Move `Layout.vue`, components, composables, styles, and theme typing into
  `src/theme/**`
- Feed runtime options such as `pagePathPrefix` from package-built config
- Move the sidebar HMR emitter/listener pair together so package-owned internal
  event naming can change without breaking live updates
- Review gate: fixture site renders the board, ticket modal, plan authoring,
  read-only build behavior, and live sidebar updates

### Phase 5: Migrate PotionPanic To Consume The Package

- Add the Git dependency to PotionPanic
- Replace local implementation files with thin adapters
- Switch `package.json` scripts to direct `docboard` package bins
- Keep [Docs/board.md](../../board.md), [Docs/archive/board.md](../../archive/board.md), and all content files in place
- Review gate: `npm test`, `npm run docs:build`, and manual `npm run docs:ui`
  succeed in PotionPanic

### Phase 6: Remove Obsolete Local Implementation And Finalize Docs

- Delete moved local libs and theme implementation files from PotionPanic once
  package usage is stable
- Keep only host adapters, content, and host-specific tests
- Update `README.md` and docs setup text to describe the package-backed
  workflow
- Review gate: final host tree is thinner and no longer owns reusable logic

## Likely File Moves, Remains, And Changes

### Likely To Move Into The Package Repo

- `Docs/.vitepress/lib/*.mjs`
- `Docs/.vitepress/theme/Layout.vue`
- `Docs/.vitepress/theme/components/**`
- `Docs/.vitepress/theme/composables/**`
- `Docs/.vitepress/theme/styles/board.css`
- `Docs/.vitepress/theme/types.ts`
- `Scripts/docs/lib/docs-ui-launcher.mjs`
- `Scripts/docs/open-board.mjs`
- `Scripts/docs/create-ticket.mjs`
- Non-startup package-behavior tests under `Scripts/docs/lib/*.test.mjs`,
  except `plans-index-links.test.mjs`
- Upstream provenance and license context should also be carried into the new
  repo scaffold, even though those files are newly authored rather than moved

### Likely To Remain In PotionPanic

- `Docs/**/*.md`
- [Docs/board.md](../../board.md)
- [Docs/archive/board.md](../../archive/board.md)
- `README.md`
- `.gitignore`
- `Docs/.vitepress/config.ts` as a thin adapter
- `Docs/.vitepress/theme/index.ts` as a thin adapter
- `Scripts/docs/lib/plans-index-links.test.mjs`

### Likely To Change In PotionPanic

- `package.json`
- `package-lock.json`
- `Docs/.vitepress/config.ts`
- `Docs/.vitepress/theme/index.ts`
- add `Docs/.vitepress/project-docs.config.ts`
- remove the local `Docs/.vitepress/lib/` implementation directory after
  cutover
- remove local theme implementation files after cutover
- optionally add one small host integration test that checks package wiring

## Test Cases And Scenarios

- Package unit tests
  - ticket sections
  - ticket links
  - ticket metadata normalization
  - suggestion catalog generation
  - board ordering
  - page-path copy
  - plan create, update, and archive
  - sidebar generation
  - sidebar HMR payloads
  - docs UI launcher
- Package fixture integration
  - board loads ticket JSON in dev and static modes
  - create, edit, archive, and restore ticket flows work
  - create, edit, and archive plan flows update indexes
  - plan index sync still works on file changes
- PotionPanic host verification
  - `npm test`
  - `npm run docs:build`
  - manual `npm run docs:ui`
  - manual board smoke on [Docs/board.md](../../board.md)
  - manual archive-board smoke on [Docs/archive/board.md](../../archive/board.md)
- Regression focus
  - no behavior drift in frontmatter contract
  - no hardcoded `Potion Panic` strings inside package internals
  - no dependency on startup scripts

## Final Package Identity

- Package/tool name: `Docboard`
- Scoped package import path: `@gabrielwawerski/docboard`
- Repo creation model: new repository with explicit upstream attribution, not a
  GitHub fork

## Assumptions And Defaults

- Startup scripts and startup-script tests are excluded
- V1 preserves the current board and theme behavior instead of redesigning the
  workflow
- Future host projects keep `vitepress` and `vue` installed locally and consume
  the reusable package as a dev dependency
- Board behavior continues to be configured primarily by board-page frontmatter
  rather than moving all board settings into TypeScript
