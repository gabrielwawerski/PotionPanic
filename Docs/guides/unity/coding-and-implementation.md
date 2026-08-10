# Coding and implementation

Use this guide to turn one accepted behavior into code, Unity composition, and evidence that another contributor can verify. Use
[Daily Workflow](../../collaboration/team-workflow.md) for task selection, branching, shared-file coordination, review, and merge.

## Start from observable behavior

Implementation begins with what the player, editor user, or calling system must observe. A class list is not a behavior.

For Milestone 1 movement, the accepted behavior includes:

- WASD produces world-aligned movement on the X/Z plane;
- diagonal input does not move faster;
- a `CharacterController` handles collision;
- there is no jump, sprint, mouse look, or camera-relative movement.

Those statements define a narrow slice. “Create a flexible player framework” does not. It leaves scope, result, and proof undecided.

## Translate behavior into responsibilities

Identify the minimum owners before creating scripts:

| Question                               | Movement example                                                    |
|----------------------------------------|---------------------------------------------------------------------|
| What input exists?                     | The accepted `Player/Move` action supplies a two-axis value.        |
| Who interprets it?                     | `PlayerController`.                                                 |
| Who performs collision-aware movement? | The same object's `CharacterController`.                            |
| What state persists?                   | Serialized movement speed, not an inventory or run-state framework. |
| What does this slice not own?          | Camera logic, interaction, animation, Panic, score, and menus.      |

The result should fit the target runtime design without constructing future systems before their behavior is approved.

## Build the smallest vertical slice

A vertical slice crosses the layers necessary to observe one result. For movement, pure vector math is insufficient by itself because the player also needs a configured scene object and collision. A full gameplay architecture is unnecessary because brewing and disasters do not prove movement.

A practical order is:

1. Prove any pure rule that can fail independently, such as diagonal normalization.
2. Implement the focused runtime component.
3. Configure the minimum prefab or scene object needed to run it.
4. Exercise the behavior in Play Mode.
5. Add only the next dependency required by the same acceptance criteria.

This order exposes design mistakes while the change is still small.

## Choose concrete code before generic frameworks

Create an abstraction when at least two known consumers need the same stable contract or when a boundary must be substituted in tests. Do not build layers for hypothetical future flexibility.

Reasonable early project contracts include:

- one `IInteractable` boundary used by known stations and disasters;
- explicit `IngredientStation` and `BrewingStation` behavior;
- one inventory owner enforcing the accepted one-item rule;
- data assets for known ingredient, potion, and disaster content.

Premature examples include a universal ability system, a dependency-injection container, a generic inventory with arbitrary stacks, or a custom event bus before the first complete gameplay loop exists.

## Design failure behavior before the happy path is finished

For every action, identify invalid state and the safe outcome:

| Action                | Failure                              | Safe behavior                                                                             |
|-----------------------|--------------------------------------|-------------------------------------------------------------------------------------------|
| Pick up an ingredient | Inventory already contains an item.  | Reject the pickup without losing either item.                                             |
| Brew                  | No ingredient is carried.            | Leave inventory and station state unchanged and provide readable feedback.                |
| Apply a potion        | Potion is wrong.                     | Consume it, keep the disaster active, add the accepted Panic penalty, and show the cause. |
| Resolve a disaster    | Resolution is triggered twice.       | Award effects once and ignore or reject later calls.                                      |
| Restart               | Old objects or subscriptions remain. | Establish a clean run without duplicate events or state.                                  |

Failure design prevents later patches from scattering one-off checks across UI, managers, and object scripts.

## Keep dependencies intentional

Use the relationship to choose the mechanism:

- cache a required same-object component once;
- serialize a stable local reference that should be visible in the inspector;
- initialize spawned objects explicitly;
- use events after an owner completes a change and several views may react.

Avoid repeated `FindObjectOfType`, `GameObject.Find`, hidden scene-name lookups, and static mutable state. They reduce setup effort initially by moving the configuration into runtime guesses.

## Keep frame loops narrow

`Update()` is appropriate for frame-based work such as reading input. It is not a general place to rediscover dependencies, poll UI, scan the scene, update unrelated timers, and save state.

When work can happen because a state changed, react to the change. When a timer belongs to one object, keep it with that owner. When several systems need the same value, expose the authoritative owner rather than recomputing it every frame.

## Make state visible while developing

Debug visibility shortens the path from symptom to cause. Useful temporary or development-only information includes:

- current run state;
- carried item identity;
- current interaction target;
- active disaster count and spawn cap;
- Panic contributions;
- gizmos for ranges and spawn points;
- clear logs for rare state transitions and rejected actions.

Do not leave noisy per-frame logging in normal operation. A useful debug signal names the owner, transition, and relevant value.

## Match tests to the behavior boundary

Use EditMode when Unity does not need to run a scene or frame loop:

- value calculation;
- path matching;
- state transitions in plain classes;
- editor-facing logic with controlled dependencies.

Use PlayMode when the proof depends on loaded GameObjects, components, serialization, lifecycle, physics, or scene integration:

- the configured player moves and collides;
- a scene contains the required camera and player composition;
- an interaction works through actual runtime components;
- restarting leaves a clean run.

A unit test for vector output cannot prove that an inspector reference is wired or the gameplay scene contains a `CharacterController`. A PlayMode test for a whole scene is slower and less diagnostic when the rule is pure arithmetic. Use both only when they prove different risks.

## Verify in layers

For a runtime slice:

1. Run the focused automated test that proves the changed rule.
2. Wait for Unity compilation and check the Console.
3. Open the affected scene or prefab and inspect serialized references.
4. Exercise the acceptance criteria in Play Mode.
5. Run the relevant wider EditMode or PlayMode suite.
6. Review the final Git diff, including `.meta`, scene, and prefab files.

Record baseline failures separately. Do not call an unperformed manual check or an unavailable external environment green.

## Handoff evidence

A useful handoff states:

- the accepted behavior implemented;
- the source and serialized assets changed;
- focused and wider tests with their results;
- scene and Console checks performed;
- known limitations and skipped gates;
- shared Unity files the reviewer should inspect carefully.

“Code complete” is not a result. The slice is ready when another contributor can identify what changed, reproduce the proof, and understand what remains unverified.

## Related pages

- [Unity Runtime Foundations](runtime-architecture.md)
- [Editor Safety](editor-safety.md)
- [Daily Workflow](../../collaboration/team-workflow.md)
