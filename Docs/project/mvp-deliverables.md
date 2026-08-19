# MVP deliverables

Use this document to translate the accepted MVP scope and runtime design into
concrete milestone artifacts that can be decomposed into implementation tickets.

[MVP Scope](mvp-scope.md) owns locked behavior, tuning, scope, and milestone
order. [Game Design](game-design.md) owns player-facing intent and content
identity. [Runtime Design](technical-architecture.md) owns runtime responsibilities
and implementation boundaries. This page owns the concrete artifacts and
integration evidence required to call each milestone delivered.

## Status and authority

Everything listed here is an **accepted MVP target**, not evidence that the
corresponding script, asset, prefab, scene wiring, UI, test, or feedback already
exists in the repository. Use the board and tickets for current implementation
status.

This document owns:

- required milestone artifacts;
- responsibility-area routing;
- asset maturity expectations;
- milestone integration outcomes;
- milestone verification evidence;
- suggested ticket boundaries.

It does not own:

- locked gameplay rules or tuning;
- player-facing design intent;
- runtime responsibility definitions;
- task status or assignees;
- exact affected files for a ticket;
- task-specific acceptance criteria;
- branch names or implementation history.

## Responsibility areas

The existing team split is used to route work without assigning a specific
person inside this document.

| Responsibility area | Typical deliverables |
|---|---|
| Gameplay / systems | C# runtime behavior, ScriptableObject types and data, system rules, technical tests. |
| World / UX / presentation | Scene and layout work, models, materials, UI, menus, audio, VFX, animation, presentation assets. |
| Shared integration | Prefab and scene wiring, serialized references, end-to-end behavior, milestone verification. |

A ticket may cross responsibility areas when the work is small and independently
reviewable. Do not combine unrelated work merely because it belongs to the same
milestone.

## Asset maturity

Use these maturity labels where presentation work appears before final polish:

- **Blockout acceptable:** a deliberately simple placeholder is sufficient to
  prove scale, placement, readability, interaction, or integration.
- **Production-ready required:** the asset must be suitable for the MVP release
  and meet the accepted art, readability, and feedback intent.

A blockout is intentional temporary content. It should still be readable enough
to validate the milestone and should not hide missing gameplay behavior.

## Verification evidence

Use the evidence type that matches the changed behavior:

- **EditMode test:** deterministic or pure logic that can be verified without
  running the gameplay scene.
- **PlayMode test:** runtime behavior, scene integration, lifecycle behavior, or
  interactions that benefit from automated Unity runtime coverage.
- **Manual validation:** visual, audio, feel, layout, readability, or an
  end-to-end behavior that must be observed directly.

Not every deliverable needs all three. A milestone is complete when the relevant
proof exists, not when a test category is filled mechanically.

## Ticket decomposition rules

The deliverables below are inputs to ticket creation, not a second task board.

1. Create one ticket when a deliverable can be implemented, verified, and
   reviewed coherently by itself.
2. Split work when gameplay logic and presentation production can progress
   independently or have different verification needs.
3. Keep exact affected files, dependencies, assignees, task-specific acceptance
   criteria, status, and implementation notes in the ticket.
4. Do not create one ticket per bullet mechanically.
5. Keep a milestone integration or exit-gate ticket when independently produced
   artifacts must be proven together.
6. Preserve milestone order from [MVP Scope](mvp-scope.md); this document does
   not authorize later work early.

## Delivery overview

| Milestone | Main gameplay / systems result | Main world / presentation result | Exit condition |
|---|---|---|---|
| 1 | Player movement and collision | Laboratory blockout and fixed camera | The player can traverse the intended room reliably. |
| 2 | Reusable interaction framework | Interaction prompt | One generic interaction path works end to end. |
| 3 | Inventory and brewing | First ingredient, potion, stations, carried-item UI | The first ingredient-to-potion loop works. |
| 4 | Disaster and Panic behavior | First disaster presentation and Panic UI | The first disaster can escalate, fail, and resolve correctly. |
| 5 | Run management and spawning | Game Over and basic score presentation | A repeatable run reaches failure and restarts cleanly. |
| 6 | Proven systems support all MVP content | Complete three-chain content set | All three ingredient-potion-disaster chains work. |
| 7 | Difficulty progression and final scoring rules | Score feedback | Locked progression and scoring operate at their boundaries. |
| 8 | Complete run-state transitions | Main, pause, and Game Over menus | The game has a complete start-to-finish run flow. |
| 9 | Feedback hooks are complete | Required audio, VFX, and animation assets | Every required gameplay event has readable feedback. |
| 10 | Stability and balance fixes | Final presentation pass | MVP release criteria are satisfied. |

# Milestone deliverables

## Milestone 1: movement and camera

### Outcome

Establish a playable laboratory foundation in which the player can move
responsively through the intended room while the fixed top-down camera provides
full readable coverage.

### Gameplay / systems

**Responsibility:** Gameplay / systems

- `PlayerController` implementing the accepted WASD movement behavior;
- configured `CharacterController` on the player;
- world-aligned X/Z movement;
- normalized diagonal movement;
- player/world collision behavior;
- movement configuration required by the accepted Milestone 1 rules.

### World / UX / presentation

**Responsibility:** World / UX / presentation

- player visual: **Blockout acceptable**;
- laboratory floor and room footprint: **Blockout acceptable**;
- walls, blockers, or equivalent boundary geometry: **Blockout acceptable**;
- collision geometry matching the playable room;
- fixed orthographic top-down camera framing the intended playable area.

Final environment decoration, materials, prop dressing, and character art are
not required for this milestone.

### Shared integration

- `Laboratory.unity` replaces or supersedes the shared prototype gameplay scene
  as specified by MVP Scope;
- Player exists in the gameplay scene with movement and collision configured;
- camera framing and playable boundaries agree with the laboratory blockout;
- the player can reach all intended Milestone 1 traversable space and cannot
  leave the intended room.

### Verification

**PlayMode test, where practical:**

- movement input produces movement on the intended axes;
- diagonal input does not increase movement speed.

**Manual validation:**

- WASD movement is responsive;
- the camera remains fixed;
- the whole intended room is readable from the camera;
- collision contains the player;
- no intended traversable area is unreachable because of the blockout.

### Suggested ticket boundaries

- implement player movement and collision;
- create the laboratory blockout and fixed camera;
- integrate and verify the Milestone 1 gameplay scene.

## Milestone 2: interaction system

### Outcome

Provide one reusable interaction path that later stations and disasters can use
without object-specific behavior inside the player's interaction controller.

### Gameplay / systems

**Responsibility:** Gameplay / systems

- `IInteractable` abstraction;
- `InteractionController`;
- Interact input handling;
- nearby interactable detection;
- deterministic current-target selection appropriate to the MVP;
- interaction prompt state exposed to presentation code.

### World / UX / presentation

**Responsibility:** World / UX / presentation

- interaction prompt UI: **Blockout acceptable**;
- prompt text or input indication sufficient to communicate when interaction is
  available.

### Shared integration

- Player owns or references the configured interaction controller;
- at least one temporary interactable proves the generic abstraction;
- prompt visibility follows the selected interaction target;
- leaving interaction range removes the target and prompt correctly.

### Verification

**EditMode test, where the selection logic is isolated:**

- target selection follows the accepted deterministic rule;
- no valid target produces no interaction action.

**PlayMode test:**

- entering and leaving range changes the available target correctly;
- Interact invokes only the selected `IInteractable`.

**Manual validation:**

- the prompt appears and disappears predictably;
- the interaction flow is readable from the fixed camera.

### Suggested ticket boundaries

- implement reusable interactable abstraction and targeting;
- implement interaction prompt;
- integrate and verify the reusable interaction path.

## Milestone 3: ingredient-to-potion loop

### Outcome

Complete the first non-disaster gameplay loop: collect one ingredient, carry it,
brew it, and receive the matching potion while the HUD reflects the carried
state.

### Gameplay / systems

**Responsibility:** Gameplay / systems

- `IngredientData`;
- `PotionData`;
- `PlayerInventory`;
- `IngredientStation`;
- `BrewingStation`;
- one-item inventory enforcement;
- ingredient-to-resulting-potion conversion through configured data.

### Data / configuration

**Responsibility:** Gameplay / systems

- Blue Mushroom data;
- Cooling Potion data;
- Blue Mushroom -> Cooling Potion relationship;
- UI/visual metadata required by the implemented data contract.

### World / UX / presentation

**Responsibility:** World / UX / presentation

- ingredient station representation: **Blockout acceptable**;
- brewing station representation: **Blockout acceptable**;
- Blue Mushroom representation: **Blockout acceptable**;
- Cooling Potion carried/world representation: **Blockout acceptable**;
- carried-item HUD with empty, ingredient, and potion states:
  **Blockout acceptable**.

### Shared integration

The complete path must work:

```text
empty inventory
  -> collect Blue Mushroom
  -> carry Blue Mushroom
  -> interact with Brewing Station
  -> consume Blue Mushroom
  -> receive Cooling Potion
  -> carried-item HUD shows Cooling Potion
```

The integration must also preserve rejection behavior:

- ingredient pickup is rejected while already carrying an item;
- brewing is rejected when no ingredient is carried;
- the inventory never contains both an ingredient and potion simultaneously.

### Verification

**EditMode test:**

- one-item inventory rules;
- Blue Mushroom resolves to Cooling Potion through configured data;
- invalid inventory transitions are rejected.

**PlayMode test:**

- station interaction updates runtime inventory correctly;
- brewing consumes the ingredient and grants its potion;
- carried-item UI reacts to inventory changes if suitable for automated coverage.

**Manual validation:**

- the full ingredient-to-potion loop is understandable without inspector
  intervention;
- carried-item feedback is readable during normal movement.

### Suggested ticket boundaries

- implement item data and `PlayerInventory`;
- implement `IngredientStation`;
- implement `BrewingStation`;
- create first ingredient, potion, and station presentation assets;
- implement carried-item HUD;
- integrate and verify the complete Milestone 3 loop.

## Milestone 4: first disaster

### Outcome

Make the game technically playable against one complete disaster using the same
interaction, inventory, brewing, Panic, and resolution path intended for the
remaining MVP disasters.

### Gameplay / systems

**Responsibility:** Gameplay / systems

- `DisasterData`;
- `DisasterInstance`;
- initial `PanicSystem`;
- potion application through the reusable interaction path;
- disaster age and escalation state;
- base and escalated Panic generation;
- correct-potion validation and resolution;
- correct-resolution Panic reduction;
- generic wrong-potion behavior from MVP Scope.

### Data / configuration

**Responsibility:** Gameplay / systems

- Mana Hotspot disaster data;
- Cooling Potion configured as its required solution;
- Panic and escalation values sourced from the locked rules in
  [MVP Scope](mvp-scope.md).

### World / UX / presentation

**Responsibility:** World / UX / presentation

- Mana Hotspot prefab or equivalent runtime representation:
  **Blockout acceptable**;
- readable normal state: **Blockout acceptable**;
- readable escalated state: **Blockout acceptable**;
- correct-resolution feedback: **Blockout acceptable**;
- wrong-potion/failure feedback sufficient to communicate cause:
  **Blockout acceptable**;
- functional Panic meter: **Blockout acceptable**.

The presentation should follow the current Mana Hotspot identity in
[Game Design](game-design.md), not the superseded infrastructure-specific
cauldron concept.

### Shared integration

- a Mana Hotspot can exist in `Laboratory.unity` and receive the Cooling Potion
  through the common interaction path;
- Panic rises while the disaster is active;
- escalation changes the configured Panic pressure and presentation state;
- correct resolution stops the disaster and applies the accepted Panic result;
- wrong-potion use consumes the potion, leaves the disaster active, applies the
  accepted penalty, and grants no score.

### Verification

**EditMode test:**

- base/escalated state transition logic;
- correct versus incorrect potion validation;
- accepted Panic changes for resolution and wrong-potion use.

**PlayMode test:**

- active disaster contributes Panic over runtime time;
- escalation changes runtime behavior;
- correct and incorrect potion application integrate with inventory and Panic.

**Manual validation:**

- normal, escalated, success, and wrong-potion states are distinguishable;
- Panic feedback makes the consequence of leaving the disaster active clear.

### Suggested ticket boundaries

- implement disaster data and `DisasterInstance`;
- implement initial `PanicSystem`;
- create Mana Hotspot presentation and escalation states;
- implement Panic meter;
- integrate and verify the first complete disaster loop.

## Milestone 5: core game loop

### Outcome

Turn the vertical slice into a repeatable run that starts, spawns disasters,
reaches Game Over, shows score, and restarts cleanly.

### Gameplay / systems

**Responsibility:** Gameplay / systems

- `GameManager` with the accepted run-state ownership needed so far;
- `DisasterManager`;
- `DisasterSpawnPoint`;
- active-disaster tracking;
- initial run setup;
- accepted first-disaster delay;
- recurring disaster spawning;
- valid-spawn selection and occupancy handling;
- operational `PanicSystem` Game Over trigger;
- initial `ScoreSystem` sufficient for the basic score display;
- restart/reload flow with clean state.

### Data / configuration

**Responsibility:** Gameplay / systems

- valid disaster spawn-point configuration in the gameplay scene;
- run and spawn values sourced from [MVP Scope](mvp-scope.md).

### World / UX / presentation

**Responsibility:** World / UX / presentation

- clearly placed valid disaster spawn locations in the laboratory blockout;
- basic score display: **Blockout acceptable**;
- Game Over presentation: **Blockout acceptable**;
- restart control: **Blockout acceptable**.

### Shared integration

The complete vertical slice must work without manual state repair:

```text
start run
  -> first disaster appears after the accepted delay
  -> disaster can be resolved
  -> recurring spawning continues
  -> Panic can reach 100
  -> Game Over
  -> restart
  -> clean new run
```

Restart must clear or replace transient run state so resolved/active disasters,
Panic, score, timers, and occupancy do not leak into the new run.

### Verification

**EditMode test:**

- run-state transition rules where isolated;
- active-disaster tracking and spawn-cap logic introduced by this milestone;
- score reset and other pure reset rules.

**PlayMode test:**

- run initialization and first spawn timing;
- Game Over at 100 Panic;
- restart establishes a clean run;
- spawn points do not remain falsely occupied after resolution/restart.

**Manual validation:**

- complete start-to-failure-to-restart loop works repeatedly;
- score and Game Over state are readable.

### Suggested ticket boundaries

- implement run-state coordination;
- implement disaster spawning and spawn points;
- connect Panic to Game Over;
- implement initial score display and score system;
- implement restart/reset flow;
- integrate and verify the repeatable vertical slice.

## Milestone 6: full MVP content

### Outcome

Extend the proven ingredient, potion, and disaster systems to the complete MVP
content set without creating parallel gameplay frameworks.

### Gameplay / systems

**Responsibility:** Gameplay / systems

- existing inventory, brewing, interaction, disaster, Panic, and spawning
  systems support all three content chains through data/configuration;
- no disaster requires a separate one-off framework to provide its basic MVP
  behavior.

### Data / configuration

**Responsibility:** Gameplay / systems

Required target data assets:

- three `IngredientData` assets;
- three `PotionData` assets;
- three `DisasterData` assets.

Required relationships:

| Ingredient | Potion | Disaster |
|---|---|---|
| Blue Mushroom | Cooling Potion | Mana Hotspot |
| Green Slime | Slime Dissolver | Slime Leak |
| Purple Crystal Dust | Purification Potion | Hex Cloud |

Each disaster must reference its correct potion and the accepted tuning source in
[MVP Scope](mvp-scope.md).

### World / UX / presentation

**Responsibility:** World / UX / presentation

For each ingredient:

- recognizable model/world representation: **Production-ready required by the
  end of Milestone 6**;
- material/visual treatment supporting its design identity;
- UI/icon representation where the carried-item UI requires one.

For each potion:

- recognizable carried/world representation: **Production-ready required by the
  end of Milestone 6**;
- material/visual treatment matching the accepted color relationship;
- UI/icon representation where required.

For each disaster:

- runtime prefab/representation: **Production-ready required by the end of
  Milestone 6**;
- readable normal state;
- readable escalation state;
- correct-resolution presentation;
- warning/icon representation where required;
- compatibility with valid disaster spawn locations.

Production-ready here means suitable for the small MVP target. It does not
require final Milestone 9 audio/VFX polish to be finished early.

### Shared integration

Each content chain must work independently through the same loop:

```text
Blue Mushroom -> Cooling Potion -> Mana Hotspot
Green Slime -> Slime Dissolver -> Slime Leak
Purple Crystal Dust -> Purification Potion -> Hex Cloud
```

The post-MVP disaster-specific wrong-potion reactions and topology-affecting
escalation described in Game Design are explicitly excluded from this milestone.
The generic MVP wrong-potion rule remains the required behavior.

### Verification

**EditMode test:**

- every ingredient references the intended potion;
- every disaster references the intended required potion;
- required data assets satisfy the runtime contract.

**PlayMode test:**

- all three solution chains can be completed through the same runtime systems;
- each disaster can spawn and resolve through valid spawn-point integration.

**Manual validation:**

- ingredients, potions, and disasters are distinguishable from the fixed camera;
- the three disaster forms preserve the accepted Mana Hotspot, Slime Leak, and
  Hex Cloud identities across valid spawn locations.

### Suggested ticket boundaries

- add Green Slime and Slime Dissolver content/data;
- add Purple Crystal Dust and Purification Potion content/data;
- add Slime Leak content/data/presentation;
- add Hex Cloud content/data/presentation;
- production-ready pass for the complete ingredient/potion set;
- integrate and verify all three MVP content chains.

## Milestone 7: difficulty and scoring

### Outcome

Apply the locked time-based difficulty progression and complete the MVP scoring
rules so pressure and score increase predictably across the full run.

### Gameplay / systems

**Responsibility:** Gameplay / systems

- elapsed run-time tracking;
- four accepted difficulty stages;
- stage-specific spawn intervals;
- stage-specific active-disaster caps;
- skipped spawn when the cap is full;
- no queued spawn backlog;
- equal weighting across enabled disaster types;
- Stage 4 disaster tuning changes;
- normal resolution score;
- fast-resolution bonus;
- survival score.

All values and boundary rules come from [MVP Scope](mvp-scope.md); do not make
this page a second tuning authority.

### World / UX / presentation

**Responsibility:** World / UX / presentation

- final score display behavior integrated with the completed `ScoreSystem`;
- readable score-change feedback where needed to understand awards:
  **Blockout acceptable until Milestone 9**.

### Shared integration

- difficulty stage changes alter spawning and Stage 4 pressure without resetting
  the current run;
- score continues correctly across resolves, speed bonuses, and survival time;
- simultaneous difficulty and scoring updates do not produce duplicate awards or
  spawn backlog.

### Verification

**EditMode test:**

- 59s -> 60s stage boundary;
- 119s -> 120s stage boundary;
- 179s -> 180s stage boundary;
- active-disaster cap enforcement;
- full-cap spawn is skipped rather than queued;
- equal disaster selection weighting configuration/selection behavior;
- fast-resolution bonus boundary;
- survival score increments once per full second;
- Stage 4 tuning selection.

**PlayMode test:**

- stage changes affect live spawning during one run;
- full-cap periods do not produce a burst of deferred spawns later;
- score shown by the UI matches the runtime score.

**Manual validation:**

- pressure rises perceptibly across stages without making cause unreadable;
- score feedback is understandable during a normal run.

### Suggested ticket boundaries

- implement time-based difficulty stages;
- implement cap/no-backlog spawning behavior and disaster selection;
- complete score rules and boundary tests;
- integrate Stage 4 disaster tuning;
- integrate and verify final difficulty/scoring behavior.

## Milestone 8: menus and run flow

### Outcome

Wrap the proven gameplay loop in the complete accepted in-scene run-state flow
from Main Menu to Playing, Paused, Game Over, and restart.

### Gameplay / systems

**Responsibility:** Gameplay / systems

- accepted `MainMenu`, `Playing`, `Paused`, and `GameOver` states;
- transitions between those states;
- Play/start-run behavior;
- pause and resume behavior;
- gameplay input and simulation suppression appropriate to each state;
- restart path from the final run flow.

### World / UX / presentation

**Responsibility:** World / UX / presentation

- main menu: **Production-ready required**;
- Play action;
- pause menu: **Production-ready required**;
- Resume action;
- Game Over presentation: **Production-ready required**;
- Restart action;
- HUD visibility rules for each state.

### Shared integration

Use this state contract as the integration target:

| State | Player movement | Gameplay simulation | HUD | Menu |
|---|---|---|---|---|
| MainMenu | Off | Off | Off | Main menu |
| Playing | On | On | On | Off |
| Paused | Off | Paused | Only information intentionally retained | Pause menu |
| GameOver | Off | Off | Final run information as intended | Game Over |

The accepted implementation remains one gameplay scene; these states must not
silently become separate scene flows.

### Verification

**EditMode test:**

- allowed run-state transitions and reset rules where isolated.

**PlayMode test:**

- starting from Main Menu initializes a run;
- pause suppresses the intended gameplay behavior and resume restores it;
- Game Over suppresses active gameplay;
- restart returns to a clean playable state.

**Manual validation:**

- menus are readable and operable at the target resolution/input setup;
- HUD/menu visibility matches the current state;
- the complete run can be navigated without editor intervention.

### Suggested ticket boundaries

- complete run-state transitions;
- build main and pause menus;
- production-ready Game Over/restart presentation;
- integrate HUD visibility with run states;
- verify complete start-to-finish flow.

## Milestone 9: audio and visual feedback

### Outcome

Give every major gameplay state change enough audiovisual feedback that the
player can identify events, causes, escalation, success, and failure during a
pressured run.

### Gameplay / systems

**Responsibility:** Gameplay / systems

- presentation hooks/events required by the final feedback assets;
- shared warning/UI/game-state audio routed through `AudioManager` where that
  matches Runtime Design;
- local object feedback remains local to the relevant prefab when appropriate;
- no gameplay rule is moved into presentation code merely to trigger an effect.

### Required feedback coverage

**Responsibility:** World / UX / presentation

The MVP must provide readable feedback for at least:

- ingredient pickup;
- brewing interaction;
- potion completion/availability;
- correct potion application;
- wrong potion application;
- Mana Hotspot normal activity and escalation;
- Slime Leak normal activity and escalation;
- Hex Cloud normal activity and escalation;
- each disaster resolving;
- Panic entering a high-risk/warning state;
- Game Over;
- menu/UI actions where silence would make state change unclear.

Each event does not need a unique sound, particle system, and animation if one or
two channels communicate it clearly. The requirement is readable coverage, not
mechanical asset-count inflation.

### Expected asset inventory

**Responsibility:** World / UX / presentation

The production inventory should include, where the chosen presentation uses that
medium:

**Audio**

- ingredient pickup sound;
- brewing/mixing sound;
- potion-ready or brewing-complete cue;
- correct-resolution cue;
- wrong-potion/failure cue;
- Mana Hotspot active loop or recurring cue;
- Mana Hotspot escalation cue;
- Slime Leak active loop or recurring cue;
- Slime Leak escalation cue;
- Hex Cloud active loop or recurring cue;
- Hex Cloud escalation cue;
- high-Panic warning/alarm;
- Game Over cue;
- essential menu/UI confirmation cues.

**VFX / visual feedback**

- ingredient pickup feedback;
- brewing feedback;
- potion application feedback;
- correct-resolution feedback;
- wrong-potion feedback;
- Mana Hotspot normal and escalation effects;
- Slime Leak normal and escalation effects;
- Hex Cloud normal and escalation effects;
- high-Panic visual warning treatment where used.

**Animation**

- player movement animation if the final player representation is animated;
- brewing/station animation if the chosen station design uses animation;
- disaster motion/state animation required by the accepted visual identity;
- UI transitions only where they improve state readability.

Do not create animation merely to satisfy the inventory. If the final asset is
intentionally static and communicates correctly through another medium, no
placeholder animation is required.

All Milestone 9 assets used in the release path are **Production-ready required**.

### Shared integration

- audiovisual feedback triggers from authoritative gameplay state changes;
- repeated/overlapping disasters remain distinguishable and do not create an
  unreadable wall of identical feedback;
- escalation is perceptibly different from the normal state;
- correct and wrong-potion outcomes cannot be confused;
- high-Panic warning communicates urgency without obscuring the laboratory.

### Verification

**PlayMode test, where stable hooks can be asserted without testing presentation
prose or exact timing:**

- authoritative gameplay events invoke the intended presentation hooks once;
- feedback subscriptions do not duplicate after restart/reload cycles.

**Manual validation:**

- verify every item under Required feedback coverage;
- verify simultaneous disasters remain readable at late-game pressure;
- verify audio levels do not hide critical warnings or dominate all other cues;
- verify VFX and animation do not obscure interactable/disaster silhouettes;
- verify success, failure, escalation, and Game Over are immediately
  distinguishable.

### Suggested ticket boundaries

- implement gameplay-to-presentation hooks and shared audio routing;
- produce core interaction/brewing/potion feedback assets;
- produce Mana Hotspot feedback set;
- produce Slime Leak feedback set;
- produce Hex Cloud feedback set;
- produce Panic/Game Over/UI feedback set;
- integrate and verify the complete feedback coverage matrix.

## Milestone 10: polish, balancing, and bug fixing

### Outcome

Close verified defects, replace unacceptable temporary content, validate the
locked balance, and satisfy the game-level MVP completion criteria. This
milestone is primarily an exit gate, not a prediction of every polish ticket.

### Gameplay / systems exit criteria

**Responsibility:** Gameplay / systems

- no known gameplay-breaking defect blocks a complete run;
- all three solution chains work with the locked wrong-potion behavior;
- all difficulty and scoring rules match MVP Scope;
- restart leaves no stale runtime state;
- no missing runtime reference or repeated subscription causes gameplay failure;
- relevant EditMode and PlayMode suites pass.

### World / UX / presentation exit criteria

**Responsibility:** World / UX / presentation

- unacceptable blockout/placeholder assets on the release path have been
  replaced;
- all release-critical UI is production-ready;
- all required Milestone 9 feedback is production-ready and integrated;
- the laboratory, ingredients, potions, disasters, HUD, and menus remain readable
  during maximum Stage 4 pressure;
- no presentation defect hides required gameplay state or interaction targets.

### Shared integration exit criteria

**Responsibility:** Shared integration

- the target PC build succeeds;
- the game launches into the intended run flow;
- a complete run can be played from start to Game Over and restarted repeatedly;
- all three ingredient-potion-disaster chains work in the release build;
- pause/menu/run-state transitions work;
- final tuning has been validated against [MVP Scope](mvp-scope.md);
- the final manual MVP smoke test passes;
- there are no known gameplay-breaking bugs.

Discovered bugs, balance findings, and polish opportunities become focused
tickets when found. Do not pre-create speculative polish work merely to fill the
milestone.

### Verification

**EditMode test:**

- run all relevant pure/system suites and resolve regressions.

**PlayMode test:**

- run all relevant runtime/scene suites and resolve regressions.

**Manual validation:**

- build and launch the target PC build;
- play a representative run through multiple difficulty stages;
- exercise all three solution chains and at least one wrong-potion path;
- reach Game Over, restart, and repeat enough of the loop to expose stale-state
  failures;
- inspect maximum-pressure readability, audio balance, and UI state;
- review the final changed assets and known-issue list before release.

### Suggested ticket boundaries

Create Milestone 10 tickets from observed evidence, for example:

- fix a reproducible gameplay/runtime defect;
- replace one remaining unacceptable placeholder set;
- correct one balance discrepancy against MVP Scope;
- fix a late-game readability or feedback problem;
- run and resolve findings from final release validation.

Keep the final milestone exit-gate verification as one explicit integration task
so release readiness is evaluated as a complete game rather than inferred from
individual completed tickets.

## Related pages

- [Project Overview](index.md)
- [Game Design](game-design.md)
- [MVP Scope](mvp-scope.md)
- [Potion Panic Runtime Design](technical-architecture.md)
- [Daily Workflow](../collaboration/team-workflow.md)
- [Presentation Workflows](../guides/unity/presentation-workflows.md)
