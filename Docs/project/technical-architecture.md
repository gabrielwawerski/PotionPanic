# Potion Panic Runtime Contract

Version: 1.0
Target engine: Unity
Target platform: PC

Use this document for Potion Panic runtime structure, data ownership, and
implementation boundaries. Use [`game-design.md`](game-design.md) for
player-facing intent and [`mvp-scope.md`](mvp-scope.md) for milestone order
and locked MVP rules.

## What this contract owns

This contract names the specific data assets, runtime components, coordinating
systems, dependencies, repository locations, and completion criteria that the
MVP needs. It does not teach general Unity architecture patterns; use the
[Architecture Primer](../unity-guides/runtime-architecture.md) for that.

| Area | Contract |
| --- | --- |
| Data | Ingredient, potion, and disaster definitions are ScriptableObjects. |
| Runtime | Components own movement, interaction, inventory, brewing, disaster behavior, Panic, score, and presentation updates. |
| Coordination | Managers coordinate systems; they do not absorb individual gameplay rules. |
| Delivery | Create only the systems needed for the current milestone and retain one reusable disaster loop. |

## Delivery rule

Do not create every script in this document immediately.

Only create the scripts needed for the current milestone. Build the game in
this order:

1. Player movement and camera
2. Interaction system
3. Ingredient and potion loop
4. First disaster
5. Panic, score, restart, and the remaining disasters

A playable vertical slice is more valuable than a complete-looking architecture
with no finished game loop.

## Data Assets

Recommended ScriptableObject types:

### `IngredientData`

Purpose:

- defines an ingredient
- points at the resulting potion
- provides UI and visual metadata

Example fields:

```csharp
string ingredientName;
Sprite icon;
Color themeColor;
PotionData resultingPotion;
```

### `PotionData`

Purpose:

- defines a brewed item the player can carry
- provides UI and visual metadata

Example fields:

```csharp
string potionName;
Sprite icon;
Color themeColor;
```

### `DisasterData`

Purpose:

- defines a disaster type
- defines the required solution
- defines Panic behavior and escalation values
- provides warning and prefab metadata

Example fields:

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

### `DisasterType`

Keep the MVP enum small:

```csharp
public enum DisasterType
{
  Fire,
  Slime,
  ToxicCloud
}
```

## Runtime Components

### `PlayerController`

Responsibilities:

- read movement input
- move the player
- keep movement responsive

Milestone 1 decisions:

- use `CharacterController`
- move only on world X/Z axes
- no jumping, sprinting, gravity gameplay, or camera-relative movement
- keep diagonal speed normalized

Should not handle:

- brewing logic
- disaster logic
- Panic or scoring
- UI state

### `InteractionController`

Responsibilities:

- detect nearby interactables
- track the current best target
- show or hide the interaction prompt
- trigger interaction when the player presses Interact

Use a common interface:

```csharp
public interface IInteractable
{
  string GetInteractionPrompt();
  void Interact(InteractionController interactor);
}
```

### `PlayerInventory`

Responsibilities:

- track the currently carried ingredient or potion
- enforce the one-item rule
- clear carried items after brewing or applying potions
- notify the UI when the carried item changes

Possible state:

```csharp
IngredientData carriedIngredient;
PotionData carriedPotion;
```

### `IngredientStation`

Responsibilities:

- provide one ingredient type
- refuse pickup if the player is already carrying something
- play pickup feedback

### `BrewingStation`

Responsibilities:

- convert the carried ingredient into its resulting potion
- refuse interaction if the player has no ingredient
- refuse interaction if the player already carries a potion
- update `PlayerInventory`

MVP rule:

> Ingredient goes in, matching potion comes out.

### `DisasterInstance`

Responsibilities:

- represent one active disaster
- track escalation
- add Panic while active
- validate the applied potion
- resolve itself on a correct potion
- apply the wrong-potion penalty
- notify `DisasterManager` when resolved

MVP behavior:

- active disasters add Panic over time
- escalation increases Panic pressure and feedback
- the correct potion resolves the disaster and reduces Panic by `10`
- the wrong potion is consumed, the disaster stays active, and `10 Panic` is
  added

### `DisasterSpawnPoint`

Responsibilities:

- mark valid disaster spawn locations
- optionally restrict allowed disaster types
- prevent double occupancy

## Coordinating Systems

### `GameManager`

Responsibilities:

- own the current run state
- start a run
- end a run
- restart a run
- pause and unpause

Suggested state model:

```csharp
public enum GameState
{
  MainMenu,
  Playing,
  Paused,
  GameOver
}
```

### `DisasterManager`

Responsibilities:

- spawn disasters
- track active disasters
- choose spawn points
- enforce active-disaster caps
- control time-based difficulty stages
- clear disasters on restart

Should not own the internal logic of one disaster.

### `PanicSystem`

Responsibilities:

- store current Panic
- add and reduce Panic
- clamp between `0` and `100`
- trigger Game Over at `100`
- notify listeners when Panic changes

### `ScoreSystem`

Responsibilities:

- store current score
- award resolve points
- award speed bonuses
- award survival points
- reset score on a new run

### `UIManager`

Responsibilities:

- display Panic, score, carried item, prompts, warnings, and run-state screens
- stay presentation-focused

UI should read game state, not own it.

### `AudioManager`

Responsibilities:

- global UI sounds
- warning sounds
- shared game-state sounds

Local object sounds can stay on object prefabs.

## Repo-Aligned Structure

The docs should match this repo's actual top-level structure:

```text
Assets/
  Scenes/
  Settings/
  Scripts/
    Runtime/
      Core/
      Player/
      Interaction/
      Disasters/
      Systems/
      UI/
      Data/
    Editor/
  Tests/
    EditMode/
    PlayMode/
```

Notes:

- keep gameplay code in `Assets/Scripts/Runtime`
- keep editor-only tooling in `Assets/Scripts/Editor`
- keep pure logic tests in `Assets/Tests/EditMode`
- keep runtime and scene integration tests in `Assets/Tests/PlayMode`
- add subfolders only when they help navigation

## Communication and Dependency Rules

Prefer:

- serialized references
- `GetComponent` for same-object dependencies
- simple C# events for loose reactions
- small, explicit interfaces

Avoid by default:

- large singleton dependency chains
- service locators
- dependency injection frameworks
- custom event buses before the project needs them

## Technical Completion Criteria

The MVP architecture is sufficient when:

- the player can move in the lab
- the player can interact with stations and disasters
- the player can carry one ingredient or one potion
- brewing converts ingredients into potions
- disasters spawn at valid spawn points
- disasters increase Panic while active
- correct potions resolve disasters
- wrong potions consume the potion, leave the disaster active, and add
  `10 Panic`
- reaching `100 Panic` triggers Game Over
- score updates correctly
- UI displays the required run information
- the run can restart cleanly

A system is not complete because every planned script exists. It is complete
when it supports the playable game loop.

## Related pages

- [Project Overview](index.md)
- [Game Design](game-design.md)
- [MVP Scope](mvp-scope.md)
- [Architecture Primer](../unity-guides/runtime-architecture.md)
