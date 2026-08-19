# Evergreen documentation contract

Potion Panic uses evergreen documents for knowledge that should remain useful
after the task that introduced it has finished. This page defines what each
evergreen document must teach, where its facts come from, and where its
responsibility ends.

Use this contract when creating or revising documentation. Use the
[Documentation Atlas](ATLAS.md) when you only need to find the page that owns a
question.

## What useful evergreen documentation contains

A substantial guide should give the reader enough context to understand the
system rather than only enough instructions to imitate one successful run.
Include the layers the subject needs:

1. **Mental model:** define the parts, their responsibilities, and the terms the
   rest of the page uses.
2. **Normal path:** show the usual workflow or decision sequence in the order it
   happens.
3. **Concrete example:** apply the explanation to Potion Panic and connect each
   action to its observable result.
4. **Failure and recovery:** explain what can go wrong, why it matters, what
   work remains safe, and when the reader must stop.
5. **Reference:** keep commands, decision tables, and checklists easy to find
   after the concepts are understood.

Short routing pages do not need all five layers. They should state what their
section owns and direct each recurring question to one clear destination.

## Writing rules

- Define an unfamiliar term before relying on it.
- Explain the cause and consequence behind a rule when the reason is not
  obvious. “Do not edit this” is incomplete without the risk and safe path.
- Prefer exact paths, menu labels, commands, states, and expected results over
  vague instructions.
- Mark information as **current implementation**, **accepted target**, **planned
  work**, or **advisory guidance** when those categories could be confused.
- Give every fact one owner. Other pages may give enough context to continue,
  then link to the owner instead of maintaining another full copy.
- Use examples to explain decisions. Do not present unimplemented example code
  as current project behavior.
- Use as much explanation as the reader needs. Word count is not a quality
  target.
- Do not add quizzes or reader exercises to teaching pages.
- Do not create automated tests that require particular prose, headings, or
  command wording. Tests may protect routes, configuration, links, and secrets.

## Knowledge ownership

| Document or area                                                                         | Reader question                                                 | Knowledge it must convey                                                                                                                                                                        | It does not own                                                             | Primary evidence                                                                                       |
|------------------------------------------------------------------------------------------|-----------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------|
| `README.md`                                                                              | What is this repository, and how do I enter it?                 | Repository purpose, current starting point, first commands, local versus published docs, and routes to deeper owners.                                                                           | Full setup, daily workflow, game rules, or implementation detail.           | Repository layout, `package.json`, Unity version, and docs configuration.                              |
| `AGENTS.md`                                                                              | What rules must every contributor or agent follow?              | Short, always-read repository constraints and links to detailed owners.                                                                                                                         | Tutorials, design rationale, or task history.                               | Current repository policy and tooling.                                                                 |
| [`index.md`](index.md)                                                                   | Where should a docs-site visitor begin?                         | A small set of audience-oriented entry choices.                                                                                                                                                 | Detailed procedures or a complete file inventory.                           | Current VitePress navigation and evergreen owners.                                                     |
| [`ATLAS.md`](ATLAS.md)                                                                   | Which document owns a recurring question?                       | A routing map with one when-to-open description per knowledge area.                                                                                                                             | Tutorials, duplicated procedures, or lifecycle status tables.               | The active documentation tree.                                                                         |
| [`onboarding/getting-started.md`](onboarding/getting-started.md)                         | How do I make a new machine ready for work?                     | Prerequisites, repository preparation, docs tooling, Unity, Rider or VS Code, first smoke test, Coordination identity, expected results, and setup recovery.                                    | Recurring task execution or detailed Coordination behavior.                 | Manifests, scripts, Unity settings, `.gitignore`, and current editor UI.                               |
| [`collaboration/team-workflow.md`](collaboration/team-workflow.md)                       | How does a normal task move from selection to safe merge?       | Git state, GUI-first JetBrains and VS Code procedures, task choice, announcement, branching, shared-file safety, implementation, verification, handoff, review, local merge, and stabilization. | First-machine setup or detailed Unity editor guidance.                      | Repository rules, board contract, Git workflow, official IDE documentation, and verification commands. |
| [`project/game-design.md`](project/game-design.md)                                       | What should playing Potion Panic feel like?                     | Player fantasy, core loop, design pillars, content intent, pressure, reward, and presentation intent.                                                                                           | Locked delivery order, implementation ownership, or research claims.        | Accepted game-design decisions.                                                                        |
| [`project/mvp-scope.md`](project/mvp-scope.md)                                           | What is included, deferred, and delivered in what order?        | Current-versus-target state, locked MVP behavior and tuning, milestone dependencies, hard boundaries, and game-level completion.                                                                | Task status or component implementation detail.                             | Accepted scope decisions and active milestone tickets.                                                 |
| [`project/technical-architecture.md`](project/technical-architecture.md)                 | What runtime design is the team building toward?                | Accepted target data, responsibilities, ownership, communication, end-to-end flow, and completion criteria.                                                                                     | General Unity teaching or claims that planned gameplay code already exists. | Accepted architecture decisions, current runtime scaffold, and active tickets.                         |
| [`project/mvp-deliverables.md`](project/mvp-deliverables.md)                             | What concrete artifacts and verification does each MVP milestone require? | Milestone code, data, world/content, UI/presentation, integration deliverables, responsibility routing, asset maturity, suggested ticket seeds with draft acceptance/DoD guidance, and milestone-level evidence. | Scope/tuning, runtime responsibility definitions, live task status, assignees, final ticket acceptance criteria after creation, exact affected files, implementation plans, or implementation notes. | Accepted MVP Scope, Runtime Design, Game Design, and the team workflow contract. |
| [`guides/index.md`](guides/index.md)                                                     | Which practical guide applies to my task?                       | Task-based routes to Coordination, Unity working guides, and research.                                                                                                                          | Project-specific binding decisions.                                         | Active guide tree and project contracts.                                                               |
| [`guides/unity/runtime-architecture.md`](guides/unity/runtime-architecture.md)           | How should I reason about a Unity runtime?                      | Scenes, GameObjects, components, prefabs, data, runtime state, lifecycle, references, events, and state ownership.                                                                              | Potion Panic's binding component contract.                                  | Unity behavior and project examples.                                                                   |
| [`guides/unity/coding-and-implementation.md`](guides/unity/coding-and-implementation.md) | How do I turn one accepted behavior into a safe slice?          | Responsibility discovery, dependency choice, failure cases, incremental delivery, debug visibility, test boundaries, and handoff evidence.                                                      | Task-specific implementation plans.                                         | Repository layout, test assemblies, and accepted runtime contract.                                     |
| [`guides/unity/editor-safety.md`](guides/unity/editor-safety.md)                         | Why are Unity asset edits risky, and how do I make them safely? | Serialization, `.meta` GUIDs, scenes, prefabs, overrides, references, Play Mode, shared assets, settings, conflicts, and recovery.                                                              | Coordination service internals.                                             | Unity project settings, serialized assets, Git rules, and Coordination scope.                          |
| [`guides/unity/presentation-workflows.md`](guides/unity/presentation-workflows.md)       | How should UI and presentation work be built and handed off?    | UI layout, hierarchy, animation ownership, model import, materials, colliders, feedback hierarchy, and presentation verification.                                                               | Gameplay rule ownership.                                                    | Accepted visual intent and Unity project conventions.                                                  |
| [`guides/coordinated-leasing.md`](guides/coordinated-leasing.md)                         | What does the Unity Coordination tool do, and how do I use it?  | Developer identity, sessions, connections, presence, claims, automatic lifecycle, window actions, save conflicts, credentials, and developer troubleshooting.                                   | Worker deployment and secret administration.                                | Unity Coordination source, EditMode tests, `coordination.json`, and verified server protocol.          |
| [`research/game-design-and-psychology.md`](research/game-design-and-psychology.md)       | What evidence and heuristics can inform design evaluation?      | Research findings, industry guidance, ethical distinctions, production risk, market framing, and validation methods with evidence labels.                                                       | Binding Potion Panic decisions.                                             | Cited research and current primary platform sources.                                                   |
| `Tools/CoordinationServer/README.md`                                                     | How does an operator verify and administer the service safely?  | Trust model, environments, deployment, health checks, credentials, token operations, monitoring, protocol lifecycle, and stop conditions.                                                       | Normal Unity contributor workflow.                                          | Worker source, tests, Wrangler configuration, and deployment output.                                   |

## Work records

Tickets, active plans, chronicles, boards, and archives are not evergreen
tutorials. They record execution, evidence, rationale, and lifecycle state. Keep
them accurate for their role, but do not rewrite historical prose to match the
current teaching style. Correct live links and active file references when the
documentation structure moves.

## Resolving conflicts

Use the authority that describes the claim:

- code, tests, and configuration for implemented behavior;
- accepted project contracts for target behavior and scope;
- deployment output or live telemetry for external runtime state;
- primary research or platform sources for external claims;
- tickets, plans, and chronicles for task history and rationale.

If two authorities disagree, record the conflict and resolve it before writing
the disputed statement as fact.
