---
title: Evergreen Documentation V2 Chronicle
status: active
---

# Evergreen Documentation V2 Chronicle

## Current step

Review. The implementation and requested verification are complete; the plan
and chronicle remain in place because V2 does not archive work records.

## Approved decisions

- Use layered depth: mental model, normal workflow or decision path, concrete
  examples, failure consequences, and reference material.
- Keep indexes short and give each fact one evergreen owner.
- Add a standalone evergreen documentation contract.
- Move Unity guides under `Docs/guides/unity/`.
- Keep the Unity Coordination Guide path and retitle the page for readers.
- Preserve work history except required link and active-reference corrections.
- Retain structural and secret-leak checks; remove assertions about prose.
- Defer the Disabled/outage-policy decision until after V2.

## Baseline evidence

- Branch: `docs-major-overhaul-and-rewrite`.
- Worktree was clean before the plan and chronicle were added.
- `npm run docs:build` passed.
- `npm test` reported 13 passing and 4 failing tests.

## Final evidence

- `npm test` passed 13/13 after prose-content assertions were removed and the
  remaining checks were aligned with navigation, workflow configuration, and
  credential safety.
- `npm run docs:build` passed. The only output beyond success was the existing
  Vite chunk-size warning.
- `git diff --check` passed.
- The active documentation tree and configuration contain no links to the old
  `Docs/unity-guides/` or `/unity-guides/` hierarchy. Remaining text matches are
  the V2 move record and preserved archive prose or code examples.
- Local rendered inspection covered the home page, Project Setup, Daily
  Workflow, Project Overview, all three project-truth pages, the guide and
  Unity-guide indexes, all four Unity guides, Unity Coordination, research, the
  Atlas, and the evergreen contract. All 17 routes loaded without missing
  images, duplicate IDs, document-width overflow, or unresolved same-page
  anchors. The coordination tables, page outline, sidebar, and operator links
  were also inspected.
- Editorial review confirmed that substantial pages provide the applicable
  mental model, normal path, Potion Panic example, failure consequences, and
  reference details. Routing pages remain concise and each stable fact has an
  identified owner in the evergreen contract.

## Deferred decision

[PP-9](../tickets/PP-9.md) owns the Coordination outage-policy decision and any
approved runtime or documentation alignment. V2 preserved the disputed
Disabled and outage passages and did not validate either policy.

## Explanation checkpoint

- **Verified concepts:** evergreen owners separate current implementation,
  accepted target design, planned work, and advisory evidence; Coordination
  reservations are developer-owned while presence and editing leases are
  connection-owned; V2 changes documentation and structural tests only.
- **Conscious skips:** none.
- **Unresolved V2 gaps:** none. PP-9 is a separately scoped policy decision,
  not an unverified V2 completion claim.
- **Resolved divergences:** repository-root Atlas links were changed to valid
  repository URLs after the first VitePress build rejected links outside the
  docs source root.
