# MVP ticket generation design

## Purpose

Define how the accepted MVP milestone deliverables are converted into concrete
Docboard tickets for the two-person Potion Panic team.

This design covers ticket decomposition, assignee ownership, dependency gates,
board/archive placement, ticket field conventions, checklist formatting, and the
level of detail expected in implementation plans.

It does not create, delete, restore, or modify tickets. Ticket generation happens
only after a separate implementation plan is approved.

## Source contracts

Ticket generation must derive scope from the current evergreen project contracts:

- `Docs/project/mvp-scope.md` — locked MVP behavior, tuning, scope, and milestone
  order;
- `Docs/project/game-design.md` — player-facing design and presentation intent;
- `Docs/project/technical-architecture.md` — runtime responsibilities and
  implementation boundaries;
- `Docs/project/mvp-deliverables.md` — concrete milestone artifacts,
  verification expectations, and suggested ticket seeds;
- `Docs/collaboration/team-workflow.md` — task workflow and board usage;
- `Docs/board.md` and `Docs/archive/board.md` — Docboard ticket schema and board
  lifecycle.

When the repository state differs from an old ticket seed, current evergreen
contracts and implemented code/configuration take precedence over stale planning
text.

## Developer ownership

Use exactly these assignee names for MVP work:

### Patro

Owns primarily presentation-facing Unity work:

- scenes and scene layout;
- laboratory/world blockouts;
- cameras;
- models;
- materials;
- UI presentation;
- animation;
- VFX;
- audio assets;
- presentation-side prefab setup;
- visual/readability validation.

### Gabro

Owns primarily gameplay/runtime work:

- systems;
- controllers;
- gameplay state;
- gameplay logic;
- data contracts and ScriptableObject types;
- spawning;
- scoring;
- runtime state transitions;
- deterministic logic tests;
- runtime integration tests where the tested behavior belongs to a gameplay
  system.

### Team

Owns cross-domain integration and milestone exit gates.

A Team ticket should normally:

- depend on the required Patro and Gabro tickets;
- integrate already-produced domain deliverables;
- verify the milestone end to end;
- avoid absorbing substantial new Patro- or Gabro-domain implementation.

If integration reveals substantial missing work, create a focused Patro or Gabro
ticket instead of expanding the Team ticket indefinitely.

## Ticket decomposition

### Split by cohesive responsibility, not by individual artifact

Prefer single-domain tickets, but do not split trivial, tightly coupled work
merely to make tickets smaller.

Keep work together when it has:

- the same assignee;
- the same domain;
- the same implementation/review context;
- direct coupling;
- little value when completed independently.

Example:

- `Create Laboratory scene and laboratory blockout` is one Patro ticket because
  creating `Laboratory.unity` is a small prerequisite directly coupled to making
  the blockout.

Separate work when it has a meaningfully independent behavior, verification path,
or handoff boundary.

Example:

- `Configure fixed orthographic camera` remains separate from the laboratory
  blockout because camera behavior and framing have their own acceptance and
  validation criteria.

### Cross-domain outcomes

Do not create a ticket containing substantial Patro and Gabro implementation.
Instead:

1. create the Patro ticket(s);
2. create the Gabro ticket(s);
3. create a Team integration/verification ticket;
4. make the Team ticket depend on the domain tickets.

Typical milestone graph:

```text
Patro domain tickets ---\
                        > Team milestone integration / exit gate
Gabro domain tickets ---/
```

### Cross-milestone dependencies

Prefer depending on the preceding milestone's Team exit-gate ticket instead of
creating a dense dependency graph from every later ticket to every earlier domain
ticket.

Use direct cross-milestone dependencies only when a specific prerequisite matters
independently of the previous milestone's complete gate.

## Board lifecycle

### Milestone 1

Create Milestone 1 MVP tickets under the normal active board ticket directory:

- `Docs/tickets/`

Milestone 1 tickets do not receive an `m-1` tag under this design.

### Milestones 2-10

Create all predictable future MVP tickets up front, but place them on the archive
board under:

- `Docs/archive/tickets/`

They remain parked until their milestone becomes active.

Future archived tickets:

- use `status: backlog`;
- receive exactly their milestone tag, for example `m-2`, `m-3`, ..., `m-10`;
- do not receive a `future-milestone` tag;
- are restored through the existing archive-board restore workflow when needed.

When a milestone is activated:

1. restore all tickets belonging to that milestone;
2. keep blocked tickets in Backlog;
3. move dependency-free ready work to To Do according to the normal team
   workflow.

### Archive semantics

The archive documentation currently describes the archive as completed or
superseded work. Ticket generation must update that documentation so the archive
also explicitly supports parked future-milestone tickets.

The distinction is carried by ticket state and milestone tags:

- historical completed/superseded work remains historical archive material;
- future MVP tickets remain `backlog` and carry their `m-N` tag.

## Existing Milestone 1 tickets

The existing Milestone 1 backlog tickets `PP-2`, `PP-3`, and `PP-4` may be safely
deleted because their scope no longer matches the approved decomposition.

Do not reuse their IDs.

Ticket IDs remain globally historical identifiers even after a ticket file is
deleted. Reusing them would make Git history and older references ambiguous.

Before generating new tickets, scan both active and archived ticket records and
allocate IDs from the next globally unused `PP-*` number. Current repository
evidence suggests the next range begins after `PP-9`, but implementation must
verify rather than assume the starting ID.

## Ticket fields

Populate the normal Docboard frontmatter fields when supported by the current
schema:

- `id`;
- `title`;
- `status`;
- `priority`;
- `milestone` where the current board contract uses it;
- `dependencies` where applicable;
- `documentation`;
- `affectedFiles` or stable affected areas where sufficiently predictable;
- `tags`;
- `assignee`;
- `order`.

Do not use tags to duplicate assignee ownership.

### Tags

Use a small domain vocabulary where useful, such as:

- `gameplay`;
- `world`;
- `ui`;
- `content`;
- `audio`;
- `vfx`;
- `integration`;
- `testing`.

Only future archived MVP tickets receive milestone tags (`m-2` through `m-10`).
Do not duplicate milestone information with additional milestone tags unless the
approved rule above explicitly requires it.

### Priority

Priority represents importance/risk within the milestone, not chronological
proximity.

Do not mark future work low merely because it is archived. Archive placement
already represents that it is not currently active.

## Ticket section contract

Use the existing Docboard sections:

- Description;
- Acceptance Criteria;
- Implementation Plan;
- Implementation Notes;
- Definition of Done;
- Notes.

### Description

State the ticket outcome, its responsibility boundary, and any important scope
exclusions.

Avoid embedding a second implementation plan in prose.

### Acceptance Criteria

Acceptance Criteria always use Markdown task checkboxes.

They describe observable success, not implementation steps.

Example:

```md
## Acceptance Criteria

- [ ] WASD moves the player on world X/Z axes.
- [ ] Diagonal movement does not increase movement speed.
- [ ] CharacterController collision prevents leaving the intended room.
```

Use nested checkboxes when a criterion contains independently checkable cases.

### Implementation Plan

Implementation Plan always uses Markdown task checkboxes.

Plans should be as complete and concrete as current evidence permits.

Prefer:

- meaningful top-level implementation steps;
- nested checkboxes for independently completable subtasks;
- nested normal bullets for constraints or clarifications that are not separate
  work;
- short imperative wording;
- explicit test and verification steps;
- concrete existing class/system/scene names where established by project
  contracts.

Avoid:

- inventing APIs or method names not yet defined;
- speculative exact prefab hierarchies;
- speculative animation/shader/VFX techniques;
- exact file paths that cannot be justified from the current repository;
- verbose prose where hierarchy communicates the same information better.

Target style:

```md
## Implementation Plan

- [ ] Inspect the current player, interaction, and item-data implementation before editing.
- [ ] Add focused EditMode coverage for:
  - [ ] empty inventory
  - [ ] ingredient pickup
  - [ ] potion pickup
  - [ ] item replacement
  - [ ] consumption
  - [ ] invalid transitions
- [ ] Implement `PlayerInventory`
  - authoritative owner of the player's single carried-item state.
- [ ] Enforce the MVP rule: player can carry either one ingredient or one potion, never both.
- [ ] Add inventory operations for:
  - [ ] acquiring an item
  - [ ] replacing an ingredient with its brewed potion
  - [ ] consuming the carried item
  - [ ] clearing state
- [ ] Expose inventory state changes so UI/presentation can observe them without owning gameplay state.
- [ ] Wire `PlayerInventory` into the player runtime setup
  - without introducing station-specific logic into the inventory component.
- [ ] Run the focused EditMode tests.
- [ ] Run the relevant PlayMode/manual inventory smoke test.
- [ ] Review the Unity Console and changed serialized references before handoff.
```

#### Future archived ticket plans

Milestones 2-10 still receive detailed implementation plans now.

Start each future plan with a repository/dependency reinspection step, for
example:

```md
- [ ] Reinspect the current repository and completed dependency-ticket results after this milestone is restored; adjust stale plan details without changing accepted ticket scope.
```

Then include the most concrete expected implementation steps currently supported
by the project contracts.

A future plan may be refined after restore if repository reality changed, but its
accepted scope must not silently change.

If a detail is genuinely ambiguous:

- use a moderately specific outcome-oriented step;
- use a deliberately vague step only where necessary;
- omit a speculative step entirely when adding it would imply an unsupported
  technical decision.

### Implementation Notes

For newly generated tickets, keep Implementation Notes minimal and factual.

Do not pre-write implementation history.

For future archived tickets, note that the ticket was generated as planned future
MVP work and has not started.

### Definition of Done

Definition of Done always uses Markdown task checkboxes.

It describes completion evidence, not the implementation sequence.

Typical evidence includes:

- acceptance criteria met;
- relevant automated tests pass;
- required manual validation completed;
- no new relevant Unity Console errors/warnings;
- serialized scene/prefab changes reviewed where applicable;
- documentation updated if an owned contract changed;
- handoff/review readiness recorded.

Use nested checkboxes where grouped evidence is clearer.

### Notes

Use Notes only for stable caveats that do not belong in the other sections.
Do not manufacture temporary blockers or speculative notes.

## Milestone ticket inventory

The following inventory is the target decomposition. Exact titles may be refined
for consistency during generation without changing scope or ownership.

### Milestone 1 — active board

Patro:

- Create Laboratory scene and laboratory blockout.
- Configure fixed orthographic camera.

Gabro:

- Implement player movement and CharacterController setup.

Team:

- Integrate and validate Milestone 1.

The Team ticket depends on all three domain tickets.

### Milestone 2 — archived, tag `m-2`

Gabro:

- Implement reusable `IInteractable` contract and interaction targeting.

Patro:

- Create interaction prompt UI.

Team:

- Integrate and validate reusable interaction flow.

### Milestone 3 — archived, tag `m-3`

Gabro:

- Implement ingredient and potion data contracts.
- Implement `PlayerInventory`.
- Implement `IngredientStation`.
- Implement `BrewingStation`.

Patro:

- Create first ingredient and potion presentation set.
- Create ingredient and brewing station blockouts.
- Create carried-item HUD.

Team:

- Integrate and validate the ingredient-to-potion loop.

### Milestone 4 — archived, tag `m-4`

Gabro:

- Implement disaster data contract and `DisasterInstance`.
- Implement `PanicSystem`.
- Implement potion application and disaster resolution rules.

Patro:

- Create Mana Hotspot presentation and escalation states.
- Create Panic meter UI.
- Create disaster resolution and wrong-potion feedback blockout.

Team:

- Integrate and validate the first disaster loop.

### Milestone 5 — archived, tag `m-5`

Gabro:

- Implement run-state coordination foundation.
- Implement disaster spawning and spawn-point logic.
- Implement initial `ScoreSystem`.
- Implement Game Over runtime behavior.
- Implement clean restart/reset behavior.

Patro:

- Place and validate disaster spawn locations.
- Create basic score HUD.
- Create Game Over and restart UI.

Team:

- Integrate and validate the repeatable core game loop.

### Milestone 6 — archived, tag `m-6`

Gabro:

- Add remaining ingredient and potion data.
- Add Slime Leak gameplay/data configuration.
- Add Hex Cloud gameplay/data configuration.
- Validate complete MVP content data relationships.

Patro:

- Create production-ready ingredient presentation set.
- Create production-ready potion presentation set.
- Create Slime Leak presentation.
- Create Hex Cloud presentation.
- Complete production-ready disaster presentation pass.

Team:

- Integrate and validate all three MVP content chains.

### Milestone 7 — archived, tag `m-7`

Gabro:

- Implement time-based difficulty stages.
- Implement difficulty spawning rules:
  - active-disaster caps;
  - skipped spawns at cap;
  - no backlog;
  - equal disaster selection.
- Implement final scoring rules.
- Implement Stage 4 disaster tuning.
- Add difficulty/scoring boundary coverage.

Patro:

- Complete score presentation and score-change feedback.

Team:

- Integrate and validate final difficulty/scoring progression.

### Milestone 8 — archived, tag `m-8`

Gabro:

- Implement final run-state transitions.
- Implement pause/resume simulation and input behavior.
- Complete final restart path where still required after Milestone 5.

Patro:

- Create production-ready Main Menu.
- Create production-ready Pause Menu.
- Create production-ready Game Over screen.
- Implement HUD/menu visibility presentation.

Team:

- Integrate and validate complete start-to-finish run flow.

Future ticket plans must recheck whether earlier milestones already satisfied part
of the expected work before adding redundant implementation.

### Milestone 9 — archived, tag `m-9`

Gabro:

- Implement gameplay-to-presentation event hooks.
- Implement shared audio routing / `AudioManager` support required by the final
  feedback design.

Patro:

- Produce core pickup/brewing/potion feedback package.
- Produce Mana Hotspot feedback package.
- Produce Slime Leak feedback package.
- Produce Hex Cloud feedback package.
- Produce Panic warning feedback package.
- Produce Game Over and essential UI feedback package.

A feedback package may contain the relevant audio, VFX, and animation. Do not
split by asset medium unless production scope later demonstrates that the work is
independently substantial.

Team:

- Integrate and validate the complete feedback coverage matrix.

### Milestone 10 — archived, tag `m-10`

Do not pre-create speculative bug tickets.

Gabro:

- Validate final MVP gameplay tuning and runtime stability.

Patro:

- Audit and replace remaining release-path placeholders.
- Validate final late-game presentation/readability.

Team:

- Run final MVP release validation.

Concrete defects discovered by Milestone 10 validation generate focused new
Patro, Gabro, or Team tickets according to the same ownership rules.

## Verification of generated ticket set

Before declaring ticket generation complete, verify at least:

- `PP-2`, `PP-3`, and `PP-4` are removed and their IDs are not reused;
- every new MVP ticket has exactly one assignee: Patro, Gabro, or Team;
- substantial cross-domain outcomes use Team integration tickets rather than
  mixed implementation tickets;
- every Team gate has its required domain dependencies;
- Milestone 1 tickets are under `Docs/tickets/`;
- Milestones 2-10 tickets are under `Docs/archive/tickets/`;
- future tickets are `backlog`;
- every future ticket has exactly its `m-N` milestone tag as required by this
  design;
- no future ticket has a `future-milestone` tag;
- Acceptance Criteria use checkboxes;
- Implementation Plan uses hierarchical checkboxes and contains concrete steps
  where evidence permits;
- Definition of Done uses checkboxes;
- future implementation plans begin with repository/dependency reinspection;
- dependencies reference valid ticket IDs after ID allocation;
- documentation and affected areas do not claim unsupported exact paths;
- archive documentation acknowledges parked future-milestone tickets;
- Docboard can render both active and archive boards with the generated files.

## Non-goals

This ticket-generation work does not:

- implement any gameplay feature;
- implement any presentation asset;
- start future milestones;
- change locked MVP scope or tuning;
- add multi-assignee support to Docboard;
- use synthetic per-developer tags;
- create speculative Milestone 10 bug tickets;
- add content tests that enforce exact prose wording.
