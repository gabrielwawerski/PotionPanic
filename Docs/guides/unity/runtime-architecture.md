# Unity runtime foundations

Use this guide to understand how Unity scenes, objects, components, reusable assets, and runtime state fit together. It provides reusable working guidance. The [Potion Panic Target Runtime Design](../../project/technical-architecture.md)
owns the specific types and responsibilities the project intends to build.

## The core mental model

Unity builds a running game by loading scenes that contain GameObjects. A GameObject is a named container with a transform. Components attached to it provide behavior or data. Prefabs save a reusable GameObject hierarchy as an asset. ScriptableObjects hold reusable data that does not need to live in a scene.

```text
Project assets
  -> scenes choose which objects exist together
  -> GameObjects give those objects identity and hierarchy
  -> components give each object focused behavior
  -> prefabs make useful object compositions reusable
  -> ScriptableObjects configure reusable content
  -> runtime state records what is true during this play session
```

These layers solve different problems. Confusing them creates bugs such as a shared ScriptableObject retaining per-run health, UI deciding gameplay rules, or one scene object becoming a hidden dependency of every prefab.

## Scenes define a running context

A scene is a serialized collection of GameObjects and references. It is useful for the objects that must exist together in one loaded context: the laboratory, camera, lighting, run systems, and scene-level UI.

For Potion Panic, the accepted MVP uses one gameplay scene. That does not mean every reusable object should be authored directly inside the scene. Ingredient stations, disasters, UI panels, and visual effects can be prefabs instantiated or referenced by the scene.

Putting everything directly into one scene makes changes difficult to isolate. Putting every object in a prefab can make the scene impossible to understand. Use the scene for composition and use prefabs for repeated or independently testable units.

## GameObjects provide identity and hierarchy

A GameObject answers “what object is this?” Its components answer “what can it do?” Its children express a transform and ownership hierarchy.

Example player composition:

```text
Player
  CharacterController
  PlayerController
  InteractionController
  PlayerInventory
  Visuals
    PlayerModel
```

The root owns movement and gameplay-facing components. The visual child can rotate, animate, or be replaced without changing collision or inventory. A single `PlayerEverything` component would make those concerns harder to test and change independently.

Hierarchy is not a general dependency-injection system. A deeply nested child should not search the entire scene for arbitrary managers merely because both objects happen to be loaded.

## Components own focused behavior

A component should exist because one responsibility needs an owner. For each component, state:

- what state it owns;
- which actions may change that state;
- what information it exposes;
- which systems it may notify;
- what it must not decide.

For example, `PlayerInventory` can own the carried item and reject a second pickup. It should not decide the global score, spawn disasters, or control the Panic meter. Keeping those boundaries explicit prevents a small rule change from spreading through unrelated systems.

## Prefer composition to deep inheritance

Composition combines focused capabilities on a GameObject or prefab. Deep inheritance combines them through a parent-child type hierarchy.

This hierarchy becomes brittle quickly:

```text
WorldObject -> Interactable -> Station -> BrewingStation
  -> AnimatedBrewingStation -> TutorialAnimatedBrewingStation
```

Changing the parent can affect every subtype, and scene behavior becomes difficult to infer from the attached components. Prefer a brewing component, an animation presenter, and any tutorial behavior as separate responsibilities unless a small pure-C# inheritance relationship genuinely expresses one stable concept.

## Separate data, behavior, and current state

| Kind                    | Good owner                   | Potion Panic example                     | Common failure                                           |
|-------------------------|------------------------------|------------------------------------------|----------------------------------------------------------|
| Reusable content data   | ScriptableObject asset       | Which ingredient produces which potion.  | Storing the currently carried item in the shared asset.  |
| Scene-attached behavior | MonoBehaviour component      | A station responds to interaction.       | Making a data asset search the scene or run frame logic. |
| Per-run state           | Component or plain C# object | Current Panic, score, active disasters.  | Letting state survive unintentionally in a shared asset. |
| Presentation state      | View component               | Current meter fill or warning animation. | Letting the view decide gameplay outcomes.               |

ScriptableObjects are assets. Multiple objects can reference the same asset, so changing a field on it at runtime can affect every reader and may persist in the editor in surprising ways. Treat content assets as configuration unless a design explicitly requires shared mutable state.

## State needs one authoritative owner

Ask “who owns the current value?” before deciding who can change it.

- `PlayerInventory` owns the carried item.
- `PanicSystem` owns current Panic.
- `ScoreSystem` owns current score.
- `GameManager` owns the current run state.
- one disaster instance owns its own active and escalation state.

Other components may request a change or observe the result. They should not keep unsynchronized copies. If UI, a disaster, and a manager all write their own Panic values, the project no longer has one answer to “what is current Panic?”

## Unity lifecycle methods are timing contracts

Unity invokes lifecycle methods at specific points. Use each for the work its timing guarantees:

| Method        | Typical responsibility                                                     | Failure prevented                                           |
|---------------|----------------------------------------------------------------------------|-------------------------------------------------------------|
| `Awake`       | Validate serialized references and cache same-object components.           | First-frame null references and repeated lookup.            |
| `OnEnable`    | Subscribe to events and enable reversible behavior.                        | Missing updates after an object is re-enabled.              |
| `Start`       | Initialization that needs other enabled objects to have completed `Awake`. | Depending on unspecified component order during self-setup. |
| `Update`      | Frame-based input and non-physics behavior.                                | Input sampled only on physics ticks.                        |
| `FixedUpdate` | Physics-step work for Rigidbody-based movement.                            | Frame-rate-dependent physics forces.                        |
| `OnDisable`   | Unsubscribe and stop reversible work.                                      | Duplicate callbacks or calls into disabled objects.         |
| `OnDestroy`   | Final cleanup that truly belongs to object destruction.                    | Treating temporary disable as permanent destruction.        |

Potion Panic's accepted Milestone 1 movement uses a `CharacterController`, so its exact update path belongs to the approved implementation plan and tests, not a generic rule that all movement must use `FixedUpdate`.

## Make dependencies visible

Prefer the narrowest dependency that expresses the real relationship:

| Relationship                                    | Default                                                                       | Example                                                    |
|-------------------------------------------------|-------------------------------------------------------------------------------|------------------------------------------------------------|
| Same GameObject, required component             | `GetComponent` once in `Awake`, optionally protected with `RequireComponent`. | Player movement obtains its `CharacterController`.         |
| Nearby child or explicitly composed object      | Serialized reference.                                                         | Interaction logic references its prompt presenter.         |
| Object created by another system                | Explicit initialization from the creator.                                     | Disaster manager supplies data to a new disaster instance. |
| One event observed by several independent views | C# event with matched subscribe and unsubscribe.                              | Panic changes update UI and warning audio.                 |

Avoid `GameObject.Find`, repeated scene-wide searches, and hidden scene-name contracts. They allow a component to appear configured while depending on spelling, load order, or an unrelated hierarchy.

Validate a required serialized reference before first use and report which component is misconfigured. A clear setup error is easier to fix than a null reference several interactions later.

## Use events for reactions, not every call

Direct calls are appropriate when one component owns an action and the caller knows that owner. Events are useful when an owner announces a completed state change and several independent systems may react.

Example:

```text
PanicSystem changes Panic
  -> raises PanicChanged
  -> HUD updates the meter
  -> warning audio changes intensity
  -> screen feedback reacts near danger thresholds
```

The listeners present the result. They do not recalculate or overwrite Panic. An event bus for every interaction would hide who owns the operation and make execution order harder to follow.

## Represent game flow explicitly

Scattered booleans can describe impossible combinations:

```text
isPaused = true
isGameOver = true
isPlaying = true
```

An explicit state model defines the allowed condition and transitions. Potion Panic's accepted run states are `MainMenu`, `Playing`, `Paused`, and `GameOver`. For each state, define which input, simulation, UI, and transitions are valid.

The state owner changes the state. Other systems observe it and enable or disable their behavior. This prevents UI panels, input handlers, and spawners from independently deciding what phase the run is in.

## Prefabs are reusable compositions

A useful prefab includes the components and child structure required for one clear purpose, reasonable defaults, and as few scene-specific references as possible.

A disaster prefab might contain its runtime component, visual hierarchy, collider, audio source, and local VFX anchors. The disaster manager supplies spawn location and content data. The prefab should not depend on a manually wired reference to one particular scene's HUD.

Prefab variants are useful when several objects share a stable base composition and differ in a controlled set of overrides. They are harmful when the base is so broad that every variant disables half of it.

## Apply the model to a new feature

When adding a feature:

1. Identify the player-observable behavior in the accepted project documents.
2. Name the runtime state it needs and one owner for each value.
3. Separate reusable content from per-run state.
4. Choose the smallest components with coherent responsibilities.
5. Make local dependencies visible and define event reactions separately.
6. Decide which object or scene composes the pieces.
7. Define failure behavior and verification before adding optional abstraction.

The goal is traceability: a contributor should be able to follow an action from input, through its state owner, to its presentation without searching every scene object.

## Related pages

- [Potion Panic Target Runtime Design](../../project/technical-architecture.md)
- [Coding and Implementation](coding-and-implementation.md)
- [Editor Safety](editor-safety.md)
