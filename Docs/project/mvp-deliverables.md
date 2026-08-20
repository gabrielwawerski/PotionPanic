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
- suggested ticket seeds.

It does not own:

- locked gameplay rules or tuning;
- player-facing design intent;
- runtime responsibility definitions;
- task status or assignees;
- exact affected files for a ticket;
- task-specific implementation plans;
- implementation notes or history;
- branch names.

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
3. Keep exact affected files, assignees, status, priority, implementation plans,
   implementation notes, and current blockers in the ticket.
4. Do not create one ticket per deliverable bullet mechanically.
5. Keep a milestone integration or exit-gate ticket when independently produced
   artifacts must be proven together.
6. Preserve milestone order from [MVP Scope](mvp-scope.md); this document does
   not authorize later work early.

## How to use the suggested ticket seeds

The ticket seeds below pre-fill only information that is stable enough to live in
an evergreen project contract. They are designed to map to the Docboard ticket
modal without turning this document into live board state.

A seed may provide:

- **Title** through the seed heading;
- **Description** as the intended ticket outcome;
- **Acceptance criteria** derived from the accepted milestone contract;
- **Definition of Done** as the minimum evidence required for that ticket;
- **Suggested tags** as advisory classification;
- **Milestone** as the stable delivery stage;
- **Dependencies** where sequencing is already known;
- **Documentation** that should be read or updated when its owned facts change;
- **Likely affected areas** when the repository contract already makes them
  predictable.

Do not pre-fill these from the evergreen seed unless the ticket creator has
current evidence:

- Status;
- Priority;
- Assignee;
- Implementation Plan;
- Implementation Notes;
- temporary Notes or blockers.

The actual ticket owns those execution-time fields. A suggested dependency names
a prerequisite outcome or seed, not a permanent ticket ID. Likely affected areas
are advisory; inspect the current repository before converting them into exact
file paths in a ticket.

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

### Suggested ticket seeds

#### Implement player movement and collision

**Responsibility:** Gameplay / systems  
**Milestone:** Milestone 1  
**Suggested tags:** `gameplay`, `player`, `movement`

**Description**

Implement the accepted MVP player movement foundation with `PlayerController`
and `CharacterController`, including world-aligned movement and collision.

**Acceptance criteria**

- WASD moves the player on world X/Z axes;
- diagonal input does not increase movement speed;
- movement uses `CharacterController` rather than `Rigidbody` gameplay;
- player collision prevents traversal through configured room boundaries;
- Milestone 1 does not introduce sprint, jump, mouse look, camera follow, or
  camera-relative movement.

**Definition of Done**

- relevant movement tests pass where practical;
- movement is manually validated in the gameplay scene;
- no new relevant Unity Console errors or warnings are introduced;
- Runtime Design is updated only if the accepted responsibility contract changes.

**Dependencies:** none beyond the existing Unity/input scaffold.

**Documentation**

- `Docs/project/mvp-scope.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`

**Likely affected areas**

- `Assets/Scripts/Runtime/Player/`
- `Assets/Tests/PlayMode/`
- player setup in the current gameplay scene or player prefab, if present.

#### Create laboratory blockout and fixed camera

**Responsibility:** World / UX / presentation  
**Milestone:** Milestone 1  
**Suggested tags:** `world`, `camera`, `blockout`

**Description**

Create the Milestone 1 laboratory blockout, playable boundaries, and fixed
orthographic top-down camera needed to prove room scale and readability.

**Acceptance criteria**

- the shared gameplay scene is renamed or replaced by `Laboratory.unity` as
  specified by MVP Scope;
- the room has a readable floor footprint and enclosing collision/boundaries;
- the fixed orthographic camera frames the intended playable area;
- all intended Milestone 1 traversable space is visible and reachable;
- final environment decoration and production art are not required.

**Definition of Done**

- the scene opens without missing-reference errors;
- camera framing and traversable boundaries are manually validated;
- serialized scene changes are reviewed before handoff;
- blockout assets are clearly temporary where appropriate.

**Dependencies:** none; can progress in parallel with player movement until
integration.

**Documentation**

- `Docs/project/game-design.md`
- `Docs/project/mvp-scope.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/guides/unity/editor-safety.md`

**Likely affected areas**

- `Assets/Scenes/`
- current scene-level player/camera setup;
- existing world/blockout assets under `Assets/`.

#### Integrate and verify Milestone 1 gameplay scene

**Responsibility:** Shared integration  
**Milestone:** Milestone 1  
**Suggested tags:** `integration`, `testing`, `milestone`

**Description**

Integrate movement, collision, laboratory blockout, and camera into the first
playable milestone scene and prove the complete Milestone 1 outcome.

**Acceptance criteria**

- Player is configured in `Laboratory.unity` with the accepted movement setup;
- camera and room boundaries agree with the traversable space;
- the player can reach every intended area and cannot leave the room;
- movement remains normalized and the camera remains fixed during play.

**Definition of Done**

- relevant PlayMode coverage passes where practical;
- the full milestone path is manually exercised in Play Mode;
- Unity Console is reviewed for new relevant errors/warnings;
- scene/prefab serialized diffs and references are reviewed.

**Dependencies**

- Implement player movement and collision.
- Create laboratory blockout and fixed camera.

**Documentation**

- `Docs/project/mvp-scope.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`

**Likely affected areas**

- `Assets/Scenes/Laboratory.unity`
- player/camera scene references;
- `Assets/Tests/PlayMode/`.

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

### Suggested ticket seeds

#### Implement reusable interactable abstraction and targeting

**Responsibility:** Gameplay / systems  
**Milestone:** Milestone 2  
**Suggested tags:** `gameplay`, `interaction`

**Description**

Implement `IInteractable` and `InteractionController` so the player can select
and invoke one nearby interactable through a reusable path.

**Acceptance criteria**

- interactable objects expose the common `IInteractable` contract;
- the player detects nearby valid interactables;
- one current target is selected deterministically;
- pressing Interact invokes only the selected target;
- leaving range clears an invalid target;
- `InteractionController` contains no station- or disaster-specific gameplay
  behavior.

**Definition of Done**

- isolated target-selection tests pass where appropriate;
- PlayMode coverage proves enter/select/interact/leave behavior;
- the temporary proof interactable can be replaced by later gameplay objects
  without changing the interaction framework.

**Dependencies:** Milestone 1 player foundation.

**Documentation**

- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`
- `Docs/project/mvp-scope.md`

**Likely affected areas**

- `Assets/Scripts/Runtime/Interaction/`
- player interaction setup;
- `Assets/Tests/EditMode/`
- `Assets/Tests/PlayMode/`.

#### Implement interaction prompt

**Responsibility:** World / UX / presentation  
**Milestone:** Milestone 2  
**Suggested tags:** `ui`, `ux`, `interaction`

**Description**

Provide blockout-quality prompt feedback that communicates when the current
interaction target is available and what action the player can take.

**Acceptance criteria**

- prompt visibility follows the current interaction target;
- no target produces no active interaction prompt;
- prompt text/input indication is readable from the accepted camera/view;
- presentation observes interaction state without owning target-selection logic.

**Definition of Done**

- prompt show/hide behavior is manually validated across enter/leave cases;
- UI does not mutate gameplay interaction state;
- no new relevant UI/runtime errors are introduced.

**Dependencies:** Implement reusable interactable abstraction and targeting.

**Documentation**

- `Docs/project/mvp-deliverables.md`
- `Docs/project/game-design.md`
- `Docs/project/technical-architecture.md`

**Likely affected areas**

- `Assets/Scripts/Runtime/UI/`
- UI prefab/assets under the current project organization;
- player/UI scene references.

#### Integrate and verify the reusable interaction path

**Responsibility:** Shared integration  
**Milestone:** Milestone 2  
**Suggested tags:** `integration`, `interaction`, `testing`

**Description**

Wire interaction targeting and prompt presentation into the gameplay scene and
prove the generic path with at least one temporary interactable.

**Acceptance criteria**

- entering range exposes one valid target;
- the prompt appears for the selected target;
- Interact invokes only that target;
- leaving range removes the target and prompt;
- the proof does not require object-specific logic in the player controller.

**Definition of Done**

- relevant EditMode/PlayMode coverage passes;
- the complete interaction path is manually verified in Play Mode;
- Unity Console and serialized integration changes are reviewed.

**Dependencies**

- Implement reusable interactable abstraction and targeting.
- Implement interaction prompt.

**Documentation**

- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`

**Likely affected areas**

- `Assets/Scenes/Laboratory.unity`
- player/UI references;
- `Assets/Tests/PlayMode/`.

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

### Suggested ticket seeds

#### Implement item data and `PlayerInventory`

**Responsibility:** Gameplay / systems  
**Milestone:** Milestone 3  
**Suggested tags:** `gameplay`, `inventory`, `data`

**Description**

Implement the MVP single-slot carried-item state and the ingredient/potion data
needed for the first brewing loop.

**Acceptance criteria**

- `IngredientData` can reference its resulting `PotionData`;
- Blue Mushroom data resolves to Cooling Potion data through configuration;
- the player can carry zero or one ingredient or potion;
- the inventory cannot contain both an ingredient and potion simultaneously;
- attempts to acquire a second item while occupied are rejected;
- inventory state can notify presentation without UI owning inventory logic.

**Definition of Done**

- EditMode coverage proves valid and invalid inventory transitions;
- Blue Mushroom -> Cooling Potion configuration is verified;
- runtime code remains independent of Blue Mushroom-specific branching.

**Dependencies:** Milestone 2 interaction foundation for later integration, but
pure inventory/data work can begin independently.

**Documentation**

- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`
- `Docs/project/mvp-scope.md`

**Likely affected areas**

- `Assets/Scripts/Runtime/Player/`
- `Assets/Scripts/Runtime/Data/`
- `Assets/Tests/EditMode/`
- ScriptableObject assets under the current data-asset organization.

#### Implement `IngredientStation`

**Responsibility:** Gameplay / systems  
**Milestone:** Milestone 3  
**Suggested tags:** `gameplay`, `interaction`, `ingredient`

**Description**

Implement a reusable ingredient station that exposes one configured ingredient
through the common interaction path.

**Acceptance criteria**

- the station participates through `IInteractable`;
- the station provides its configured `IngredientData`;
- successful pickup places the ingredient in `PlayerInventory`;
- pickup is rejected while the player is already carrying an item;
- the component can be reused for later ingredient types without branching by
  ingredient name.

**Definition of Done**

- station behavior is covered by relevant runtime tests;
- rejection behavior is verified;
- the station does not own the player's inventory state.

**Dependencies**

- Implement reusable interactable abstraction and targeting.
- Implement item data and `PlayerInventory`.

**Documentation**

- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`

**Likely affected areas**

- `Assets/Scripts/Runtime/Interaction/`
- relevant station prefab/setup under `Assets/`;
- `Assets/Tests/PlayMode/`.

#### Implement `BrewingStation`

**Responsibility:** Gameplay / systems  
**Milestone:** Milestone 3  
**Suggested tags:** `gameplay`, `brewing`, `interaction`

**Description**

Implement the reusable brewing interaction that replaces a carried ingredient
with its configured resulting potion.

**Acceptance criteria**

- the brewing station participates through the common interaction path;
- interaction with no carried ingredient is rejected;
- the station reads the ingredient's configured resulting potion;
- brewing consumes/replaces the ingredient state and grants the potion;
- the component contains no Blue Mushroom-specific recipe branching.

**Definition of Done**

- valid and invalid brewing cases are covered by relevant tests;
- the inventory transition is verified in runtime;
- brewing behavior remains data-driven.

**Dependencies**

- Implement reusable interactable abstraction and targeting.
- Implement item data and `PlayerInventory`.

**Documentation**

- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`
- `Docs/project/game-design.md`

**Likely affected areas**

- `Assets/Scripts/Runtime/Interaction/`
- relevant brewing-station prefab/setup under `Assets/`;
- `Assets/Tests/PlayMode/`.

#### Create first ingredient, potion, and station presentation assets

**Responsibility:** World / UX / presentation  
**Milestone:** Milestone 3  
**Suggested tags:** `content`, `world`, `art`

**Description**

Create blockout-quality representations for Blue Mushroom, Cooling Potion, the
ingredient station, and the brewing station so the first loop is readable in the
world.

**Acceptance criteria**

- Blue Mushroom and Cooling Potion are visually distinguishable;
- ingredient and brewing stations are readable as separate interaction targets;
- representations work at the accepted camera distance and scale;
- assets can be wired to the corresponding data/runtime objects;
- blockout quality is acceptable; production polish is deferred.

**Definition of Done**

- assets are manually reviewed in `Laboratory.unity` from the gameplay camera;
- scale/orientation and interaction readability are acceptable;
- imported/serialized assets contain no broken references.

**Dependencies:** data/runtime contracts for the represented objects should be
known; production can otherwise progress in parallel.

**Documentation**

- `Docs/project/game-design.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/guides/unity/presentation-workflows.md`

**Likely affected areas**

- presentation/model/material assets under `Assets/`;
- station/item prefab assets under the current project organization;
- `Assets/Scenes/Laboratory.unity` during integration.

#### Implement carried-item HUD

**Responsibility:** World / UX / presentation  
**Milestone:** Milestone 3  
**Suggested tags:** `ui`, `ux`, `inventory`

**Description**

Display the player's current carried-item state while keeping inventory ownership
inside `PlayerInventory`.

**Acceptance criteria**

- empty inventory has a correct HUD state;
- carrying Blue Mushroom produces the ingredient state;
- carrying Cooling Potion produces the potion state;
- the HUD updates when inventory state changes;
- presentation does not mutate or own inventory gameplay state.

**Definition of Done**

- all three display states are manually verified;
- runtime updates do not require inspector intervention;
- UI remains readable during normal movement.

**Dependencies:** Implement item data and `PlayerInventory`.

**Documentation**

- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`
- `Docs/project/game-design.md`

**Likely affected areas**

- `Assets/Scripts/Runtime/UI/`
- UI prefab/assets under the current project organization;
- scene-level UI references.

#### Integrate and verify the complete Milestone 3 loop

**Responsibility:** Shared integration  
**Milestone:** Milestone 3  
**Suggested tags:** `integration`, `brewing`, `testing`

**Description**

Wire the first ingredient station, inventory, brewing station, content, and HUD
into one complete non-disaster gameplay loop.

**Acceptance criteria**

1. Player begins empty-handed.
2. Player interacts with the Blue Mushroom station.
3. Blue Mushroom becomes the carried item.
4. Carried-item HUD shows Blue Mushroom.
5. Player interacts with the Brewing Station.
6. Blue Mushroom is consumed.
7. Cooling Potion becomes the carried item.
8. HUD updates to Cooling Potion.
9. Pickup while already carrying an item is rejected.
10. Brewing with no ingredient is rejected.

**Definition of Done**

- relevant EditMode and PlayMode coverage passes;
- the complete loop is manually verified in `Laboratory.unity`;
- Unity Console and serialized integration diffs are reviewed;
- no object-specific workaround bypasses the common interaction/inventory path.

**Dependencies**

- Implement item data and `PlayerInventory`.
- Implement `IngredientStation`.
- Implement `BrewingStation`.
- Create first ingredient, potion, and station presentation assets.
- Implement carried-item HUD.

**Documentation**

- `Docs/project/mvp-scope.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`
- `Docs/project/game-design.md`

**Likely affected areas**

- `Assets/Scenes/Laboratory.unity`
- relevant station/item prefabs and data assets;
- player/UI serialized references;
- `Assets/Tests/PlayMode/`.

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

### Suggested ticket seeds

#### Implement disaster data and `DisasterInstance`

**Responsibility:** Gameplay / systems  
**Milestone:** Milestone 4  
**Suggested tags:** `gameplay`, `disaster`, `data`

**Description**

Implement the reusable disaster runtime/data contract and configure the first
Mana Hotspot to use Cooling Potion as its solution.

**Acceptance criteria**

- `DisasterData` can define required potion and accepted Panic/escalation data;
- Mana Hotspot data references Cooling Potion;
- `DisasterInstance` tracks age and escalation state;
- correct potion resolves the instance;
- wrong potion is consumed, leaves the disaster active, applies `+10 Panic`, and
  awards no score;
- correct resolution applies the accepted Panic reduction;
- implementation remains reusable for later disaster data.

**Definition of Done**

- EditMode coverage proves correct/incorrect potion and escalation logic;
- runtime behavior is verified without Mana Hotspot-specific framework code;
- locked values are sourced from MVP Scope rather than duplicated as a new
  authority.

**Dependencies:** Milestone 3 inventory/brewing loop.

**Documentation**

- `Docs/project/mvp-scope.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`
- `Docs/project/game-design.md`

**Likely affected areas**

- `Assets/Scripts/Runtime/Disasters/`
- `Assets/Scripts/Runtime/Data/`
- `Assets/Tests/EditMode/`
- disaster ScriptableObject assets under the current organization.

#### Implement initial `PanicSystem`

**Responsibility:** Gameplay / systems  
**Milestone:** Milestone 4  
**Suggested tags:** `gameplay`, `panic`, `system`

**Description**

Implement the runtime Panic state required for active-disaster pressure,
resolution reduction, and wrong-potion penalties.

**Acceptance criteria**

- Panic has one authoritative runtime owner;
- additions and reductions are applied through the Panic system;
- Panic remains clamped to the accepted `0-100` range;
- changes can notify presentation/runtime listeners;
- disaster logic does not directly own global Panic state.

**Definition of Done**

- pure Panic-state behavior is covered by EditMode tests;
- integration points required by `DisasterInstance` are verified;
- no duplicate global Panic state is introduced.

**Dependencies:** disaster behavior contract may be developed in parallel but
must integrate before milestone completion.

**Documentation**

- `Docs/project/mvp-scope.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`

**Likely affected areas**

- `Assets/Scripts/Runtime/Systems/`
- `Assets/Tests/EditMode/`.

#### Create Mana Hotspot presentation and escalation states

**Responsibility:** World / UX / presentation  
**Milestone:** Milestone 4  
**Suggested tags:** `content`, `disaster`, `vfx`

**Description**

Create blockout-quality Mana Hotspot presentation for normal, escalated,
resolved, and wrong-potion states using the accepted location-agnostic disaster
identity.

**Acceptance criteria**

- Mana Hotspot reads as an unstable magical area rather than fixed laboratory
  machinery;
- normal and escalated states are visually distinguishable;
- success and wrong-potion feedback are distinguishable;
- the representation works at valid arbitrary spawn locations;
- blockout quality is acceptable at this milestone.

**Definition of Done**

- all required states are manually reviewed from the gameplay camera;
- the prefab/representation can be driven by authoritative runtime state;
- serialized asset references are valid.

**Dependencies:** Implement disaster data and `DisasterInstance` for final
integration; presentation work can begin from Game Design earlier.

**Documentation**

- `Docs/project/game-design.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/guides/unity/presentation-workflows.md`

**Likely affected areas**

- disaster prefab/presentation assets under `Assets/`;
- relevant material/VFX assets;
- `Assets/Scenes/Laboratory.unity` during integration.

#### Implement Panic meter

**Responsibility:** World / UX / presentation  
**Milestone:** Milestone 4  
**Suggested tags:** `ui`, `panic`, `ux`

**Description**

Display current Panic clearly while keeping the authoritative value in
`PanicSystem`.

**Acceptance criteria**

- the meter represents the current `0-100` Panic value;
- presentation updates when Panic changes;
- increases and reductions are visible during disaster interaction;
- UI does not own or calculate gameplay Panic rules.

**Definition of Done**

- Panic changes are manually validated across normal, correct-resolution, and
  wrong-potion paths;
- UI remains readable from the target gameplay view;
- no duplicate Panic state exists in presentation code.

**Dependencies:** Implement initial `PanicSystem`.

**Documentation**

- `Docs/project/mvp-deliverables.md`
- `Docs/project/game-design.md`
- `Docs/project/technical-architecture.md`

**Likely affected areas**

- `Assets/Scripts/Runtime/UI/`
- UI prefab/assets under the current project organization;
- scene-level UI references.

#### Integrate and verify the first complete disaster loop

**Responsibility:** Shared integration  
**Milestone:** Milestone 4  
**Suggested tags:** `integration`, `disaster`, `testing`

**Description**

Wire Mana Hotspot, Panic, potion application, presentation, and HUD into the
first complete disaster-resolution loop.

**Acceptance criteria**

- Mana Hotspot can exist in `Laboratory.unity` and accepts potion interaction;
- active state raises Panic using the accepted base rate;
- escalation changes the accepted pressure and presentation state;
- Cooling Potion resolves the disaster and reduces Panic correctly;
- wrong potion is consumed, leaves the disaster active, adds `10 Panic`, and
  awards no score;
- all outcomes use the common inventory/interaction path.

**Definition of Done**

- relevant EditMode and PlayMode coverage passes;
- normal, escalated, correct, and wrong-potion paths are manually exercised;
- Console and serialized integration changes are reviewed.

**Dependencies**

- Implement disaster data and `DisasterInstance`.
- Implement initial `PanicSystem`.
- Create Mana Hotspot presentation and escalation states.
- Implement Panic meter.

**Documentation**

- `Docs/project/mvp-scope.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`
- `Docs/project/game-design.md`

**Likely affected areas**

- `Assets/Scenes/Laboratory.unity`
- Mana Hotspot prefab/data assets;
- player/inventory/Panic serialized references;
- `Assets/Tests/PlayMode/`.

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

### Suggested ticket seeds

#### Implement run-state coordination

**Responsibility:** Gameplay / systems  
**Milestone:** Milestone 5  
**Suggested tags:** `gameplay`, `run-state`, `core`

**Description**

Implement the `GameManager` run-state ownership needed to initialize, play, end,
and restart the current vertical slice without absorbing unrelated gameplay
rules.

**Acceptance criteria**

- one runtime owner coordinates the current run state;
- starting a run initializes the systems required by the current milestone;
- Game Over can be entered from authoritative gameplay state;
- restart invokes the accepted clean-reset path;
- `GameManager` does not own disaster internals, brewing rules, score math, or
  direct presentation logic.

**Definition of Done**

- isolated transition/reset logic is tested where practical;
- runtime state changes are verified through the vertical slice;
- ownership remains consistent with Runtime Design.

**Dependencies:** Milestone 4 first disaster loop.

**Documentation**

- `Docs/project/mvp-scope.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`

**Likely affected areas**

- `Assets/Scripts/Runtime/Core/`
- `Assets/Tests/EditMode/`
- `Assets/Tests/PlayMode/`.

#### Implement disaster spawning and spawn points

**Responsibility:** Gameplay / systems  
**Milestone:** Milestone 5  
**Suggested tags:** `gameplay`, `spawning`, `disaster`

**Description**

Implement `DisasterManager` and `DisasterSpawnPoint` so disasters can appear at
valid laboratory locations, be tracked while active, and release occupancy when
resolved or reset.

**Acceptance criteria**

- valid spawn points can be configured in the gameplay scene;
- the first disaster appears after the accepted initial delay;
- recurring spawning continues while the run is active;
- active disasters are tracked centrally without moving their internal logic
  into `DisasterManager`;
- occupied spawn points cannot double-spawn;
- resolution/restart clears occupancy correctly.

**Definition of Done**

- active tracking and occupancy behavior have relevant tests;
- spawn timing and cleanup are verified in Play Mode;
- no stale occupancy remains after restart.

**Dependencies**

- Implement run-state coordination.
- Milestone 4 reusable disaster instance.

**Documentation**

- `Docs/project/mvp-scope.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`

**Likely affected areas**

- `Assets/Scripts/Runtime/Disasters/`
- `Assets/Scripts/Runtime/Core/` or coordinating-system area;
- `Assets/Scenes/Laboratory.unity`
- `Assets/Tests/EditMode/`
- `Assets/Tests/PlayMode/`.

#### Connect Panic to Game Over

**Responsibility:** Gameplay / systems  
**Milestone:** Milestone 5  
**Suggested tags:** `gameplay`, `panic`, `game-over`

**Description**

Connect authoritative Panic state to run termination so reaching `100 Panic`
ends the current run exactly once.

**Acceptance criteria**

- Panic remains clamped to the accepted range;
- reaching `100 Panic` requests the accepted Game Over transition;
- Game Over is not triggered repeatedly by subsequent Panic updates;
- presentation reacts to run state rather than owning the failure rule.

**Definition of Done**

- boundary behavior at `100 Panic` is covered by relevant tests;
- Game Over transition is verified in Play Mode;
- no duplicate failure path is introduced in UI or disaster code.

**Dependencies**

- Implement initial `PanicSystem`.
- Implement run-state coordination.

**Documentation**

- `Docs/project/mvp-scope.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`

**Likely affected areas**

- `Assets/Scripts/Runtime/Systems/`
- `Assets/Scripts/Runtime/Core/`
- `Assets/Tests/EditMode/`
- `Assets/Tests/PlayMode/`.

#### Implement initial `ScoreSystem`

**Responsibility:** Gameplay / systems  
**Milestone:** Milestone 5  
**Suggested tags:** `gameplay`, `score`, `system`

**Description**

Introduce authoritative run score state sufficient for the vertical slice and
clean restart, while leaving the complete Milestone 7 award rules for later.

**Acceptance criteria**

- score has one authoritative runtime owner;
- the system can accept the scoring behavior required by the current vertical
  slice without UI owning score state;
- score can notify presentation;
- starting/restarting a run resets score cleanly;
- full survival/fast-response completion remains Milestone 7 work.

**Definition of Done**

- score reset/state behavior is covered by EditMode tests;
- basic score changes can be observed by presentation;
- later scoring rules can extend the system without replacing its ownership.

**Dependencies:** Implement run-state coordination.

**Documentation**

- `Docs/project/mvp-scope.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`

**Likely affected areas**

- `Assets/Scripts/Runtime/Systems/`
- `Assets/Tests/EditMode/`.

#### Implement basic score and Game Over presentation

**Responsibility:** World / UX / presentation  
**Milestone:** Milestone 5  
**Suggested tags:** `ui`, `score`, `game-over`

**Description**

Provide blockout-quality score, Game Over, and restart presentation for the
repeatable vertical slice.

**Acceptance criteria**

- current runtime score is displayed;
- Game Over state is visibly distinct from active play;
- restart control is available from the failure state;
- presentation observes `ScoreSystem` and run state without owning either rule;
- production-ready menu polish remains Milestone 8 work.

**Definition of Done**

- score updates, Game Over visibility, and restart control are manually verified;
- UI remains readable from the target view;
- no gameplay state is duplicated in presentation code.

**Dependencies**

- Implement initial `ScoreSystem`.
- Connect Panic to Game Over.

**Documentation**

- `Docs/project/mvp-deliverables.md`
- `Docs/project/game-design.md`
- `Docs/project/technical-architecture.md`

**Likely affected areas**

- `Assets/Scripts/Runtime/UI/`
- UI prefab/assets under the current project organization;
- scene-level UI references.

#### Implement restart/reset flow

**Responsibility:** Gameplay / systems  
**Milestone:** Milestone 5  
**Suggested tags:** `gameplay`, `restart`, `stability`

**Description**

Implement the accepted clean restart path so a completed/failed run cannot leak
transient state into the next run.

**Acceptance criteria**

- restart reloads `Laboratory.unity` as specified by MVP Scope;
- Panic and score reset;
- active/resolved disaster state does not persist;
- spawn timers and occupancy do not persist incorrectly;
- the new run follows the accepted initial spawn delay.

**Definition of Done**

- reset behavior has relevant automated coverage;
- repeated Game Over -> Restart cycles are verified in Play Mode;
- no stale event subscription or serialized runtime state causes duplicate
  behavior after restart.

**Dependencies**

- Implement run-state coordination.
- Implement disaster spawning and spawn points.
- Connect Panic to Game Over.
- Implement initial `ScoreSystem`.

**Documentation**

- `Docs/project/mvp-scope.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`

**Likely affected areas**

- `Assets/Scripts/Runtime/Core/`
- coordinating system scripts;
- `Assets/Tests/PlayMode/`.

#### Integrate and verify the repeatable vertical slice

**Responsibility:** Shared integration  
**Milestone:** Milestone 5  
**Suggested tags:** `integration`, `vertical-slice`, `testing`

**Description**

Prove the complete start -> spawn -> resolve/fail -> Game Over -> restart loop
without manual state repair.

**Acceptance criteria**

- starting a run initializes expected state;
- first disaster appears after the accepted delay;
- recurring spawning continues;
- disasters can still be resolved through the established loop;
- Panic reaching 100 enters Game Over;
- score and failure state are displayed;
- restart creates a clean new run with no stale state.

**Definition of Done**

- relevant EditMode and PlayMode suites pass;
- repeated full-loop manual validation succeeds;
- Console and serialized integration changes are reviewed;
- no system bypass is required to recover between runs.

**Dependencies**

- Implement run-state coordination.
- Implement disaster spawning and spawn points.
- Connect Panic to Game Over.
- Implement initial `ScoreSystem`.
- Implement basic score and Game Over presentation.
- Implement restart/reset flow.

**Documentation**

- `Docs/project/mvp-scope.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`
- `Docs/project/game-design.md`

**Likely affected areas**

- `Assets/Scenes/Laboratory.unity`
- run/disaster/Panic/score/UI serialized references;
- `Assets/Tests/PlayMode/`.

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

### Suggested ticket seeds

#### Configure remaining ingredient and potion data

**Responsibility:** Gameplay / systems  
**Milestone:** Milestone 6  
**Suggested tags:** `data`, `ingredient`, `potion`

**Description**

Add the remaining MVP ingredient/potion data and relationships through the
proven data-driven brewing path.

**Acceptance criteria**

- Green Slime resolves to Slime Dissolver through configured data;
- Purple Crystal Dust resolves to Purification Potion through configured data;
- all three `IngredientData` and `PotionData` targets required by the MVP exist;
- no additional recipe framework or name-based branching is introduced.

**Definition of Done**

- relationship/configuration tests pass;
- data assets satisfy the accepted runtime contract;
- all data remains reusable by the existing inventory/brewing systems.

**Dependencies:** Milestone 3 data/inventory/brewing foundation.

**Documentation**

- `Docs/project/game-design.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`

**Likely affected areas**

- `Assets/Scripts/Runtime/Data/` only if the data contract itself needs no
  expansion or a justified compatible expansion;
- ScriptableObject data assets under the current organization;
- `Assets/Tests/EditMode/`.

#### Configure Slime Leak disaster data

**Responsibility:** Gameplay / systems  
**Milestone:** Milestone 6  
**Suggested tags:** `data`, `disaster`, `slime`

**Description**

Configure Slime Leak as a second reusable `DisasterData` instance using Slime
Dissolver as its solution and the accepted shared tuning.

**Acceptance criteria**

- Slime Leak references Slime Dissolver;
- default and Stage 4 tuning come from the accepted MVP rules;
- it uses the existing `DisasterInstance` path for ticking, escalation,
  correct resolution, and generic wrong-potion behavior;
- no Slime Leak-specific gameplay framework is introduced.

**Definition of Done**

- data validation/relationship tests pass;
- the disaster can be instantiated through the existing runtime contract;
- no post-MVP topology or bespoke wrong-potion behavior is added.

**Dependencies**

- Configure remaining ingredient and potion data.
- Milestone 4 reusable disaster system.

**Documentation**

- `Docs/project/game-design.md`
- `Docs/project/mvp-scope.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`

**Likely affected areas**

- disaster ScriptableObject assets;
- `Assets/Tests/EditMode/`.

#### Configure Hex Cloud disaster data

**Responsibility:** Gameplay / systems  
**Milestone:** Milestone 6  
**Suggested tags:** `data`, `disaster`, `hex`

**Description**

Configure Hex Cloud as the third reusable `DisasterData` instance using
Purification Potion as its solution and the accepted shared tuning.

**Acceptance criteria**

- Hex Cloud references Purification Potion;
- default and Stage 4 tuning come from the accepted MVP rules;
- it uses the existing disaster runtime path;
- generic wrong-potion behavior remains the MVP fallback;
- no topology-affecting escalation or bespoke reaction system is added.

**Definition of Done**

- data validation/relationship tests pass;
- the disaster can be instantiated through the existing runtime contract;
- configuration matches the accepted design identity.

**Dependencies**

- Configure remaining ingredient and potion data.
- Milestone 4 reusable disaster system.

**Documentation**

- `Docs/project/game-design.md`
- `Docs/project/mvp-scope.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`

**Likely affected areas**

- disaster ScriptableObject assets;
- `Assets/Tests/EditMode/`.

#### Complete production-ready ingredient and potion presentation

**Responsibility:** World / UX / presentation  
**Milestone:** Milestone 6  
**Suggested tags:** `content`, `art`, `items`

**Description**

Bring all three ingredients and all three potions to the production-ready visual
quality required for the MVP content milestone.

**Acceptance criteria**

- Blue Mushroom, Green Slime, and Purple Crystal Dust are distinguishable from
  the fixed camera;
- Cooling Potion, Slime Dissolver, and Purification Potion are distinguishable;
- accepted blue/green/purple relationships remain immediately readable;
- world/carried representations and required UI/icon metadata are available;
- temporary Milestone 3 placeholders are replaced where they are not acceptable
  for release.

**Definition of Done**

- all six item representations are reviewed in gameplay context;
- required materials/icons/imports contain no broken references;
- the carried-item HUD remains readable for every item.

**Dependencies:** accepted item data identities; can progress alongside remaining
data configuration.

**Documentation**

- `Docs/project/game-design.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/guides/unity/presentation-workflows.md`

**Likely affected areas**

- item model/material/icon assets under `Assets/`;
- item/station prefab assets under the current organization;
- carried-item UI assets where required.

#### Complete production-ready Mana Hotspot presentation

**Responsibility:** World / UX / presentation  
**Milestone:** Milestone 6  
**Suggested tags:** `content`, `disaster`, `mana-hotspot`

**Description**

Replace unacceptable Mana Hotspot blockout presentation with the
production-ready MVP representation required before the full content set is
considered complete.

**Acceptance criteria**

- Mana Hotspot retains its location-agnostic unstable-area identity;
- normal and escalation states are readable;
- required warning/icon and resolution presentation exist;
- representation works across valid spawn locations;
- final Milestone 9 audio/VFX polish that is explicitly deferred is not required
  here.

**Definition of Done**

- visual states are reviewed at gameplay scale and multiple valid spawn points;
- prefab/asset references are valid;
- the production-ready result replaces any unacceptable milestone placeholder.

**Dependencies:** Milestone 4 Mana Hotspot runtime/presentation foundation.

**Documentation**

- `Docs/project/game-design.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/guides/unity/presentation-workflows.md`

**Likely affected areas**

- Mana Hotspot prefab/presentation assets under `Assets/`;
- related material/icon assets.

#### Create production-ready Slime Leak presentation

**Responsibility:** World / UX / presentation  
**Milestone:** Milestone 6  
**Suggested tags:** `content`, `disaster`, `slime`

**Description**

Create the production-ready Slime Leak representation required for the MVP
content set.

**Acceptance criteria**

- the disaster reads as bubbling/spreading alchemical slime rather than fixed
  laboratory infrastructure;
- its normal and escalation states are distinguishable;
- required warning/icon and resolution presentation exist;
- it makes sense at any valid spawn location;
- it can be wired to the reusable disaster runtime.

**Definition of Done**

- representation is reviewed from the gameplay camera and multiple spawn points;
- serialized references are valid;
- asset is suitable for the small MVP target before Milestone 9 polish.

**Dependencies:** Configure Slime Leak disaster data for final integration.

**Documentation**

- `Docs/project/game-design.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/guides/unity/presentation-workflows.md`

**Likely affected areas**

- Slime Leak prefab/presentation assets under `Assets/`;
- related material/icon assets.

#### Create production-ready Hex Cloud presentation

**Responsibility:** World / UX / presentation  
**Milestone:** Milestone 6  
**Suggested tags:** `content`, `disaster`, `hex`

**Description**

Create the production-ready Hex Cloud representation required for the MVP
content set.

**Acceptance criteria**

- the disaster reads as an airborne purple hex-magic cloud;
- its normal and escalation states are distinguishable;
- required warning/icon and resolution presentation exist;
- it remains readable and plausible at valid spawn locations;
- it can be wired to the reusable disaster runtime.

**Definition of Done**

- representation is reviewed from the gameplay camera and multiple spawn points;
- serialized references are valid;
- asset is suitable for the small MVP target before Milestone 9 polish.

**Dependencies:** Configure Hex Cloud disaster data for final integration.

**Documentation**

- `Docs/project/game-design.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/guides/unity/presentation-workflows.md`

**Likely affected areas**

- Hex Cloud prefab/presentation assets under `Assets/`;
- related material/icon assets.

#### Integrate and verify all three MVP content chains

**Responsibility:** Shared integration  
**Milestone:** Milestone 6  
**Suggested tags:** `integration`, `content`, `testing`

**Description**

Wire the complete ingredient, potion, and disaster set into the proven gameplay
systems and prove all three solution chains without parallel frameworks.

**Acceptance criteria**

- Blue Mushroom -> Cooling Potion -> Mana Hotspot works;
- Green Slime -> Slime Dissolver -> Slime Leak works;
- Purple Crystal Dust -> Purification Potion -> Hex Cloud works;
- all three disasters can spawn at valid locations and resolve through the same
  runtime path;
- generic wrong-potion behavior remains correct;
- no post-MVP bespoke reactions or topology effects are introduced.

**Definition of Done**

- relationship/data tests pass;
- all three chains have PlayMode coverage where practical;
- all three chains are manually validated from collection through resolution;
- fixed-camera readability and spawn compatibility are reviewed.

**Dependencies**

- Configure remaining ingredient and potion data.
- Configure Slime Leak disaster data.
- Configure Hex Cloud disaster data.
- Complete production-ready ingredient and potion presentation.
- Complete production-ready Mana Hotspot presentation.
- Create production-ready Slime Leak presentation.
- Create production-ready Hex Cloud presentation.

**Documentation**

- `Docs/project/game-design.md`
- `Docs/project/mvp-scope.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`

**Likely affected areas**

- `Assets/Scenes/Laboratory.unity`
- all MVP item/disaster data and prefab assets;
- relevant spawning/serialized references;
- `Assets/Tests/PlayMode/`.

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

### Suggested ticket seeds

#### Implement time-based difficulty stages

**Responsibility:** Gameplay / systems  
**Milestone:** Milestone 7  
**Suggested tags:** `gameplay`, `difficulty`, `timer`

**Description**

Track elapsed run time and expose the four locked difficulty stages at their
accepted time boundaries.

**Acceptance criteria**

- Stage 1 applies from 0:00-0:59;
- Stage 2 begins at 1:00;
- Stage 3 begins at 2:00;
- Stage 4 begins at 3:00 and remains active thereafter;
- stage progression is time-based and does not depend on score;
- stage change does not reset the current run.

**Definition of Done**

- 59->60, 119->120, and 179->180 boundaries are covered by EditMode tests;
- live stage changes are verified during one PlayMode run;
- accepted timing remains sourced from MVP Scope.

**Dependencies:** Milestone 5 repeatable run timer/lifecycle foundation.

**Documentation**

- `Docs/project/mvp-scope.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`

**Likely affected areas**

- `Assets/Scripts/Runtime/Disasters/` or coordinating-system area owning
  difficulty/spawning;
- `Assets/Tests/EditMode/`
- `Assets/Tests/PlayMode/`.

#### Implement cap, no-backlog, and disaster-selection behavior

**Responsibility:** Gameplay / systems  
**Milestone:** Milestone 7  
**Suggested tags:** `gameplay`, `spawning`, `difficulty`

**Description**

Apply stage-specific active-disaster caps, spawn intervals, no-backlog behavior,
and equal selection weighting to the existing disaster scheduler.

**Acceptance criteria**

- each stage uses the locked active-disaster cap and spawn interval;
- when the cap is full, the scheduled spawn is skipped;
- skipped spawns do not queue for later release;
- enabled disaster types are selected with equal weighting;
- a full-cap period does not cause a later burst of deferred disasters.

**Definition of Done**

- cap and skip/no-backlog logic are covered by EditMode tests;
- equal selection behavior/configuration is tested appropriately;
- PlayMode verification proves no deferred-spawn burst.

**Dependencies**

- Implement time-based difficulty stages.
- Milestone 5 disaster spawning.

**Documentation**

- `Docs/project/mvp-scope.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`

**Likely affected areas**

- `Assets/Scripts/Runtime/Disasters/`
- `Assets/Tests/EditMode/`
- `Assets/Tests/PlayMode/`.

#### Complete MVP scoring rules

**Responsibility:** Gameplay / systems  
**Milestone:** Milestone 7  
**Suggested tags:** `gameplay`, `score`, `testing`

**Description**

Complete `ScoreSystem` with the locked resolution, fast-response, and survival
awards.

**Acceptance criteria**

- resolved disaster awards `+100`;
- resolution within `10 seconds` of spawning awards the additional `+50`;
- each full second survived awards `+1`;
- wrong-potion use awards no score;
- score does not receive duplicate awards for one resolution/event;
- restart still resets score cleanly.

**Definition of Done**

- award values and the exact fast-resolution boundary are covered by EditMode
  tests;
- survival scoring is tested at full-second boundaries;
- score remains authoritative in `ScoreSystem`.

**Dependencies:** Milestone 5 initial `ScoreSystem` and disaster lifecycle.

**Documentation**

- `Docs/project/mvp-scope.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`

**Likely affected areas**

- `Assets/Scripts/Runtime/Systems/`
- `Assets/Tests/EditMode/`.

#### Integrate Stage 4 disaster tuning

**Responsibility:** Gameplay / systems  
**Milestone:** Milestone 7  
**Suggested tags:** `gameplay`, `difficulty`, `disaster`

**Description**

Apply the accepted Stage 4 Panic-rate and escalation-time tuning to every MVP
disaster without duplicating disaster-specific frameworks.

**Acceptance criteria**

- Stages 1-3 retain the accepted default disaster tuning;
- Stage 4 uses `1.875 Panic/sec`, 15-second escalation, and `3.75 Panic/sec`
  escalated pressure;
- all enabled disaster types select the correct tuning for the current stage;
- existing active-disaster behavior remains coherent across the Stage 4 boundary.

**Definition of Done**

- tuning selection is covered by EditMode tests;
- Stage 4 behavior is verified in Play Mode across the live stage transition;
- numeric authority remains in MVP Scope.

**Dependencies**

- Implement time-based difficulty stages.
- Milestone 6 full disaster set.

**Documentation**

- `Docs/project/mvp-scope.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`

**Likely affected areas**

- disaster/difficulty runtime code;
- disaster data/configuration if stage tuning is data-driven;
- `Assets/Tests/EditMode/`
- `Assets/Tests/PlayMode/`.

#### Complete score presentation

**Responsibility:** World / UX / presentation  
**Milestone:** Milestone 7  
**Suggested tags:** `ui`, `score`, `ux`

**Description**

Connect the final MVP score rules to the score display and provide enough
feedback for the player to understand meaningful awards.

**Acceptance criteria**

- displayed score always matches `ScoreSystem`;
- resolution, fast-response, and survival awards become visible without UI
  calculating them;
- score remains readable during late-stage pressure;
- detailed audiovisual polish may remain blockout-quality until Milestone 9.

**Definition of Done**

- score display is verified against representative runtime awards;
- no duplicate score state exists in UI;
- late-game readability is manually reviewed.

**Dependencies:** Complete MVP scoring rules.

**Documentation**

- `Docs/project/game-design.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`

**Likely affected areas**

- `Assets/Scripts/Runtime/UI/`
- score UI prefab/assets;
- scene-level UI references.

#### Integrate and verify final difficulty/scoring behavior

**Responsibility:** Shared integration  
**Milestone:** Milestone 7  
**Suggested tags:** `integration`, `difficulty`, `score`

**Description**

Prove that stage progression, spawning pressure, Stage 4 tuning, scoring, and UI
all remain correct during one continuous run.

**Acceptance criteria**

- all stage boundaries change the intended runtime settings;
- active-disaster caps and no-backlog behavior hold under pressure;
- Stage 4 applies the accepted disaster tuning;
- score awards remain correct during the same run;
- UI shows the authoritative score without duplicate awards.

**Definition of Done**

- boundary-focused EditMode tests pass;
- representative PlayMode coverage crosses live stage boundaries;
- a manual run confirms perceptible pressure increase without unreadable cause;
- Console is reviewed for duplicate-event or timing errors.

**Dependencies**

- Implement time-based difficulty stages.
- Implement cap, no-backlog, and disaster-selection behavior.
- Complete MVP scoring rules.
- Integrate Stage 4 disaster tuning.
- Complete score presentation.

**Documentation**

- `Docs/project/mvp-scope.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`
- `Docs/project/game-design.md`

**Likely affected areas**

- difficulty/spawning/score runtime code;
- score UI references;
- `Assets/Tests/PlayMode/`.

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

### Suggested ticket seeds

#### Complete run-state transitions

**Responsibility:** Gameplay / systems  
**Milestone:** Milestone 8  
**Suggested tags:** `gameplay`, `run-state`, `pause`

**Description**

Extend the existing run-state coordination into the complete accepted
`MainMenu`, `Playing`, `Paused`, and `GameOver` flow.

**Acceptance criteria**

- Play transitions MainMenu -> Playing and initializes a run;
- pause transitions Playing -> Paused;
- resume transitions Paused -> Playing;
- reaching failure transitions Playing -> GameOver;
- restart returns to a clean playable run;
- gameplay input and simulation are suppressed appropriately outside Playing;
- all states remain inside `Laboratory.unity` rather than separate gameplay/menu
  scenes.

**Definition of Done**

- allowed transitions/reset rules are tested where isolated;
- PlayMode coverage proves main/pause/game-over/restart transitions;
- no competing run-state owner is introduced.

**Dependencies:** Milestone 5 run-state coordination and restart flow.

**Documentation**

- `Docs/project/mvp-scope.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`

**Likely affected areas**

- `Assets/Scripts/Runtime/Core/`
- `Assets/Tests/EditMode/`
- `Assets/Tests/PlayMode/`.

#### Build production-ready main and pause menus

**Responsibility:** World / UX / presentation  
**Milestone:** Milestone 8  
**Suggested tags:** `ui`, `menu`, `ux`

**Description**

Create production-ready Main Menu and Pause Menu presentation for the accepted
single-scene run flow.

**Acceptance criteria**

- Main Menu exposes the required Play action;
- Pause Menu exposes the required Resume action;
- menus are readable and operable at the target resolution/input setup;
- UI visibility follows authoritative run state;
- menus do not own run-state transitions beyond invoking the accepted actions.

**Definition of Done**

- menu navigation/actions are manually validated;
- UI is production-ready for the MVP target;
- no missing references or overlapping unintended HUD elements remain.

**Dependencies:** Complete run-state transitions for final wiring; visual work can
progress earlier from the accepted state contract.

**Documentation**

- `Docs/project/game-design.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/guides/unity/presentation-workflows.md`

**Likely affected areas**

- UI prefab/assets under `Assets/`;
- `Assets/Scripts/Runtime/UI/` where presentation glue is needed;
- scene-level UI references.

#### Complete production-ready Game Over and restart presentation

**Responsibility:** World / UX / presentation  
**Milestone:** Milestone 8  
**Suggested tags:** `ui`, `game-over`, `restart`

**Description**

Replace the blockout Milestone 5 failure presentation with the production-ready
Game Over and restart UI required for the MVP run flow.

**Acceptance criteria**

- Game Over state is clearly distinct from active/paused play;
- final run information intended for failure state remains readable;
- Restart action invokes the accepted clean restart path;
- gameplay input is not accidentally available behind the failure UI;
- presentation does not duplicate run-state ownership.

**Definition of Done**

- Game Over/restart flow is manually validated repeatedly;
- presentation is production-ready;
- no stale UI state remains after restart.

**Dependencies**

- Complete run-state transitions.
- Milestone 5 clean restart path.

**Documentation**

- `Docs/project/game-design.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/guides/unity/presentation-workflows.md`

**Likely affected areas**

- Game Over/restart UI assets;
- `Assets/Scripts/Runtime/UI/`;
- scene-level UI references.

#### Integrate HUD visibility with run states

**Responsibility:** World / UX / presentation  
**Milestone:** Milestone 8  
**Suggested tags:** `ui`, `hud`, `run-state`

**Description**

Apply the accepted state matrix to HUD/menu visibility so each run state shows
only the information and controls intended for it.

**Acceptance criteria**

- MainMenu hides gameplay HUD;
- Playing shows gameplay HUD and no menu overlay;
- Paused shows the pause menu and only intentionally retained HUD information;
- GameOver shows final run information and Game Over presentation;
- visibility derives from authoritative run state.

**Definition of Done**

- each state is manually checked for correct HUD/menu visibility;
- repeated pause/resume/restart does not leave stale UI active;
- UI state remains presentation-only.

**Dependencies**

- Complete run-state transitions.
- Build production-ready main and pause menus.
- Complete production-ready Game Over and restart presentation.

**Documentation**

- `Docs/project/mvp-deliverables.md`
- `Docs/project/game-design.md`
- `Docs/project/technical-architecture.md`

**Likely affected areas**

- `Assets/Scripts/Runtime/UI/`
- HUD/menu prefab assets;
- scene-level UI references.

#### Verify complete start-to-finish run flow

**Responsibility:** Shared integration  
**Milestone:** Milestone 8  
**Suggested tags:** `integration`, `menu`, `testing`

**Description**

Verify the complete player-facing flow from launching into Main Menu through
Playing, Paused, Game Over, restart, and a new run without editor intervention.

**Acceptance criteria**

- Main Menu -> Playing works;
- Playing -> Paused -> Playing works;
- Playing -> GameOver works at failure;
- GameOver -> Restart -> clean Playing works;
- state-specific input/simulation/HUD/menu behavior matches the accepted matrix;
- no extra scene is required for menu/run states.

**Definition of Done**

- relevant transition PlayMode tests pass;
- the complete run flow is manually exercised from launch;
- Console and UI serialized changes are reviewed;
- repeated state transitions do not produce stale subscriptions or UI.

**Dependencies**

- Complete run-state transitions.
- Build production-ready main and pause menus.
- Complete production-ready Game Over and restart presentation.
- Integrate HUD visibility with run states.

**Documentation**

- `Docs/project/mvp-scope.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`
- `Docs/project/game-design.md`

**Likely affected areas**

- `Assets/Scenes/Laboratory.unity`
- run-state and UI references;
- `Assets/Tests/PlayMode/`.

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

### Suggested ticket seeds

#### Implement gameplay-to-presentation hooks and shared audio routing

**Responsibility:** Gameplay / systems  
**Milestone:** Milestone 9  
**Suggested tags:** `gameplay`, `audio`, `events`

**Description**

Expose stable presentation hooks from authoritative gameplay state changes and
route shared UI/warning/game-state audio through `AudioManager` where the Runtime
Design calls for shared ownership.

**Acceptance criteria**

- ingredient, brewing, potion outcome, disaster, escalation, Panic warning, and
  Game Over events expose the hooks required by final presentation;
- shared/global audio uses `AudioManager` where appropriate;
- local object sounds may remain on their relevant prefabs;
- presentation code does not decide gameplay outcomes;
- one gameplay event does not produce duplicate presentation invocations because
  of repeated subscriptions.

**Definition of Done**

- stable hook invocations are covered by PlayMode tests where practical;
- restart/reload does not duplicate subscriptions;
- Runtime Design ownership remains intact.

**Dependencies:** Milestones 3-8 gameplay events and states must exist before the
complete hook matrix can be finalized.

**Documentation**

- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`
- `Docs/project/game-design.md`

**Likely affected areas**

- gameplay runtime components emitting presentation events;
- `Assets/Scripts/Runtime/UI/` or presentation glue;
- shared audio manager area;
- `Assets/Tests/PlayMode/`.

#### Produce core interaction, brewing, and potion feedback assets

**Responsibility:** World / UX / presentation  
**Milestone:** Milestone 9  
**Suggested tags:** `audio`, `vfx`, `interaction`

**Description**

Produce the production-ready feedback package for ingredient pickup, brewing,
potion completion, correct application, and wrong application.

**Acceptance criteria**

- ingredient pickup has readable feedback;
- brewing interaction has readable feedback;
- potion-ready/completion state has readable feedback where needed;
- correct potion application and wrong potion application cannot be confused;
- the chosen combination of audio/VFX/animation covers the events without
  requiring every event to use every medium;
- assets do not obscure interaction/disaster readability.

**Definition of Done**

- required events are manually validated in gameplay context;
- audio levels and VFX scale are reviewed under normal and pressured play;
- all release-path assets are production-ready.

**Dependencies:** Implement gameplay-to-presentation hooks and shared audio
routing for final integration.

**Documentation**

- `Docs/project/game-design.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/guides/unity/presentation-workflows.md`

**Likely affected areas**

- audio/VFX/animation assets under `Assets/`;
- ingredient/brewing/potion prefabs and UI presentation where relevant.

#### Produce Mana Hotspot feedback package

**Responsibility:** World / UX / presentation  
**Milestone:** Milestone 9  
**Suggested tags:** `audio`, `vfx`, `mana-hotspot`

**Description**

Complete production-ready Mana Hotspot audiovisual feedback for active,
escalated, and resolved states.

**Acceptance criteria**

- active Mana Hotspot has a readable loop or recurring cue where audio is used;
- escalation is perceptibly different from normal activity;
- resolution is clearly communicated;
- effects reinforce the accepted unstable-energy identity;
- feedback remains distinguishable when other disasters are active.

**Definition of Done**

- normal, escalation, and resolution states are manually validated;
- audio/VFX do not obscure other critical threats or interaction targets;
- release-path assets are production-ready.

**Dependencies:** Implement gameplay-to-presentation hooks and shared audio
routing for final integration.

**Documentation**

- `Docs/project/game-design.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/guides/unity/presentation-workflows.md`

**Likely affected areas**

- Mana Hotspot prefab/presentation assets;
- related audio/VFX/animation assets.

#### Produce Slime Leak feedback package

**Responsibility:** World / UX / presentation  
**Milestone:** Milestone 9  
**Suggested tags:** `audio`, `vfx`, `slime`

**Description**

Complete production-ready Slime Leak audiovisual feedback for active,
escalated, and resolved states.

**Acceptance criteria**

- active Slime Leak communicates bubbling/wet/spreading activity;
- escalation is perceptibly stronger than normal activity;
- resolution is clearly communicated;
- feedback remains distinguishable from Mana Hotspot and Hex Cloud;
- effects preserve silhouette and route readability.

**Definition of Done**

- all required states are manually validated under simultaneous-disaster play;
- audio/VFX levels and scale are acceptable;
- release-path assets are production-ready.

**Dependencies:** Implement gameplay-to-presentation hooks and shared audio
routing for final integration.

**Documentation**

- `Docs/project/game-design.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/guides/unity/presentation-workflows.md`

**Likely affected areas**

- Slime Leak prefab/presentation assets;
- related audio/VFX/animation assets.

#### Produce Hex Cloud feedback package

**Responsibility:** World / UX / presentation  
**Milestone:** Milestone 9  
**Suggested tags:** `audio`, `vfx`, `hex`

**Description**

Complete production-ready Hex Cloud audiovisual feedback for active, escalated,
and resolved states.

**Acceptance criteria**

- active Hex Cloud communicates airborne unstable hex magic;
- escalation is perceptibly denser/more aggressive than normal activity;
- resolution is clearly communicated;
- feedback remains distinguishable from Mana Hotspot and Slime Leak;
- cloud effects do not obscure critical interactable/disaster silhouettes.

**Definition of Done**

- all required states are manually validated under simultaneous-disaster play;
- audio/VFX levels and density are acceptable;
- release-path assets are production-ready.

**Dependencies:** Implement gameplay-to-presentation hooks and shared audio
routing for final integration.

**Documentation**

- `Docs/project/game-design.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/guides/unity/presentation-workflows.md`

**Likely affected areas**

- Hex Cloud prefab/presentation assets;
- related audio/VFX/animation assets.

#### Produce Panic, Game Over, and essential UI feedback package

**Responsibility:** World / UX / presentation  
**Milestone:** Milestone 9  
**Suggested tags:** `audio`, `ui`, `panic`

**Description**

Complete production-ready warning and state-change feedback for high Panic, Game
Over, and essential menu/UI actions.

**Acceptance criteria**

- entering the high-Panic warning state is clearly communicated;
- warning treatment increases urgency without hiding laboratory state;
- Game Over has a clear audiovisual cue;
- essential UI confirmations are audible/visible where silence would make the
  state change unclear;
- feedback does not dominate disaster cues or create continuous sensory clutter.

**Definition of Done**

- high-Panic, Game Over, and required UI actions are manually validated;
- audio hierarchy remains readable during late-game pressure;
- release-path assets are production-ready.

**Dependencies:** Implement gameplay-to-presentation hooks and shared audio
routing for final integration.

**Documentation**

- `Docs/project/game-design.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/guides/unity/presentation-workflows.md`

**Likely affected areas**

- Panic/Game Over/UI audio/VFX assets;
- HUD/menu presentation assets;
- scene-level presentation references.

#### Integrate and verify the complete feedback coverage matrix

**Responsibility:** Shared integration  
**Milestone:** Milestone 9  
**Suggested tags:** `integration`, `audio`, `vfx`

**Description**

Wire every required feedback event to its production-ready assets and verify
readability during overlapping late-game pressure.

**Acceptance criteria**

- every event in Required feedback coverage has readable feedback;
- correct and wrong-potion outcomes cannot be confused;
- every disaster's normal and escalated states are distinguishable;
- simultaneous disasters remain distinguishable;
- high-Panic warning remains urgent without masking critical cues;
- restart/reload does not duplicate feedback subscriptions.

**Definition of Done**

- stable hook behavior has PlayMode coverage where practical;
- the entire Required feedback coverage list is manually checked;
- audio balance, VFX density, animation visibility, and late-game overlap are
  reviewed together;
- all release-path feedback is production-ready.

**Dependencies**

- Implement gameplay-to-presentation hooks and shared audio routing.
- Produce core interaction, brewing, and potion feedback assets.
- Produce Mana Hotspot feedback package.
- Produce Slime Leak feedback package.
- Produce Hex Cloud feedback package.
- Produce Panic, Game Over, and essential UI feedback package.

**Documentation**

- `Docs/project/game-design.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/project/technical-architecture.md`

**Likely affected areas**

- gameplay/presentation integration references across relevant prefabs and scene;
- audio/VFX/animation assets;
- `Assets/Tests/PlayMode/`.

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

### Suggested ticket seeds and evidence-driven templates

Milestone 10 should not create speculative cleanup tickets in advance. Instantiate
the first four templates only when concrete evidence exists. The final release
validation ticket is expected once the preceding MVP work is ready.

#### Template: fix a reproducible gameplay/runtime defect

**Responsibility:** Gameplay / systems or Shared integration, based on the defect  
**Milestone:** Milestone 10  
**Suggested tags:** `bug`, `stability`

**Description**

Fix one observed gameplay/runtime defect that blocks or degrades the accepted
MVP behavior. Replace this description with the exact symptom and reproduction
when the ticket is created.

**Acceptance criteria**

- the ticket records a reproducible current failure;
- the underlying cause is corrected without weakening an accepted MVP rule;
- the original reproduction no longer fails;
- relevant neighboring behavior remains intact.

**Definition of Done**

- a regression test is added where practical and verified against the defect;
- the original reproduction is manually retested;
- related EditMode/PlayMode suites pass;
- implementation notes record the actual cause/fix in the ticket, not here.

**Dependencies:** concrete defect evidence from playtesting or release
validation.

**Documentation:** update the owning evergreen contract only if the accepted
behavior itself changes.

**Likely affected areas:** determine from the reproduction and current runtime
ownership before ticket creation.

#### Template: replace an unacceptable placeholder set

**Responsibility:** World / UX / presentation  
**Milestone:** Milestone 10  
**Suggested tags:** `polish`, `content`, `presentation`

**Description**

Replace one observed placeholder/blockout set that remains on the release path
but does not satisfy the production-ready MVP target.

**Acceptance criteria**

- the ticket identifies the exact placeholder set and where it appears;
- replacement matches the accepted design identity and readability needs;
- all serialized references remain valid;
- the replacement does not reduce gameplay readability.

**Definition of Done**

- replacement is reviewed in the target gameplay context;
- affected prefabs/scenes/imports are checked for broken references;
- final asset is suitable for the MVP release target.

**Dependencies:** concrete placeholder finding from the release-path review.

**Documentation**

- `Docs/project/game-design.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/guides/unity/presentation-workflows.md`

**Likely affected areas:** determine from the identified placeholder set.

#### Template: correct a balance discrepancy against MVP Scope

**Responsibility:** Gameplay / systems  
**Milestone:** Milestone 10  
**Suggested tags:** `balance`, `bug`, `testing`

**Description**

Correct one observed runtime value or boundary that differs from the locked
MVP Scope. If playtesting instead suggests changing the locked rule itself, that
is a scope/design decision and must be approved before implementation.

**Acceptance criteria**

- the ticket names the current observed value/behavior and the locked expected
  value/behavior;
- runtime behavior matches MVP Scope after the fix;
- relevant boundary cases are covered by tests;
- no unrelated tuning is changed opportunistically.

**Definition of Done**

- focused tests reproduce and then protect the corrected boundary/value;
- representative PlayMode/manual validation confirms the fix;
- MVP Scope is changed only if the accepted rule was separately revised.

**Dependencies:** concrete discrepancy evidence.

**Documentation**

- `Docs/project/mvp-scope.md`
- `Docs/project/mvp-deliverables.md`

**Likely affected areas:** determine from the runtime owner of the discrepant
value.

#### Template: fix a late-game readability or feedback problem

**Responsibility:** World / UX / presentation or Shared integration  
**Milestone:** Milestone 10  
**Suggested tags:** `polish`, `ux`, `feedback`

**Description**

Fix one observed Stage 4 readability/feedback problem where required gameplay
state becomes difficult to perceive under maximum intended pressure.

**Acceptance criteria**

- the ticket records the exact scenario and readability failure;
- the fix improves the identified signal without hiding another critical one;
- gameplay rules remain unchanged unless separately approved;
- the result is checked with simultaneous disasters/high Panic as relevant.

**Definition of Done**

- the original scenario is manually retested;
- audio/VFX/UI overlap is reviewed after the change;
- relevant runtime/presentation tests still pass.

**Dependencies:** concrete late-game playtest evidence.

**Documentation**

- `Docs/project/game-design.md`
- `Docs/project/mvp-deliverables.md`

**Likely affected areas:** determine from the identified UI/audio/VFX/prefab
source.

#### Run final MVP release validation

**Responsibility:** Shared integration  
**Milestone:** Milestone 10  
**Suggested tags:** `integration`, `release`, `testing`

**Description**

Run the final MVP exit gate against the target PC build and record any remaining
release-blocking findings as focused follow-up tickets.

**Acceptance criteria**

- target PC build succeeds and launches;
- Main Menu -> Playing -> Paused -> Playing -> Game Over -> Restart works;
- all three ingredient-potion-disaster chains work in the build;
- at least one generic wrong-potion path behaves correctly;
- representative play crosses multiple difficulty stages including Stage 4;
- score, Panic, UI, audio, VFX, and disaster readability remain acceptable;
- repeated restart does not expose stale state;
- no known gameplay-breaking bug remains when the ticket is closed.

**Definition of Done**

- all relevant EditMode and PlayMode suites pass;
- target PC build is tested directly;
- the manual MVP smoke test is completed and its evidence is recorded in the
  actual ticket;
- every discovered blocker is either fixed before closure or tracked by a
  focused ticket that prevents premature release.

**Dependencies:** all required Milestones 1-9 deliverables and observed Milestone
10 fixes needed for release readiness.

**Documentation**

- `Docs/project/mvp-scope.md`
- `Docs/project/mvp-deliverables.md`
- `Docs/project/game-design.md`
- `Docs/project/technical-architecture.md`

**Likely affected areas**

- no implementation area is assumed at ticket creation;
- validation may identify follow-up work anywhere on the release path.

## Related pages

- [Project Overview](index.md)
- [Game Design](game-design.md)
- [MVP Scope](mvp-scope.md)
- [Potion Panic Runtime Design](technical-architecture.md)
- [Daily Workflow](../collaboration/team-workflow.md)
- [Presentation Workflows](../guides/unity/presentation-workflows.md)
