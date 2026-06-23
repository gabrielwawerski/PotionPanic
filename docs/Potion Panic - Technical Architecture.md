# Potion Panic - Technical Architecture

Version: 0.1
Project Type: Small Scope 3D Indie Game
Target Engine: Unity
Target Platform: PC

---

# Technical Architecture

The game should use a simple, data-driven architecture.

The goal is not to create a large framework.

The goal is to keep gameplay systems understandable, reusable, and easy to balance.

---

## Purpose of This Document

This document describes the intended technical structure for the MVP.

It is not a line-by-line task list, but it does lock the intended MVP runtime structure and behavior.

The team should use it to:

* keep responsibilities separated
* avoid hardcoded one-off systems
* prevent the `GameManager` from becoming too large
* guide script and folder organization
* support the milestone-based build order

Implementation details may change if a simpler approach is clearly better during development.

The architecture should support the project, not slow it down.

---

## Implementation Rule

Do not create every script in this document immediately.

Only create the scripts needed for the current milestone.

The architecture should emerge in this order:

1. Player movement and camera
2. Interaction system
3. Ingredient and potion loop
4. First disaster
5. Panic, score, restart, and remaining disasters

Avoid building unused managers or systems before they are required by gameplay.

A playable vertical slice is more valuable than a complete-looking architecture with no finished game loop.

---

# Core Architecture Principles

## 1. Build One Reusable Gameplay Loop

The same flow should support all disasters:

1. Disaster appears.
2. Disaster increases Panic.
3. Player identifies required potion.
4. Player gathers ingredient.
5. Player brews potion.
6. Player applies potion.
7. Disaster resolves.
8. Score and Panic update.

Do not build separate unrelated systems for each disaster.

Good:

```text
Fire, slime, and cloud all use the same core DisasterInstance structure.
```

Bad:

```text
FireDisaster, SlimeDisaster, and CloudDisaster each contain unrelated custom gameplay logic.
```

Custom scripts can be added later if a disaster truly needs unique behavior.

For MVP, different disasters should mostly differ through:

* data
* visuals
* audio
* required potion
* Panic rate
* escalation values

---

## 2. Use ScriptableObjects for Static Game Data

ScriptableObjects should store static design data.

Recommended for:

* ingredients
* potions
* disasters

Benefits:

* easier balancing
* cleaner inspector setup
* less hardcoded logic
* easier expansion after MVP

ScriptableObjects should define data.

Runtime MonoBehaviours should handle behavior.

Example:

```text
IngredientData defines what ingredient exists.
IngredientStation defines how the player collects it.
```

---

## 3. Keep Runtime Logic in Runtime Components

Runtime components should handle scene behavior.

Examples:

* player movement
* interaction detection
* carrying items
* brewing
* disaster ticking
* Panic changes
* score changes
* UI updates

Avoid putting active gameplay behavior directly inside ScriptableObjects.

---

## 4. Keep Managers Thin

Managers should coordinate systems.

They should not contain all gameplay logic.

Avoid creating one giant `GameManager` that owns everything.

Managers do not all need to be global singletons.

Use direct scene references, serialized fields, or simple events where appropriate.

Avoid global access points unless there is a clear need.

---

# ScriptableObject Types

## IngredientData

Used for ingredient definitions.

Possible MVP fields:

```csharp
string ingredientName;
Sprite icon;
Color themeColor;
PotionData resultingPotion;
```

Purpose:

* defines what ingredient this is
* defines what potion it creates
* provides UI, icon, and color data

Authority:

* `IngredientData.resultingPotion` is the canonical ingredient-to-potion mapping.

Example assets:

```text
Blue Mushroom
Green Slime
Purple Crystal Dust
```

---

## PotionData

Used for potion definitions.

Possible MVP fields:

```csharp
string potionName;
Sprite icon;
Color themeColor;
```

Purpose:

* provides UI, icon, and color data
* represents the brewed item the player carries and applies
* can be compared directly against `DisasterData.requiredPotion`

Notes:

* `PotionData` is not the source of truth for disaster matching in MVP.
* If a derived `solvesDisasterType` field is added later for UI or filtering, it should mirror `DisasterData.requiredPotion` rather than define gameplay rules.

Example assets:

```text
Cooling Potion
Slime Dissolver
Purification Potion
```

---

## DisasterData

Used for disaster definitions.

Possible MVP fields:

```csharp
string disasterName;
DisasterType disasterType;
PotionData requiredPotion;
float basePanicRate;
float escalationTime;
float escalatedPanicRate;
Sprite warningIcon;
GameObject disasterPrefab;
```

Purpose:

* defines the disaster type
* defines the required solution
* defines Panic behavior
* defines escalation values
* defines presentation data

Authority:

* `DisasterData.requiredPotion` is the canonical disaster-to-solution mapping.

Example assets:

```text
Overheated Cauldron
Slime Leak
Toxic Magic Cloud
```

---

## DisasterType

Used to identify disaster categories.

Possible MVP enum:

```csharp
public enum DisasterType
{
  Fire,
  Slime,
  ToxicCloud
}
```

Purpose:

* lets disasters declare what type they are
* avoids comparing strings during gameplay

Notes:

* The enum should stay small for MVP.
* Add new values only when new disaster families are added.

---

# Runtime Components

## PlayerController

Responsibilities:

* read movement input
* move the player
* handle player-facing movement behavior
* keep movement responsive

Should not handle:

* brewing logic
* disaster logic
* scoring
* Panic values
* UI state
* ingredient/potion data rules

Notes:

* The player should use WASD movement.
* Movement should be tuned for responsiveness before visual polish.
* Mouse look is not part of the MVP.
* The intended camera for MVP is fixed top-down.

---

## InteractionController

Responsibilities:

* detect nearby interactable objects
* track the current best interactable target
* show or hide the interaction prompt
* trigger interaction when the player presses Interact

Should communicate with objects through an interface.

Possible MVP interface:

```csharp
public interface IInteractable
{
  string GetInteractionPrompt();
  void Interact(InteractionController interactor);
}
```

Notes:

* This keeps ingredient stations, brewing stations, and disasters consistent.
* Avoid writing one-off interaction code for every object.
* `InteractionController` may reference `PlayerInventory` so interactable objects can read or modify the carried item.

---

## PlayerInventory

Responsibilities:

* track currently carried ingredient or potion
* prevent carrying more than one item
* clear carried item after brewing or applying potion
* notify UI when carried item changes

MVP rule:

> The player can carry one ingredient OR one potion.

Possible MVP state:

```csharp
IngredientData carriedIngredient;
PotionData carriedPotion;
```

Rules:

* If carrying an ingredient, the player cannot pick up another ingredient.
* If carrying a potion, the player cannot pick up an ingredient.
* Brewing converts the carried ingredient into a potion.
* Applying a potion consumes it.
* The player cannot carry both an ingredient and a potion at the same time.

Notes:

* Do not add stacks.
* Do not add inventory grids.
* Do not add a hotbar.
* Do not add multiple potion slots before MVP is finished.

---

## IngredientStation

Responsibilities:

* provide one ingredient type
* give ingredient to the player when interacted with
* refuse interaction if the player is already carrying something
* play pickup feedback

Uses:

```csharp
IngredientData ingredientData;
```

Notes:

* Ingredient stations are permanent sources.
* They do not need limited stock in MVP.
* Each station should be visually readable from the fixed top-down camera.

Example stations:

```text
Mushroom Shelf
Slime Tank
Crystal Grinder
```

---

## BrewingStation

Responsibilities:

* convert the carried ingredient into its resulting potion
* refuse interaction if the player has no ingredient
* refuse interaction if the player is already carrying a potion
* play brewing feedback
* update the carried item through `PlayerInventory`

Rules:

* No recipe menu in MVP.
* No two-ingredient recipes in MVP.
* No brewing minigame in MVP.
* The carried ingredient directly determines the potion.

Example flow:

```text
Blue Mushroom -> Brewing Station -> Cooling Potion
```

Notes:

* Brewing should be fast or instant.
* The challenge should come from crisis management, not recipe memorization.
* Brewing uses the same `Interact` action as other world objects.

---

## DisasterInstance

Responsibilities:

* represent one active disaster in the scene
* track escalation timer
* add Panic while active
* check whether applied potion is correct
* resolve itself when correct potion is used
* apply the MVP wrong-potion penalty
* notify `DisasterManager` when resolved

Uses:

```csharp
DisasterData disasterData;
```

Suggested MVP behavior:

* While active, increase Panic over time.
* After escalation time, increase Panic rate and update visuals.
* If correct potion is applied, consume the potion, resolve the disaster, reduce Panic by `10`, and award score.
* If wrong potion is applied, consume the potion, keep the disaster active, add `10 Panic`, and show failure feedback.

Notes:

* MVP escalation should be simple.
* Escalation can mean stronger VFX, louder sound, warning icon, and higher Panic rate.
* Actual spreading fire, slime, or cloud behavior should be a stretch feature.
* All three MVP disasters share the same default tuning for Stages 1-3: `1.5 Panic/sec`, escalation at `20` seconds, then `3.0 Panic/sec`.
* Stage 4 spawns use `1.875 Panic/sec`, escalation at `15` seconds, then `3.75 Panic/sec`.

---

## DisasterSpawnPoint

Responsibilities:

* mark valid locations where disasters can appear
* store whether the spawn point is occupied
* optionally define which disaster types are allowed there

Possible MVP fields:

```csharp
bool isOccupied;
List<DisasterType> allowedDisasterTypes;
```

Notes:

* Spawn points should be placed manually in the laboratory.
* Avoid procedural placement in MVP.
* A spawn point should not spawn a second disaster while already occupied.

---

# Managers

Managers should coordinate systems, not contain all gameplay logic.

Avoid creating one giant `GameManager` that owns everything.

Managers do not all need to be singletons.

Use the simplest reference style that works for the current project.

Acceptable approaches:

* serialized scene references
* inspector-assigned dependencies
* simple C# events
* UnityEvents for simple UI wiring
* singleton access only when clearly justified

---

## GameManager

Responsibilities:

* current game state
* starting a run
* ending a run
* restarting a run
* pausing and unpausing
* switching between menu, gameplay, and game over states

Suggested states:

```csharp
public enum GameState
{
  MainMenu,
  Playing,
  Paused,
  GameOver
}
```

Should not handle:

* individual disaster behavior
* ingredient pickup rules
* brewing logic
* UI drawing
* score calculation details
* Panic calculation details

Notes:

* `GameManager` should coordinate state transitions.
* It should not become a dumping ground for unrelated logic.
* MVP uses one gameplay scene: `Laboratory.unity`.
* `MainMenu`, `Playing`, `Paused`, and `GameOver` are run states inside that single scene.
* Starting a run resets Panic, score, inventory, active disasters, and stage timers, then enters `Playing`.
* Restarting a run reloads `Laboratory.unity` for a clean reset.

---

## DisasterManager

Responsibilities:

* spawn disasters
* track active disasters
* control difficulty stages
* choose spawn points
* prevent too many active disasters
* clear disasters on restart

Suggested responsibilities:

```text
- Spawn next disaster after delay
- Increase max active disasters over time
- Select valid spawn point
- Select disaster type
- Register disaster when spawned
- Unregister disaster when resolved
```

Should not handle:

* the internal timer of each disaster
* whether a potion is correct
* Panic increase per frame for each disaster

Those belong to `DisasterInstance`.

Notes:

* `DisasterManager` controls when and where disasters appear.
* `DisasterInstance` controls what an individual disaster does while active.
* The first disaster spawns `3 seconds` after the run enters `Playing`.
* Stage progression is time-based:
  * Stage 1: `0:00-0:59`, max `1` active disaster, spawn every `12` seconds
  * Stage 2: `1:00-1:59`, max `2` active disasters, spawn every `10` seconds
  * Stage 3: `2:00-2:59`, max `3` active disasters, spawn every `8` seconds
  * Stage 4: `3:00+`, max `3` active disasters, spawn every `6` seconds
* If the active-disaster cap is full, no queued spawn backlog is stored.
* Disaster selection uses equal weighting across currently enabled disaster types.

---

## PanicSystem

Responsibilities:

* store current Panic value
* increase Panic
* decrease Panic
* clamp Panic between 0 and 100
* trigger Game Over when Panic reaches 100
* notify UI when Panic changes

Suggested API:

```csharp
public void AddPanic(float amount)
{
}

public void ReducePanic(float amount)
{
}

public float CurrentPanic { get; private set; }
```

Rules:

* Panic should primarily increase from active disasters.
* Passive time-based Panic is not part of the MVP.
* Wrong potion use adds `10 Panic`.
* Resolving a disaster reduces Panic by `10`.
* Panic should never go below 0.
* Panic should never go above 100.

Notes:

* `PanicSystem` should own the Panic value.
* UI should display Panic, not store it.

---

## ScoreSystem

Responsibilities:

* store current score
* award points for resolving disasters
* award survival points in MVP
* award speed bonuses in MVP
* reset score on new run
* notify UI when score changes

MVP scoring:

```text
+100 points for resolving a disaster
+50 points if the disaster is resolved within 10 seconds of spawning
+1 point for each full second survived
```

Optional after MVP:

```text
combo multiplier for consecutive correct solutions
```

Notes:

* Keep scoring simple at first.
* Do not balance the game around complex combos until the core loop feels good.
* Combo chains are not part of MVP scoring.
* Speed and combo behavior affect score, not Panic.

---

## UIManager

Responsibilities:

* display Panic Meter
* display score
* display carried ingredient or potion
* display interaction prompt
* display active warnings
* display menu screens
* display game over screen

Should not own gameplay values.

The UI should read or subscribe to changes from:

* `PanicSystem`
* `ScoreSystem`
* `PlayerInventory`
* `InteractionController`
* `GameManager`

Rule:

> UI displays game state. It should not be the source of gameplay state.

Notes:

* UI scripts should stay presentation-focused.
* Gameplay rules should not be implemented inside UI scripts.

---

## AudioManager

Responsibilities:

* play global UI sounds
* play warning sounds
* play game over sound
* optionally manage music

Local object sounds can remain on the objects themselves.

Examples:

```text
Fire disaster loops fire crackle locally.
Slime disaster loops bubbling locally.
Brewing station plays brewing sound locally.
```

The `AudioManager` should not become required for every small sound effect.

Notes:

* Use local `AudioSource` components for simple object-specific sounds.
* Use `AudioManager` for sounds that are global or shared across game states.

---

## Optional After MVP: VFXManager

Only add this if visual effects become duplicated or messy.

MVP can use particles directly on prefabs.

Use a `VFXManager` later if needed for:

* reusable popup effects
* shared resolve effects
* centralized particle spawning
* object pooling

Do not add this manager before there is a real need.

---

# Suggested Folder Structure

```text
Assets/
  Scripts/
    Core/
      GameManager.cs
      GameState.cs

    Player/
      PlayerController.cs
      PlayerInventory.cs
      InteractionController.cs

    Interaction/
      IInteractable.cs
      IngredientStation.cs
      BrewingStation.cs

    Disasters/
      DisasterManager.cs
      DisasterInstance.cs
      DisasterSpawnPoint.cs
      DisasterType.cs

    Systems/
      PanicSystem.cs
      ScoreSystem.cs
      AudioManager.cs

    UI/
      UIManager.cs
      PanicBarUI.cs
      ScoreUI.cs
      CarriedItemUI.cs
      InteractionPromptUI.cs

    Data/
      IngredientData.cs
      PotionData.cs
      DisasterData.cs

  ScriptableObjects/
    Ingredients/
    Potions/
    Disasters/

  Prefabs/
    Player/
    Stations/
    Disasters/
    UI/
    VFX/

  Scenes/
    Laboratory.unity
```

Notes:

* This folder structure is a starting point.
* If a folder has only one script for most of development, that is acceptable.
* Do not create empty folders only for the sake of matching this structure.
* Add folders when they help navigation.

---

# Architecture Notes

## Keep Disaster Logic Generic

All disasters should use the same base structure.

Different disasters should mostly differ by data and visuals.

Good:

```text
Fire, slime, and cloud all use DisasterInstance.
```

Bad:

```text
FireDisaster, SlimeDisaster, and CloudDisaster each contain separate unrelated logic.
```

Custom scripts can be added later if a disaster truly needs unique behavior.

For MVP, prefer:

```text
One DisasterInstance script
Three DisasterData assets
Three disaster prefabs
```

Instead of:

```text
Three separate disaster systems
```

---

## Avoid Full Inventory

The MVP inventory should remain intentionally limited.

Allowed:

```text
One carried ingredient OR one carried potion.
```

Forbidden before MVP:

* item stacks
* inventory grid
* hotbar
* crafting menu
* storage chest
* multiple potion slots

Inventory complexity should not become part of the first version.

---

## Keep Brewing Fast or Instant

Brewing should be fast and readable.

MVP brewing rule:

> Ingredient goes in, matching potion comes out.

Do not add:

* timing minigames
* recipe memory
* ingredient combinations
* crafting failure chances
* brewing station upgrades

until the main loop is fun.

---

## Keep Escalation Simple

Escalation should increase pressure without multiplying implementation complexity.

MVP escalation can include:

* increased Panic rate
* bigger VFX
* louder sound
* warning icon
* stronger screen/UI warning

Avoid before MVP:

* spreading fire
* dynamic slime growth
* moving gas clouds
* chain reactions
* physics-based hazards

Escalation should first be a data and presentation change, not a complex simulation.

---

## Keep UI Passive

UI should present information.

UI should not own gameplay state.

Good:

```text
PanicSystem owns Panic.
PanicBarUI displays Panic.
```

Bad:

```text
PanicBarUI stores the real Panic value.
```

This makes gameplay easier to test, reset, and modify.

---

## Avoid Premature Optimization

Do not add pooling, service locators, dependency injection frameworks, save systems, or complex event buses before they are needed.

For the MVP, simple Unity references are acceptable.

Add complexity only when it solves a real problem.

---

## Build Order Rule

At every stage, prefer a playable game over a larger unfinished system.

Recommended build order:

```text
One room
One ingredient
One potion
One disaster
One Panic Meter
One complete playable loop
```

Then expand to:

```text
Three ingredients
Three potions
Three disasters
Scoring
Difficulty scaling
Menus
Polish
```

---

# MVP Technical Completion Criteria

The technical architecture is sufficient for MVP when:

* the player can move in the laboratory
* the player can interact with stations and disasters
* the player can carry one ingredient or one potion
* the brewing station converts ingredients into potions
* disasters spawn at valid spawn points
* disasters increase Panic while active
* correct potions resolve disasters
* wrong potions consume the potion, leave the disaster active, and add `10 Panic`
* Panic reaching 100 triggers Game Over
* score updates when disasters are resolved
* UI displays Panic, score, carried item, prompts, and Game Over
* the run can restart cleanly

A system is not complete because every planned script exists.

A system is complete when it supports the playable game loop.
