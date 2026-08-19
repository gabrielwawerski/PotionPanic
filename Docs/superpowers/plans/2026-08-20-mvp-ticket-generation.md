# MVP Ticket Generation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the current Milestone 1 backlog tickets with the approved Patro/Gabro/Team decomposition, generate the complete MVP ticket set for Milestones 1-10, keep only Milestone 1 on the active board, and park Milestones 2-10 on the archive board with milestone tags for later restoration.

**Architecture:** Ticket files remain the source of board state. Milestone 1 tickets live under `Docs/tickets/`; future Milestone 2-10 tickets live under `Docs/archive/tickets/`. Each domain ticket has one assignee (`Patro` or `Gabro`), each cross-domain integration gate has assignee `Team`, and dependencies form a milestone DAG ending at the Team gate. Ticket prose is derived from the evergreen MVP contracts, while task-specific Implementation Plan sections are written as concrete hierarchical checklists.

**Tech Stack:** Markdown/YAML frontmatter, Docboard, VitePress, Node.js documentation tests, Git.

**Spec:** `Docs/superpowers/specs/2026-08-20-mvp-ticket-generation-design.md`

## Global Constraints

- Use exactly these assignee names: `Patro`, `Gabro`, `Team`.
- Prefer cohesive single-domain tickets; do not split trivial work merely because it touches more than one artifact.
- Do not put substantial Patro and Gabro implementation in one ticket; use a `Team` integration ticket with domain-ticket dependencies.
- Milestone 1 tickets live in `Docs/tickets/` and do not receive an `m-1` tag.
- Milestones 2-10 tickets live in `Docs/archive/tickets/`, use `status: backlog`, and include their milestone tag (`m-2` through `m-10`).
- Do not add a `future-milestone` tag.
- Acceptance Criteria always use Markdown task checkboxes.
- Implementation Plan always uses Markdown task checkboxes, with nested checkboxes for independently completable subtasks and plain nested bullets for constraints/clarifications.
- Definition of Done always uses Markdown task checkboxes.
- Future archived ticket Implementation Plans begin with a repository/dependency reinspection step.
- Future plans should still be as complete and concrete as current evidence permits.
- Do not invent APIs, method names, exact prefab hierarchies, animation/shader/VFX techniques, or exact file paths that are not supported by the current repository/contracts.
- `PP-2`, `PP-3`, and `PP-4` are deleted and their IDs are never reused.
- Before creating tickets, verify the complete active/archive ticket namespace; do not assume `PP-10` is free if repository state changed.
- Do not create speculative Milestone 10 bug tickets.
- Do not add automated tests that assert exact ticket prose.
- Do not change gameplay code, Unity assets, locked MVP tuning, or Docboard functionality.

## File Structure

### Existing files to delete

- `Docs/tickets/PP-2.md`
- `Docs/tickets/PP-3.md`
- `Docs/tickets/PP-4.md`

### Existing archive documentation to modify

- `Docs/archive/index.md` — expand archive semantics to include parked future-milestone tickets.
- `Docs/archive/tickets/index.md` — explain that this directory contains both historical records and parked future MVP tickets; do not manually enumerate all generated future tickets.

### New active Milestone 1 tickets

- `Docs/tickets/PP-10.md`
- `Docs/tickets/PP-11.md`
- `Docs/tickets/PP-12.md`
- `Docs/tickets/PP-13.md`

### New archived future tickets

- Milestone 2: `Docs/archive/tickets/PP-14.md` through `PP-16.md`
- Milestone 3: `Docs/archive/tickets/PP-17.md` through `PP-24.md`
- Milestone 4: `Docs/archive/tickets/PP-25.md` through `PP-31.md`
- Milestone 5: `Docs/archive/tickets/PP-32.md` through `PP-40.md`
- Milestone 6: `Docs/archive/tickets/PP-41.md` through `PP-50.md`
- Milestone 7: `Docs/archive/tickets/PP-51.md` through `PP-57.md`
- Milestone 8: `Docs/archive/tickets/PP-58.md` through `PP-65.md`
- Milestone 9: `Docs/archive/tickets/PP-66.md` through `PP-74.md`
- Milestone 10: `Docs/archive/tickets/PP-75.md` through `PP-78.md`

If ID verification finds a newer `PP-*` identifier, shift the entire generated range upward while preserving sequence and all dependency relationships. Do not fill historical gaps.

## Common ticket format

Every generated ticket uses the existing Docboard section order:

```md
---
id: <number>
title: <title>
status: <backlog|todo>
priority: medium
dependencies:
  - PP-<id>
documentation:
  - project/mvp-scope.md
  - project/mvp-deliverables.md
affectedFiles:
  - <stable area only>
tags:
  - <domain tag>
assignee: <Patro|Gabro|Team>
order: <id>
---

## Description

...

## Acceptance Criteria

- [ ] ...

## Implementation Plan

- [ ] ...

## Implementation Notes

...

## Definition of Done

- [ ] ...

## Notes
```

Rules:

- Omit `dependencies` when none exist.
- Omit unsupported/unknown `affectedFiles` entries instead of guessing.
- Do not add `milestone:` frontmatter unless execution-time inspection proves the current Docboard configuration has a valid milestone registry that should be used. The approved M2-M10 milestone classification is the `m-N` tag.
- Use `priority: medium` for the generated baseline. Dependencies and board state express sequencing; priority can be refined later from evidence without changing scope.
- Use `order: <ticket id>` for deterministic initial ordering.
- For archived M2-M10 tickets, include `m-N` alongside useful domain tags; the approved milestone tag is not a replacement for domain tags.
- Do not add `archivedAt` to future tickets created directly in the archive directory; it is historical lifecycle metadata, not required to render an archive-board ticket.
- Newly generated active tickets should have factual Implementation Notes such as `Generated from the approved MVP ticket-generation design; implementation has not started.`
- Future archived tickets should state that they were generated as parked future MVP work and have not started.

## Ticket ID and dependency map

The map below assumes current verification still establishes `PP-10` as the next globally unused integer ID.

| ID | Milestone | Assignee | Title | Direct dependencies |
|---|---:|---|---|---|
| PP-10 | 1 | Patro | Create Laboratory scene and laboratory blockout | — |
| PP-11 | 1 | Patro | Configure fixed orthographic camera | PP-10 |
| PP-12 | 1 | Gabro | Implement player movement and CharacterController setup | — |
| PP-13 | 1 | Team | Integrate and validate Milestone 1 | PP-10, PP-11, PP-12 |
| PP-14 | 2 | Gabro | Implement reusable IInteractable contract and interaction targeting | PP-13 |
| PP-15 | 2 | Patro | Create interaction prompt UI | PP-14 |
| PP-16 | 2 | Team | Integrate and validate reusable interaction flow | PP-14, PP-15 |
| PP-17 | 3 | Gabro | Implement ingredient and potion data contracts | PP-16 |
| PP-18 | 3 | Gabro | Implement PlayerInventory | PP-17 |
| PP-19 | 3 | Gabro | Implement IngredientStation | PP-17, PP-18 |
| PP-20 | 3 | Gabro | Implement BrewingStation | PP-17, PP-18 |
| PP-21 | 3 | Patro | Create first ingredient and potion presentation set | PP-16 |
| PP-22 | 3 | Patro | Create ingredient and brewing station blockouts | PP-16 |
| PP-23 | 3 | Patro | Create carried-item HUD | PP-18 |
| PP-24 | 3 | Team | Integrate and validate the ingredient-to-potion loop | PP-17, PP-18, PP-19, PP-20, PP-21, PP-22, PP-23 |
| PP-25 | 4 | Gabro | Implement disaster data contract and DisasterInstance | PP-24 |
| PP-26 | 4 | Gabro | Implement PanicSystem | PP-24 |
| PP-27 | 4 | Gabro | Implement potion application and disaster resolution rules | PP-25, PP-26 |
| PP-28 | 4 | Patro | Create Mana Hotspot presentation and escalation states | PP-24 |
| PP-29 | 4 | Patro | Create Panic meter UI | PP-26 |
| PP-30 | 4 | Patro | Create disaster resolution and wrong-potion feedback blockout | PP-27 |
| PP-31 | 4 | Team | Integrate and validate the first disaster loop | PP-25, PP-26, PP-27, PP-28, PP-29, PP-30 |
| PP-32 | 5 | Gabro | Implement run-state coordination foundation | PP-31 |
| PP-33 | 5 | Gabro | Implement disaster spawning and spawn-point logic | PP-32 |
| PP-34 | 5 | Gabro | Implement initial ScoreSystem | PP-31 |
| PP-35 | 5 | Gabro | Implement Game Over runtime behavior | PP-26, PP-32 |
| PP-36 | 5 | Gabro | Implement clean restart/reset behavior | PP-32, PP-33, PP-34, PP-35 |
| PP-37 | 5 | Patro | Place and validate disaster spawn locations | PP-33 |
| PP-38 | 5 | Patro | Create basic score HUD | PP-34 |
| PP-39 | 5 | Patro | Create Game Over and restart UI | PP-35 |
| PP-40 | 5 | Team | Integrate and validate the repeatable core game loop | PP-32, PP-33, PP-34, PP-35, PP-36, PP-37, PP-38, PP-39 |
| PP-41 | 6 | Gabro | Add remaining ingredient and potion data | PP-40 |
| PP-42 | 6 | Gabro | Add Slime Leak gameplay and data configuration | PP-41 |
| PP-43 | 6 | Gabro | Add Hex Cloud gameplay and data configuration | PP-41 |
| PP-44 | 6 | Gabro | Validate complete MVP content data relationships | PP-41, PP-42, PP-43 |
| PP-45 | 6 | Patro | Create production-ready ingredient presentation set | PP-41 |
| PP-46 | 6 | Patro | Create production-ready potion presentation set | PP-41 |
| PP-47 | 6 | Patro | Create Slime Leak presentation | PP-42 |
| PP-48 | 6 | Patro | Create Hex Cloud presentation | PP-43 |
| PP-49 | 6 | Patro | Complete production-ready disaster presentation pass | PP-28, PP-47, PP-48 |
| PP-50 | 6 | Team | Integrate and validate all three MVP content chains | PP-44, PP-45, PP-46, PP-47, PP-48, PP-49 |
| PP-51 | 7 | Gabro | Implement time-based difficulty stages | PP-50 |
| PP-52 | 7 | Gabro | Implement difficulty spawning rules | PP-33, PP-51 |
| PP-53 | 7 | Gabro | Implement final scoring rules | PP-34, PP-51 |
| PP-54 | 7 | Gabro | Implement Stage 4 disaster tuning | PP-51 |
| PP-55 | 7 | Gabro | Add difficulty and scoring boundary coverage | PP-51, PP-52, PP-53, PP-54 |
| PP-56 | 7 | Patro | Complete score presentation and score-change feedback | PP-53 |
| PP-57 | 7 | Team | Integrate and validate final difficulty and scoring progression | PP-51, PP-52, PP-53, PP-54, PP-55, PP-56 |
| PP-58 | 8 | Gabro | Implement final run-state transitions | PP-57 |
| PP-59 | 8 | Gabro | Implement pause and resume simulation and input behavior | PP-58 |
| PP-60 | 8 | Gabro | Complete final restart path | PP-58 |
| PP-61 | 8 | Patro | Create production-ready Main Menu | PP-58 |
| PP-62 | 8 | Patro | Create production-ready Pause Menu | PP-59 |
| PP-63 | 8 | Patro | Create production-ready Game Over screen | PP-58 |
| PP-64 | 8 | Patro | Implement HUD and menu visibility presentation | PP-58, PP-61, PP-62, PP-63 |
| PP-65 | 8 | Team | Integrate and validate complete start-to-finish run flow | PP-58, PP-59, PP-60, PP-61, PP-62, PP-63, PP-64 |
| PP-66 | 9 | Gabro | Implement gameplay-to-presentation event hooks | PP-65 |
| PP-67 | 9 | Gabro | Implement shared audio routing and AudioManager support | PP-65 |
| PP-68 | 9 | Patro | Produce core pickup, brewing, and potion feedback package | PP-65 |
| PP-69 | 9 | Patro | Produce Mana Hotspot feedback package | PP-65 |
| PP-70 | 9 | Patro | Produce Slime Leak feedback package | PP-65 |
| PP-71 | 9 | Patro | Produce Hex Cloud feedback package | PP-65 |
| PP-72 | 9 | Patro | Produce Panic warning feedback package | PP-65 |
| PP-73 | 9 | Patro | Produce Game Over and essential UI feedback package | PP-65 |
| PP-74 | 9 | Team | Integrate and validate the complete feedback coverage matrix | PP-66, PP-67, PP-68, PP-69, PP-70, PP-71, PP-72, PP-73 |
| PP-75 | 10 | Gabro | Validate final MVP gameplay tuning and runtime stability | PP-74 |
| PP-76 | 10 | Patro | Audit and replace remaining release-path placeholders | PP-74 |
| PP-77 | 10 | Patro | Validate final late-game presentation and readability | PP-74 |
| PP-78 | 10 | Team | Run final MVP release validation | PP-75, PP-76, PP-77 |

---

### Task 1: Verify ticket namespace and current contracts

**Files:**
- Read: `Docs/tickets/`
- Read: `Docs/archive/tickets/`
- Read: `Docs/project/mvp-scope.md`
- Read: `Docs/project/mvp-deliverables.md`
- Read: `Docs/project/technical-architecture.md`
- Read: `Docs/project/game-design.md`
- Read: `Docs/collaboration/team-workflow.md`
- Read: `Docs/board.md`
- Read: `Docs/archive/board.md`
- Read: `Docs/superpowers/specs/2026-08-20-mvp-ticket-generation-design.md`

**Interfaces:**
- Consumes: current repository ticket namespace and evergreen MVP contracts.
- Produces: confirmed starting ID, confirmed ticket schema, and the exact dependency map used by all later tasks.

- [ ] **Step 1: Start from an isolated execution branch/worktree**

Use `superpowers:using-git-worktrees` at execution time when a local checkout is available. Base the execution branch on the then-current `master`, not on an older planning commit.

- [ ] **Step 2: Enumerate all active and archived ticket filenames**

Run:

```bash
find Docs/tickets Docs/archive/tickets -maxdepth 1 -type f -name 'PP-*.md' -print | sort -V
```

Expected current historical integer IDs: `1` through `9`, with `PP-2`, `PP-3`, `PP-4` active and `PP-5`, `PP-6`, `PP-7` archived. Decimal historical IDs such as `PP-1.1` remain historical and do not change the next integer sequence.

- [ ] **Step 3: Verify no `PP-10` or later integer ticket exists**

Run:

```bash
find Docs/tickets Docs/archive/tickets -maxdepth 1 -type f -name 'PP-*.md' -print \
  | grep -E '/PP-([1-9][0-9]+)\.md$' \
  | sort -V
```

Expected before generation: no result at or above `PP-10.md`.

If this expectation is false, shift the complete new ID range upward and update every dependency consistently before creating files.

- [ ] **Step 4: Re-read the MVP contracts for drift since the spec was approved**

Confirm at minimum:

- disaster identities remain Mana Hotspot, Slime Leak, Hex Cloud;
- ingredients/potions remain Blue Mushroom/Cooling Potion, Green Slime/Slime Dissolver, Purple Crystal Dust/Purification Potion;
- wrong-potion behavior remains consumed potion, disaster active, `+10 Panic`, no score;
- fixed top-down orthographic camera and CharacterController movement remain locked;
- M2-M10 remain future work in milestone order;
- no newly implemented runtime behavior makes a planned ticket redundant.

If a binding contract changed, stop and reconcile the ticket plan before writing ticket files.

- [ ] **Step 5: Verify Docboard frontmatter/section schema**

Confirm both boards still use:

```text
Description
Acceptance Criteria
Implementation Plan
Implementation Notes
Definition of Done
Notes
```

Confirm archive restore still targets `Docs/tickets/`.

- [ ] **Step 6: Record the verified ID range and schema in the execution notes/commit message**

No repository file is required solely for this record; the point is to establish the assumptions used by Tasks 2-13.

---

### Task 2: Replace obsolete Milestone 1 backlog and update archive semantics

**Files:**
- Delete: `Docs/tickets/PP-2.md`
- Delete: `Docs/tickets/PP-3.md`
- Delete: `Docs/tickets/PP-4.md`
- Modify: `Docs/archive/index.md`
- Modify: `Docs/archive/tickets/index.md`

**Interfaces:**
- Consumes: approved lifecycle model from the spec.
- Produces: no overlapping M1 backlog and archive documentation that correctly describes parked future tickets.

- [ ] **Step 1: Reconfirm PP-2/PP-3/PP-4 are still unstarted/replacement-safe**

Read their current frontmatter and Implementation Notes. If any has moved to `doing`, gained implementation evidence, or been materially executed since planning, stop instead of deleting live work.

- [ ] **Step 2: Delete `PP-2`, `PP-3`, and `PP-4`**

Do not archive or renumber them. Their IDs remain retired historical identifiers visible in Git history.

- [ ] **Step 3: Update `Docs/archive/index.md` archive semantics**

Replace the statement that archive contains only completed/superseded work with wording that also permits deliberately parked future-milestone tickets. Keep completed/superseded history explicitly supported.

- [ ] **Step 4: Update `Docs/archive/tickets/index.md`**

State that the directory contains:

- completed/superseded task records;
- parked future MVP tickets that remain `backlog` until restored.

Explain that future MVP tickets are identified by their `m-N` tag and board state. Do not add a 60+ item hand-maintained link list.

- [ ] **Step 5: Inspect the diff**

Run:

```bash
git diff -- Docs/tickets/PP-2.md Docs/tickets/PP-3.md Docs/tickets/PP-4.md Docs/archive/index.md Docs/archive/tickets/index.md
```

Expected: only the three deletions and archive wording changes.

- [ ] **Step 6: Commit**

```bash
git add Docs/tickets/PP-2.md Docs/tickets/PP-3.md Docs/tickets/PP-4.md Docs/archive/index.md Docs/archive/tickets/index.md
git commit -m "docs(board): prepare MVP ticket lifecycle"
```

---

### Task 3: Create active Milestone 1 tickets

**Files:**
- Create: `Docs/tickets/PP-10.md`
- Create: `Docs/tickets/PP-11.md`
- Create: `Docs/tickets/PP-12.md`
- Create: `Docs/tickets/PP-13.md`

**Interfaces:**
- Consumes: M1 sections of MVP Scope, Runtime Design, MVP Deliverables, and the dependency map above.
- Produces: complete active M1 workstream with Patro/Gabro separation and Team exit gate.

- [ ] **Step 1: Create PP-10 — Create Laboratory scene and laboratory blockout**

Frontmatter:

- assignee `Patro`;
- status `todo`;
- priority `medium`;
- tags include `world`, `blockout`;
- no dependencies.

Implementation Plan must concretely cover:

- inspect current shared scene and project scene references;
- create/convert `Laboratory.unity` while preserving required setup;
- establish floor footprint, perimeter walls/boundaries, collision, traversal space;
- keep blockout maturity and avoid final dressing;
- check room dimensions against player collision expectations;
- clean hierarchy;
- review serialized scene diff;
- manual Unity validation and Console review.

- [ ] **Step 2: Create PP-11 — Configure fixed orthographic camera**

Frontmatter:

- assignee `Patro`;
- status `backlog`;
- dependency `PP-10`;
- tags include `camera`, `world`.

Implementation Plan must cover:

- inspect final M1 blockout dimensions;
- configure a static orthographic main camera;
- frame the complete intended playable area;
- validate no follow/look behavior is introduced;
- validate readability at target resolution;
- review scene diff and Console.

- [ ] **Step 3: Create PP-12 — Implement player movement and CharacterController setup**

Frontmatter:

- assignee `Gabro`;
- status `todo`;
- no dependencies;
- tags include `gameplay`, `player`, `movement`.

Implementation Plan must cover:

- inspect current input scaffold/player runtime;
- add focused EditMode coverage for axis conversion and diagonal normalization where practical;
- implement `PlayerController` using accepted `CharacterController` movement;
- world X/Z movement only;
- normalized diagonal speed;
- configure player CharacterController/runtime setup;
- explicitly exclude sprint, jump, mouse look, camera-relative movement;
- run focused tests;
- manual Play Mode collision/movement smoke;
- Console/reference review.

- [ ] **Step 4: Create PP-13 — Integrate and validate Milestone 1**

Frontmatter:

- assignee `Team`;
- status `backlog`;
- dependencies `PP-10`, `PP-11`, `PP-12`;
- tags include `integration`, `testing`.

Implementation Plan must cover:

- confirm all three dependencies complete;
- inspect merged Patro/Gabro deliverables;
- wire required scene/player references without absorbing new domain work;
- run relevant EditMode/PlayMode verification;
- validate WASD, normalized movement, collisions, full room reachability, fixed camera;
- review Console and serialized scene/prefab diff;
- create follow-up domain ticket if integration exposes substantial missing implementation.

- [ ] **Step 5: Verify M1 ticket structure**

Confirm all Acceptance Criteria, Implementation Plan, and Definition of Done items use `- [ ]` syntax, and no M1 ticket contains an `m-1` tag.

- [ ] **Step 6: Commit**

```bash
git add Docs/tickets/PP-10.md Docs/tickets/PP-11.md Docs/tickets/PP-12.md Docs/tickets/PP-13.md
git commit -m "docs(tickets): create Milestone 1 workstream"
```

---

### Task 4: Create archived Milestone 2 tickets

**Files:**
- Create: `Docs/archive/tickets/PP-14.md`
- Create: `Docs/archive/tickets/PP-15.md`
- Create: `Docs/archive/tickets/PP-16.md`

**Interfaces:**
- Consumes: completed M1 Team gate `PP-13` and M2 interaction contracts.
- Produces: parked M2 interaction workstream tagged `m-2`.

For every ticket in this and Tasks 5-12:

- use `status: backlog`;
- include the milestone tag;
- begin Implementation Plan with repository/dependency reinspection after restore;
- include factual note that the ticket is parked future work and has not started.

- [ ] **Step 1: Create PP-14 — Implement reusable IInteractable contract and interaction targeting**

Assignee `Gabro`; dependency `PP-13`; tags include `m-2`, `gameplay`, `interaction`.

Implementation Plan must cover interface/targeting inspection, deterministic target selection, nearby detection, Interact input, target clearing, isolation from station/disaster-specific logic, EditMode/PlayMode coverage, and runtime smoke validation.

- [ ] **Step 2: Create PP-15 — Create interaction prompt UI**

Assignee `Patro`; dependency `PP-14`; tags include `m-2`, `ui`, `interaction`.

Implementation Plan must cover observing current-target state, prompt visible/hidden states, input indication/readability, blockout maturity, no gameplay-state ownership, manual enter/leave/readability checks, and UI/reference review.

- [ ] **Step 3: Create PP-16 — Integrate and validate reusable interaction flow**

Assignee `Team`; dependencies `PP-14`, `PP-15`; tags include `m-2`, `integration`, `testing`.

Implementation Plan must cover dependency confirmation, temporary proof interactable, prompt wiring, select/interact/leave path, automated verification where practical, manual Play Mode proof, and Console/serialized-reference review.

- [ ] **Step 4: Commit**

```bash
git add Docs/archive/tickets/PP-14.md Docs/archive/tickets/PP-15.md Docs/archive/tickets/PP-16.md
git commit -m "docs(tickets): create Milestone 2 archive"
```

---

### Task 5: Create archived Milestone 3 tickets

**Files:**
- Create: `Docs/archive/tickets/PP-17.md` through `PP-24.md`

**Interfaces:**
- Consumes: M2 gate `PP-16` and M3 inventory/brewing contracts.
- Produces: parked M3 ingredient-to-potion workstream tagged `m-3`.

- [ ] **Step 1: Create PP-17 — Implement ingredient and potion data contracts**

Assignee `Gabro`; dependency `PP-16`; tags `m-3`, `gameplay`, `data`.

Plan focus: inspect current data organization; define `IngredientData`/`PotionData` ScriptableObject responsibilities; configure Blue Mushroom → Cooling Potion relationship; include UI/visual metadata required by accepted contract; validate data references; avoid hardcoded chain logic.

- [ ] **Step 2: Create PP-18 — Implement PlayerInventory**

Assignee `Gabro`; dependency `PP-17`; tags `m-3`, `gameplay`, `inventory`.

Use the approved hierarchical style. Plan must include focused EditMode coverage for empty inventory, ingredient pickup, potion pickup, item replacement, consumption, invalid transitions; implement authoritative one-item state; inventory operations for acquire/replace/consume/clear; observable state changes; player wiring without station-specific logic; tests and smoke verification.

- [ ] **Step 3: Create PP-19 — Implement IngredientStation**

Assignee `Gabro`; dependencies `PP-17`, `PP-18`; tags `m-3`, `gameplay`, `interaction`.

Plan focus: reusable `IInteractable`, configured IngredientData, pickup refusal while occupied, no inventory ownership, tests for success/rejection, scene/prefab wiring only where required.

- [ ] **Step 4: Create PP-20 — Implement BrewingStation**

Assignee `Gabro`; dependencies `PP-17`, `PP-18`; tags `m-3`, `gameplay`, `brewing`.

Plan focus: reusable interaction path, ingredient requirement, configured `resultingPotion`, ingredient-to-potion replacement, refusal without ingredient/when invalid, no Blue Mushroom-specific branching, tests and runtime validation.

- [ ] **Step 5: Create PP-21 — Create first ingredient and potion presentation set**

Assignee `Patro`; dependency `PP-16`; tags `m-3`, `content`, `world`.

Plan focus: Blue Mushroom and Cooling Potion blockout/readable representations, required material/icon metadata, fixed-camera distinguishability, scale/orientation checks, import/reference review.

- [ ] **Step 6: Create PP-22 — Create ingredient and brewing station blockouts**

Assignee `Patro`; dependency `PP-16`; tags `m-3`, `world`, `content`.

Plan focus: station blockouts, readable interaction locations, correct footprint/collision where needed, no final-art overproduction, scene/prefab safety review.

- [ ] **Step 7: Create PP-23 — Create carried-item HUD**

Assignee `Patro`; dependency `PP-18`; tags `m-3`, `ui`, `ux`.

Plan focus: empty/ingredient/potion states, observe inventory changes, distinguish Blue Mushroom/Cooling Potion, no mutation of inventory, fixed-camera/HUD readability, manual state-transition checks.

- [ ] **Step 8: Create PP-24 — Integrate and validate the ingredient-to-potion loop**

Assignee `Team`; dependencies `PP-17` through `PP-23`; tags `m-3`, `integration`, `testing`.

Plan focus: empty → pickup → carry → brew → consume ingredient → receive Cooling Potion → HUD update; rejection cases; automated coverage where suitable; manual end-to-end validation; Console/reference review.

- [ ] **Step 9: Commit**

```bash
git add Docs/archive/tickets/PP-17.md Docs/archive/tickets/PP-18.md Docs/archive/tickets/PP-19.md Docs/archive/tickets/PP-20.md Docs/archive/tickets/PP-21.md Docs/archive/tickets/PP-22.md Docs/archive/tickets/PP-23.md Docs/archive/tickets/PP-24.md
git commit -m "docs(tickets): create Milestone 3 archive"
```

---

### Task 6: Create archived Milestone 4 tickets

**Files:**
- Create: `Docs/archive/tickets/PP-25.md` through `PP-31.md`

**Interfaces:**
- Consumes: M3 gate `PP-24` and first-disaster/Panic contracts.
- Produces: parked M4 workstream tagged `m-4`.

- [ ] **Step 1: Create PP-25 — Implement disaster data contract and DisasterInstance**

Assignee `Gabro`; dependency `PP-24`; tags `m-4`, `gameplay`, `disaster`.

Plan focus: `DisasterData`, `DisasterType`, `DisasterInstance`, required potion, age/escalation state, base/escalated Panic contribution contract, resolved notification, reusable content-driven behavior, tests for state transitions/data validation.

- [ ] **Step 2: Create PP-26 — Implement PanicSystem**

Assignee `Gabro`; dependency `PP-24`; tags `m-4`, `gameplay`, `systems`.

Plan focus: Panic storage, add/reduce, clamp 0-100, change notifications, values from MVP Scope, isolation from UI, EditMode tests.

- [ ] **Step 3: Create PP-27 — Implement potion application and disaster resolution rules**

Assignee `Gabro`; dependencies `PP-25`, `PP-26`; tags `m-4`, `gameplay`, `disaster`.

Plan focus: common interaction application path; correct required-potion comparison; correct resolution stops disaster and applies `-10 Panic`; wrong potion consumed, disaster remains, `+10 Panic`, no score; inventory integration; tests for both paths.

- [ ] **Step 4: Create PP-28 — Create Mana Hotspot presentation and escalation states**

Assignee `Patro`; dependency `PP-24`; tags `m-4`, `content`, `vfx`.

Plan focus: blockout Mana Hotspot representation, distinct normal/escalated states, fixed-camera readability, spawn-location compatibility, no superseded cauldron identity.

- [ ] **Step 5: Create PP-29 — Create Panic meter UI**

Assignee `Patro`; dependency `PP-26`; tags `m-4`, `ui`, `ux`.

Plan focus: observe Panic state, 0-100 readability, updates during runtime, no Panic ownership/rule logic in UI, blockout maturity, manual high/low checks.

- [ ] **Step 6: Create PP-30 — Create disaster resolution and wrong-potion feedback blockout**

Assignee `Patro`; dependency `PP-27`; tags `m-4`, `vfx`, `feedback`.

Plan focus: distinguish success from wrong potion, blockout feedback only, no bespoke post-MVP wrong-reaction matrix, fixed-camera readability.

- [ ] **Step 7: Create PP-31 — Integrate and validate the first disaster loop**

Assignee `Team`; dependencies `PP-25` through `PP-30`; tags `m-4`, `integration`, `testing`.

Plan focus: Mana Hotspot active Panic, escalation, Cooling Potion success, wrong-potion path, Panic UI, resolution behavior, automated and manual end-to-end proof.

- [ ] **Step 8: Commit**

```bash
git add Docs/archive/tickets/PP-25.md Docs/archive/tickets/PP-26.md Docs/archive/tickets/PP-27.md Docs/archive/tickets/PP-28.md Docs/archive/tickets/PP-29.md Docs/archive/tickets/PP-30.md Docs/archive/tickets/PP-31.md
git commit -m "docs(tickets): create Milestone 4 archive"
```

---

### Task 7: Create archived Milestone 5 tickets

**Files:**
- Create: `Docs/archive/tickets/PP-32.md` through `PP-40.md`

**Interfaces:**
- Consumes: M4 gate `PP-31` and repeatable-run contracts.
- Produces: parked M5 core-loop workstream tagged `m-5`.

- [ ] **Step 1: Create PP-32 — Implement run-state coordination foundation**

Assignee `Gabro`; dependency `PP-31`; plan focus: `GameManager` current run state, initialization, start/end ownership needed by M5, no menu overreach, state reset boundaries.

- [ ] **Step 2: Create PP-33 — Implement disaster spawning and spawn-point logic**

Assignee `Gabro`; dependency `PP-32`; plan focus: `DisasterManager`, `DisasterSpawnPoint`, first-spawn delay, recurring schedule, active tracking, occupancy, valid location selection, reset handling; leave final M7 stage/cap rules for M7 where not yet required.

- [ ] **Step 3: Create PP-34 — Implement initial ScoreSystem**

Assignee `Gabro`; dependency `PP-31`; plan focus: authoritative score state, minimum resolve-score support needed for basic display, reset behavior, observable updates, no final M7 scoring rules early.

- [ ] **Step 4: Create PP-35 — Implement Game Over runtime behavior**

Assignee `Gabro`; dependencies `PP-26`, `PP-32`; plan focus: Panic 100 transition, stop/suppress active gameplay appropriately for M5, no presentation ownership, runtime tests.

- [ ] **Step 5: Create PP-36 — Implement clean restart/reset behavior**

Assignee `Gabro`; dependencies `PP-32`, `PP-33`, `PP-34`, `PP-35`; plan focus: accepted `Laboratory.unity` reload/reset path, Panic/score/timers/disasters/occupancy cleanup, repeated restart tests.

- [ ] **Step 6: Create PP-37 — Place and validate disaster spawn locations**

Assignee `Patro`; dependency `PP-33`; plan focus: scene locations compatible with laboratory topology, collision/visibility/readability, enough valid points for later caps, no location-specific disaster identity.

- [ ] **Step 7: Create PP-38 — Create basic score HUD**

Assignee `Patro`; dependency `PP-34`; plan focus: observe ScoreSystem, readable blockout score display, reset updates, no score calculation in UI.

- [ ] **Step 8: Create PP-39 — Create Game Over and restart UI**

Assignee `Patro`; dependency `PP-35`; plan focus: blockout Game Over state, restart control, readable state distinction, hook to runtime commands without owning state.

- [ ] **Step 9: Create PP-40 — Integrate and validate the repeatable core game loop**

Assignee `Team`; dependencies `PP-32` through `PP-39`; plan focus: start → first spawn after accepted delay → resolve → recurring spawn → Panic 100 → Game Over → restart → clean new run; repeated smoke and automated coverage.

- [ ] **Step 10: Commit**

```bash
git add Docs/archive/tickets/PP-32.md Docs/archive/tickets/PP-33.md Docs/archive/tickets/PP-34.md Docs/archive/tickets/PP-35.md Docs/archive/tickets/PP-36.md Docs/archive/tickets/PP-37.md Docs/archive/tickets/PP-38.md Docs/archive/tickets/PP-39.md Docs/archive/tickets/PP-40.md
git commit -m "docs(tickets): create Milestone 5 archive"
```

---

### Task 8: Create archived Milestone 6 tickets

**Files:**
- Create: `Docs/archive/tickets/PP-41.md` through `PP-50.md`

**Interfaces:**
- Consumes: M5 gate `PP-40` and complete three-chain content contract.
- Produces: parked M6 content workstream tagged `m-6`.

- [ ] **Step 1: Create PP-41 — Add remaining ingredient and potion data**

Assignee `Gabro`; dependency `PP-40`; cover Green Slime → Slime Dissolver and Purple Crystal Dust → Purification Potion using existing data contracts.

- [ ] **Step 2: Create PP-42 — Add Slime Leak gameplay and data configuration**

Assignee `Gabro`; dependency `PP-41`; reuse generic DisasterInstance/data path; configure Slime Dissolver requirement and accepted tuning source; no bespoke framework.

- [ ] **Step 3: Create PP-43 — Add Hex Cloud gameplay and data configuration**

Assignee `Gabro`; dependency `PP-41`; reuse generic path; configure Purification Potion requirement; no bespoke framework.

- [ ] **Step 4: Create PP-44 — Validate complete MVP content data relationships**

Assignee `Gabro`; dependencies `PP-41`, `PP-42`, `PP-43`; validate all ingredient→potion and disaster→required-potion references and reusable runtime contracts.

- [ ] **Step 5: Create PP-45 — Create production-ready ingredient presentation set**

Assignee `Patro`; dependency `PP-41`; production-ready Blue Mushroom, Green Slime, Purple Crystal Dust representations/icons/material identity as required.

- [ ] **Step 6: Create PP-46 — Create production-ready potion presentation set**

Assignee `Patro`; dependency `PP-41`; production-ready Cooling Potion, Slime Dissolver, Purification Potion representations and accepted color/visual relationships.

- [ ] **Step 7: Create PP-47 — Create Slime Leak presentation**

Assignee `Patro`; dependency `PP-42`; production-ready normal/escalation/resolution-readable form compatible with random spawn locations.

- [ ] **Step 8: Create PP-48 — Create Hex Cloud presentation**

Assignee `Patro`; dependency `PP-43`; same quality/readability contract for Hex Cloud.

- [ ] **Step 9: Create PP-49 — Complete production-ready disaster presentation pass**

Assignee `Patro`; dependencies `PP-28`, `PP-47`, `PP-48`; bring Mana Hotspot and all disaster presentation to consistent M6 production readiness without prematurely requiring all M9 audio/VFX polish.

- [ ] **Step 10: Create PP-50 — Integrate and validate all three MVP content chains**

Assignee `Team`; dependencies `PP-44` through `PP-49`; validate all three chains through the same systems, distinguishability, spawn compatibility, and exclusion of post-MVP wrong-potion reactions/topology escalation.

- [ ] **Step 11: Commit**

```bash
git add Docs/archive/tickets/PP-41.md Docs/archive/tickets/PP-42.md Docs/archive/tickets/PP-43.md Docs/archive/tickets/PP-44.md Docs/archive/tickets/PP-45.md Docs/archive/tickets/PP-46.md Docs/archive/tickets/PP-47.md Docs/archive/tickets/PP-48.md Docs/archive/tickets/PP-49.md Docs/archive/tickets/PP-50.md
git commit -m "docs(tickets): create Milestone 6 archive"
```

---

### Task 9: Create archived Milestone 7 tickets

**Files:**
- Create: `Docs/archive/tickets/PP-51.md` through `PP-57.md`

**Interfaces:**
- Consumes: M6 gate `PP-50` and locked difficulty/scoring values from MVP Scope.
- Produces: parked M7 difficulty/scoring workstream tagged `m-7`.

- [ ] **Step 1: Create PP-51 — Implement time-based difficulty stages**

Assignee `Gabro`; dependency `PP-50`; implement elapsed runtime and exact 0/60/120/180-second stage boundaries using MVP Scope as numeric authority.

- [ ] **Step 2: Create PP-52 — Implement difficulty spawning rules**

Assignee `Gabro`; dependencies `PP-33`, `PP-51`; hierarchical plan for stage spawn intervals, active-disaster caps, skipped spawn at cap, no backlog, equal weighting.

- [ ] **Step 3: Create PP-53 — Implement final scoring rules**

Assignee `Gabro`; dependencies `PP-34`, `PP-51`; +100 resolve, +50 within 10 seconds, +1 per full survival second, no combo scoring, reset compatibility.

- [ ] **Step 4: Create PP-54 — Implement Stage 4 disaster tuning**

Assignee `Gabro`; dependency `PP-51`; exact Stage 4 rates/escalation time sourced from MVP Scope; no duplication into another authority document.

- [ ] **Step 5: Create PP-55 — Add difficulty and scoring boundary coverage**

Assignee `Gabro`; dependencies `PP-51` through `PP-54`; focused boundary checks for 59→60, 119→120, 179→180, cap/no backlog, fast-resolution threshold, survival increments, Stage 4 selection.

- [ ] **Step 6: Create PP-56 — Complete score presentation and score-change feedback**

Assignee `Patro`; dependency `PP-53`; present final score state and readable award changes without owning scoring logic; blockout feedback acceptable until M9 where applicable.

- [ ] **Step 7: Create PP-57 — Integrate and validate final difficulty and scoring progression**

Assignee `Team`; dependencies `PP-51` through `PP-56`; one-run stage progression, no deferred-spawn burst, correct score UI/runtime match, late-stage pressure readability.

- [ ] **Step 8: Commit**

```bash
git add Docs/archive/tickets/PP-51.md Docs/archive/tickets/PP-52.md Docs/archive/tickets/PP-53.md Docs/archive/tickets/PP-54.md Docs/archive/tickets/PP-55.md Docs/archive/tickets/PP-56.md Docs/archive/tickets/PP-57.md
git commit -m "docs(tickets): create Milestone 7 archive"
```

---

### Task 10: Create archived Milestone 8 tickets

**Files:**
- Create: `Docs/archive/tickets/PP-58.md` through `PP-65.md`

**Interfaces:**
- Consumes: M7 gate `PP-57` and accepted in-scene run-state contract.
- Produces: parked M8 menus/run-flow workstream tagged `m-8`.

- [ ] **Step 1: Create PP-58 — Implement final run-state transitions**

Assignee `Gabro`; dependency `PP-57`; `MainMenu`, `Playing`, `Paused`, `GameOver` transitions in one gameplay scene; recheck what M5 already established before adding redundant work.

- [ ] **Step 2: Create PP-59 — Implement pause and resume simulation and input behavior**

Assignee `Gabro`; dependency `PP-58`; define/suppress gameplay simulation and player input for Paused, restore on resume, tests for state transition/lifecycle.

- [ ] **Step 3: Create PP-60 — Complete final restart path**

Assignee `Gabro`; dependency `PP-58`; revalidate M5 restart implementation against final run states; implement only missing final-state integration.

- [ ] **Step 4: Create PP-61 — Create production-ready Main Menu**

Assignee `Patro`; dependency `PP-58`; final Main Menu presentation and Play action wiring/readability.

- [ ] **Step 5: Create PP-62 — Create production-ready Pause Menu**

Assignee `Patro`; dependency `PP-59`; final pause presentation and Resume action.

- [ ] **Step 6: Create PP-63 — Create production-ready Game Over screen**

Assignee `Patro`; dependency `PP-58`; final Game Over/final-run information and restart presentation.

- [ ] **Step 7: Create PP-64 — Implement HUD and menu visibility presentation**

Assignee `Patro`; dependencies `PP-58`, `PP-61`, `PP-62`, `PP-63`; state-specific visibility matching the accepted state matrix without gameplay ownership.

- [ ] **Step 8: Create PP-65 — Integrate and validate complete start-to-finish run flow**

Assignee `Team`; dependencies `PP-58` through `PP-64`; Main Menu → Playing → Paused → Playing → Game Over → Restart in one scene, UI/HUD state validation, no editor intervention.

- [ ] **Step 9: Commit**

```bash
git add Docs/archive/tickets/PP-58.md Docs/archive/tickets/PP-59.md Docs/archive/tickets/PP-60.md Docs/archive/tickets/PP-61.md Docs/archive/tickets/PP-62.md Docs/archive/tickets/PP-63.md Docs/archive/tickets/PP-64.md Docs/archive/tickets/PP-65.md
git commit -m "docs(tickets): create Milestone 8 archive"
```

---

### Task 11: Create archived Milestone 9 tickets

**Files:**
- Create: `Docs/archive/tickets/PP-66.md` through `PP-74.md`

**Interfaces:**
- Consumes: M8 gate `PP-65` and M9 feedback coverage/inventory contract.
- Produces: parked M9 feedback workstream tagged `m-9`.

- [ ] **Step 1: Create PP-66 — Implement gameplay-to-presentation event hooks**

Assignee `Gabro`; dependency `PP-65`; expose authoritative state changes required for feedback without moving gameplay rules into presentation; prevent duplicate subscriptions across restart/reload; add stable hook coverage where practical.

- [ ] **Step 2: Create PP-67 — Implement shared audio routing and AudioManager support**

Assignee `Gabro`; dependency `PP-65`; implement only shared/global warning/UI/game-state routing required by Runtime Design; local object sounds stay local where appropriate.

- [ ] **Step 3: Create PP-68 — Produce core pickup, brewing, and potion feedback package**

Assignee `Patro`; dependency `PP-65`; package may include audio/VFX/animation as appropriate for pickup, brewing, potion-ready/application, correct/wrong feedback; do not split by medium mechanically.

- [ ] **Step 4: Create PP-69 — Produce Mana Hotspot feedback package**

Assignee `Patro`; dependency `PP-65`; production-ready active/escalation/resolution feedback.

- [ ] **Step 5: Create PP-70 — Produce Slime Leak feedback package**

Assignee `Patro`; dependency `PP-65`; same event-coverage standard.

- [ ] **Step 6: Create PP-71 — Produce Hex Cloud feedback package**

Assignee `Patro`; dependency `PP-65`; same event-coverage standard.

- [ ] **Step 7: Create PP-72 — Produce Panic warning feedback package**

Assignee `Patro`; dependency `PP-65`; high-Panic urgency without masking critical gameplay cues or obscuring laboratory readability.

- [ ] **Step 8: Create PP-73 — Produce Game Over and essential UI feedback package**

Assignee `Patro`; dependency `PP-65`; Game Over and menu/UI confirmations where silence would make state unclear.

- [ ] **Step 9: Create PP-74 — Integrate and validate the complete feedback coverage matrix**

Assignee `Team`; dependencies `PP-66` through `PP-73`; verify every required feedback event, correct/wrong distinction, normal/escalated disaster states, simultaneous-disaster readability, high-Panic warning, audio balance, restart subscription safety.

- [ ] **Step 10: Commit**

```bash
git add Docs/archive/tickets/PP-66.md Docs/archive/tickets/PP-67.md Docs/archive/tickets/PP-68.md Docs/archive/tickets/PP-69.md Docs/archive/tickets/PP-70.md Docs/archive/tickets/PP-71.md Docs/archive/tickets/PP-72.md Docs/archive/tickets/PP-73.md Docs/archive/tickets/PP-74.md
git commit -m "docs(tickets): create Milestone 9 archive"
```

---

### Task 12: Create archived Milestone 10 tickets

**Files:**
- Create: `Docs/archive/tickets/PP-75.md`
- Create: `Docs/archive/tickets/PP-76.md`
- Create: `Docs/archive/tickets/PP-77.md`
- Create: `Docs/archive/tickets/PP-78.md`

**Interfaces:**
- Consumes: M9 gate `PP-74` and M10 exit-gate criteria.
- Produces: predictable release-readiness work only; no speculative defect backlog.

- [ ] **Step 1: Create PP-75 — Validate final MVP gameplay tuning and runtime stability**

Assignee `Gabro`; dependency `PP-74`; tags `m-10`, `testing`, `gameplay`.

Plan focus: reinspection, all three chains, wrong-potion behavior, difficulty/scoring against MVP Scope, restart stale-state checks, missing-reference/subscription issues, relevant automated suites. If findings are defects, create focused follow-up tickets during M10 execution rather than pre-populating them now.

- [ ] **Step 2: Create PP-76 — Audit and replace remaining release-path placeholders**

Assignee `Patro`; dependency `PP-74`; tags `m-10`, `content`, `polish`.

Plan focus: inventory remaining placeholders on release path, replace only unacceptable ones, preserve readability/references, production-ready review in gameplay context.

- [ ] **Step 3: Create PP-77 — Validate final late-game presentation and readability**

Assignee `Patro`; dependency `PP-74`; tags `m-10`, `ux`, `polish`.

Plan focus: Stage 4 pressure, simultaneous disasters, HUD/menu/readability, audio/VFX overlap, interaction target visibility; findings become focused tickets.

- [ ] **Step 4: Create PP-78 — Run final MVP release validation**

Assignee `Team`; dependencies `PP-75`, `PP-76`, `PP-77`; tags `m-10`, `integration`, `release`, `testing`.

Plan focus: target PC build, launch/run-state sequence, all three chains, wrong-potion path, multiple stages including Stage 4, score/Panic/UI/audio/VFX/readability, repeated restart, blocker tracking, no known gameplay-breaking bugs at closure.

- [ ] **Step 5: Confirm no speculative defect tickets were generated**

Expected generated upper bound: `PP-78` under the assumed ID map.

- [ ] **Step 6: Commit**

```bash
git add Docs/archive/tickets/PP-75.md Docs/archive/tickets/PP-76.md Docs/archive/tickets/PP-77.md Docs/archive/tickets/PP-78.md
git commit -m "docs(tickets): create Milestone 10 archive"
```

---

### Task 13: Verify the complete generated ticket set

**Files:**
- Verify: `Docs/tickets/`
- Verify: `Docs/archive/tickets/`
- Verify: `Docs/archive/index.md`
- Verify: `Docs/archive/tickets/index.md`
- Verify: `Docs/board.md`
- Verify: `Docs/archive/board.md`

**Interfaces:**
- Consumes: all generated ticket files.
- Produces: evidence that the ticket set is internally consistent and renders under the existing docs/Docboard stack.

- [ ] **Step 1: Verify retired IDs are absent and not reused**

Run:

```bash
test ! -e Docs/tickets/PP-2.md
test ! -e Docs/tickets/PP-3.md
test ! -e Docs/tickets/PP-4.md
```

Search generated frontmatter for accidental reuse:

```bash
grep -R '^id: [234]$' Docs/tickets Docs/archive/tickets
```

Expected: no newly generated `id: 2`, `id: 3`, or `id: 4`; historical archive files may contain other historical IDs but must not be new replacements.

- [ ] **Step 2: Verify active/future placement**

Run:

```bash
find Docs/tickets -maxdepth 1 -type f -name 'PP-1[0-3].md' -print | sort -V
find Docs/archive/tickets -maxdepth 1 -type f -name 'PP-*.md' -print | sort -V
```

Expected under the assumed range:

- active M1: PP-10 through PP-13;
- archived future: PP-14 through PP-78;
- historical archive records remain untouched.

- [ ] **Step 3: Verify future milestone tags**

For each M2-M10 range, confirm every generated ticket includes the matching `m-N` tag and none contains `future-milestone`.

Run at minimum:

```bash
grep -R 'future-milestone' Docs/tickets Docs/archive/tickets && exit 1 || true
```

Then inspect each milestone range with `grep -n 'm-[2-9]\|m-10'` and confirm exact membership.

- [ ] **Step 4: Verify assignee domain contract**

Check every generated ticket has exactly one of:

```text
assignee: Patro
assignee: Gabro
assignee: Team
```

Confirm every `Team` ticket is an integration/exit-gate ticket and contains dependencies on the required domain tickets.

- [ ] **Step 5: Verify checklist formatting**

Inspect every generated ticket and confirm:

- Acceptance Criteria contains `- [ ]` tasks;
- Implementation Plan contains `- [ ]` tasks;
- Definition of Done contains `- [ ]` tasks;
- nested independently completable cases use nested checkboxes;
- nested constraints use plain bullets;
- no future ticket has an empty or prose-only Implementation Plan;
- every future Implementation Plan begins with the repository/dependency reinspection step.

Do this as review/validation; do not add a prose-content snapshot test.

- [ ] **Step 6: Verify dependency references resolve**

Build a one-off local check or inspect programmatically so every `dependencies: PP-*` target exists either in active or archive ticket directories after generation. Do not commit the one-off checker unless it protects a structural board invariant independently of ticket wording.

- [ ] **Step 7: Verify no unsupported exact paths/API decisions were invented**

Review `affectedFiles` and Implementation Plans against current Runtime Design/repository layout. Replace speculative exact file names with stable directories/areas where needed.

- [ ] **Step 8: Run documentation tests**

Run:

```bash
npm test
```

Expected: all existing documentation/configuration tests pass.

- [ ] **Step 9: Build the docs**

Run:

```bash
npm run docs:build
```

Expected: VitePress build succeeds and Docboard parses all active/archive ticket frontmatter.

- [ ] **Step 10: Manually inspect both boards**

Run:

```bash
npm run docs:dev:local
```

Open the main board and archive board. Verify:

- M1 tickets render on the active board;
- M2-M10 tickets render on the archive board;
- assignees render as Patro/Gabro/Team;
- dependency lists render;
- nested task checkboxes render correctly;
- future milestone tags render;
- archive restore action remains available;
- historical archive tickets still render.

Stop the docs server after inspection.

- [ ] **Step 11: Compare branch scope against base**

Run:

```bash
git diff --stat master...HEAD
git diff --name-status master...HEAD
```

Expected implementation scope:

- three old M1 ticket deletions;
- four new active M1 ticket files;
- sixty-five new archived M2-M10 ticket files;
- two archive documentation updates;
- no gameplay/Unity asset changes;
- no Docboard source changes.

- [ ] **Step 12: Commit any verification-only corrections**

If review found formatting/dependency/content corrections, commit them separately:

```bash
git add Docs/tickets Docs/archive
git commit -m "docs(tickets): verify MVP ticket set"
```

If no corrections were required, do not create an empty commit.

## Final expected state

Assuming the starting integer ID remains `PP-10`:

- 69 new MVP tickets total;
- 4 active M1 tickets (`PP-10` through `PP-13`);
- 65 archived future tickets (`PP-14` through `PP-78`);
- `PP-2`, `PP-3`, `PP-4` deleted and never reused;
- every M2-M10 ticket tagged with its corresponding `m-N` tag;
- no `future-milestone` tag;
- Patro/Gabro/Team ownership visible on every ticket;
- Team tickets form milestone integration gates;
- complete checkbox-based Acceptance Criteria, Implementation Plan, and Definition of Done sections;
- archive documentation explicitly supports parked future-milestone tickets;
- existing docs tests and VitePress build pass;
- both active and archive boards render successfully.
