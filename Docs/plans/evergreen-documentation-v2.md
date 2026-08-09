---
title: Evergreen Documentation V2 Rework
status: active
---

# Evergreen Documentation V2 Rework

## Goal

Rebuild the active evergreen documentation as a layered learning system that
teaches the project mental model, normal workflows, concrete Potion Panic
examples, failure consequences, and reference details without rewriting work
history.

## Boundaries

- Cover README, onboarding, collaboration, project truth, Unity guides,
  coordination guidance, research framing, navigation, and the coordination
  server runbook.
- Keep plans, tickets, boards, and archives as work records. Change only links,
  routes, and active path references required by the guide move.
- Do not change gameplay, Coordination runtime behavior, scenes, prefabs,
  packages, or project settings.
- Do not modify or validate the disputed Disabled/outage-policy passages.
- Do not add quizzes, reader tasks, or tests that assert documentation wording.

## Implementation sequence

1. Create `Docs/evergreen-documentation.md` and link it from README, AGENTS,
   and the Documentation Atlas.
2. Move `Docs/unity-guides/` to `Docs/guides/unity/`; update VitePress
   navigation, current references, active work metadata, and live archive
   links.
3. Rewrite Project Setup and Daily Workflow around first-machine readiness and
   the complete task lifecycle.
4. Clarify Game Design, MVP Scope, and the accepted target runtime design while
   preserving all accepted product decisions.
5. Rebuild the Unity guides and Unity Coordination Guide with mental models,
   examples, failure modes, and compact reference material.
6. Improve the Coordination Server runbook and research framing without
   changing the deferred outage policy or research conclusions.
7. Remove prose-content assertions; retain structural, workflow, link, and
   secret-leak safeguards.
8. Verify `npm test`, `npm run docs:build`, `git diff --check`, path migration,
   rendered navigation, and each page against the evergreen contract.

## Source authority

- Implementation behavior: `Assets/Scripts/Editor/Coordination/`, its EditMode
  tests, `Tools/CoordinationServer/src/`, and server tests.
- Project configuration: `coordination.json`, `ProjectSettings/ProjectVersion.txt`,
  `Packages/manifest.json`, `package.json`, and VitePress configuration.
- Accepted gameplay and delivery decisions: `Docs/project/` and active tickets.
- Historical rationale: plans and archives, used only when consistent with
  current authoritative sources.

## Verification baseline

- `npm run docs:build` passes before implementation.
- `npm test` starts with 13 passing tests and 4 failures caused by stale prose,
  security-scan, and navigation expectations.

## Post-V2 decision

[PP-9](../tickets/PP-9.md) is the separate task for deciding whether outages
should use the enabled local-save fallback, the explicit Disabled opt-out, or a
runtime behavior change. Only after approval should the affected guide, setup,
workflow, runbook, quick-reference, and troubleshooting passages be changed and
manually checked in Unity.
