# Unity Coding And Implementation Guide

Use this guide when turning a feature into code, tests, and a safe delivery
sequence.

## Start With The Smallest Playable Loop

Do not begin with the full final architecture.

Ask:

> What is the smallest playable version of this system?

For Potion Panic, examples look like:

- one room
- one ingredient
- one brewing station
- one potion
- one disaster
- one Panic meter
- one fail state

Expand only after that loop works.

## Concrete Use Cases Before Abstractions

Do not build large generic frameworks for imagined future needs.

Usually too early:

- universal interaction frameworks with several unused layers
- custom dependency injection frameworks
- large inventory systems before the one-item rule is working

Better early:

- `IInteractable`
- `IngredientStation`
- `BrewingStation`
- `DisasterInstance`

Build for the next few known features, not for fifty hypothetical ones.

## Keep Files Honest

A class name should explain why it exists.

Weak names:

- `Manager`
- `Handler`
- `Thing`
- `ObjectScript`

Better names:

- `PlayerController`
- `InteractionController`
- `DisasterManager`
- `PanicSystem`
- `InteractionPromptView`

If a file name is vague, its responsibility probably is too.

## Avoid Update Dumps

`Update()` is easy to misuse.

Bad pattern:

```text
one method reads input, polls UI, checks inventory, scans the scene, and saves
state every frame
```

Prefer:

- input components read input
- movement components move
- timers own timer logic
- UI reacts to state changes
- systems react to events when possible

## Failure Cases Before Code

Before implementing a system, ask:

- What if a reference is missing?
- What if the object is destroyed?
- What if the action is triggered twice?
- What if the scene reloads?
- What if this happens during pause?
- What if the player inventory is full?

Thinking through failure cases early prevents patchy fixes later.

## Debug Visibility

Add small debug hooks early.

Useful examples:

- current game state display
- current carried item display
- current interactable target
- disaster spawn count
- detection radius gizmos
- state transition logs

Projects without visibility quickly turn into guesswork.

## Practical Dependency Rules

Prefer:

- serialized fields for explicit scene wiring
- same-object `GetComponent` lookups in `Awake`
- inspector-assigned references for clear ownership
- events for fan-out reactions

Avoid defaulting to:

- `FindObjectOfType`
- `GameObject.Find`
- hidden scene-name dependencies

## Implementation Checklist

Before coding a feature, answer:

- What should the player experience?
- What data exists?
- Who owns that data?
- What scene objects are involved?
- What components are needed?
- How do those components communicate?
- What should not know about what?
- What failure cases exist?
- What is the smallest playable version?

## Testing Guidance

Choose tests by behavior:

- `EditMode` for pure logic and small helpers
- `PlayMode` for scene or runtime integration

Before calling work complete:

1. wait for Unity compilation to finish
2. open the affected scene
3. press Play
4. check the Console
5. run the relevant test suite if gameplay code or tests changed

## Final Implementation Rule

A feature is not done because the code exists.

It is done when:

- the target behavior works
- the scene still runs
- the Console stays clean for the task
- the implementation fits the current milestone
- the other collaborator can understand the change
