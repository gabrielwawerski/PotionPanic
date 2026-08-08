# Unity Architecture Primer

Use this guide when learning or applying general Unity choices for component
boundaries, ownership, lifecycle, data, and runtime state. For Potion
Panic-specific data assets and component responsibilities, use the
[Runtime Contract](../../project/technical-architecture.md).

## Core Mental Model

Unity is a scene-driven object composition engine where code controls behavior
attached to objects.

Think in layers:

- scenes
- GameObjects
- components
- prefabs
- ScriptableObject data
- runtime state
- systems
- UI and presentation

Good architecture keeps these layers from becoming tangled.

## Responsibilities Before Scripts

A script should exist because a responsibility needs an owner.

Ask of each component:

- What does it own?
- What is it allowed to change?
- What should it not know about?

Prefer smaller components over one giant behavior class.

## Logic and Presentation

Separate gameplay rules from visuals.

Good separation:

- gameplay components store and change state
- view components listen and present that state
- death, audio, and UI feedback react to events instead of owning the rules

Changing a health bar should not risk breaking combat.

## Composition Over Inheritance

Unity scales better with composition than with deep scene-object inheritance.

Prefer GameObjects assembled from focused components rather than large
inheritance trees such as:

```text
Entity -> Character -> Enemy -> FlyingEnemy -> PoisonFlyingEnemy
```

Use inheritance sparingly for pure C# abstractions when it clearly simplifies
the code.

## Data, Behavior, and Runtime State

Keep these concepts separate:

- ScriptableObject: reusable configuration and data
- MonoBehaviour: scene-attached behavior
- runtime state: current values during play

Avoid storing changing gameplay state in shared ScriptableObjects unless the
design explicitly calls for that behavior.

## Communication Between Systems

Do not let everything talk to everything.

Prefer:

- direct serialized references for local, obvious relationships
- events when one thing announces something and multiple systems may react
- small coordinating systems for broad game-state concerns

If most scripts depend on `GameManager.Instance`, the project is probably too
coupled.

## Game State and Flow

Avoid scattered booleans such as:

```csharp
bool isPaused;
bool isBrewing;
bool isGameOver;
```

Prefer explicit state:

```csharp
public enum GameState
{
  MainMenu,
  Playing,
  Paused,
  GameOver
}
```

For each state, define:

- allowed input
- visible UI
- active systems
- paused systems
- valid transitions

## Prefabs as Reusable Units

A prefab is a reusable unit of composition, not just a saved object.

A good prefab has:

- one clear purpose
- required components present
- reasonable defaults
- minimal scene-specific references
- a readable child hierarchy

## References and Dependencies

Prefer visible, intentional references.

Good defaults:

- serialized fields
- `GetComponent` in `Awake` for same-object dependencies
- explicit initialization from a spawner
- events for looser reactions

Validate critical references early and fail loudly when something required is
missing.

## Unity Lifecycle

Default mental model:

- `Awake`: self setup and local reference caching
- `OnEnable`: subscribe to events
- `Start`: setup that depends on other objects being ready
- `Update`: frame-based input and non-physics logic
- `FixedUpdate`: physics movement
- `OnDisable`: unsubscribe from events

Do not subscribe to events and forget to unsubscribe.

## State Ownership

Always ask:

> Who owns this data?

Examples:

- health owns current health
- inventory owns carried items
- the brewer owns brewing validation
- the state machine owns the current run state

Avoid multiple systems silently owning the same value.

## Hardcoded Rules, Configured Content

Good default:

- hardcode rules
- configure content

Example:

- rule: a recipe consumes an ingredient and produces a potion
- content: which ingredient produces which potion

## Folder and Namespace Guidance

For this repo, keep gameplay code under `Assets/Scripts/Runtime` and editor-only
helpers under `Assets/Scripts/Editor`.

A practical runtime split looks like:

```text
Assets/Scripts/Runtime/
  Core/
  Player/
  Interaction/
  Disasters/
  Systems/
  UI/
  Data/
```

Use namespaces that match the `PotionPanic.*` pattern.

## Final Runtime Rule

Aim for:

- small components
- clear ownership
- data in ScriptableObjects
- runtime state in components or plain classes
- events for reactions
- inspector references for local dependencies
- minimal `Update()` dumping

The goal is not maximum abstraction. The goal is that changing one feature does
not unpredictably break five others.

## Related pages

- [Potion Panic Runtime Contract](../../project/technical-architecture.md)
- [Coding and Implementation](coding-and-implementation.md)
- [Editor Safety](editor-safety.md)
