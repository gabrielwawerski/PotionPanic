# MVP scope

Use this document for locked MVP decisions, milestone sequencing, and hard scope
boundaries.

It owns binding MVP scope, tuning, and delivery order. It does not replace task
acceptance criteria, implementation plans, or the player-experience intent
in [Game Design](game-design.md).

## How to read this document

This page separates three kinds of truth:

- **Current repository state:** what exists in the checkout now.
- **Accepted MVP target:** behavior and content the team has approved.
- **Delivery sequence:** the dependency order for reaching that target.

An accepted target is binding for implementation decisions, but it is not
evidence that the feature already exists. Use the board and tickets for current
status.

## Current Repo Note

The current Unity scaffold still uses `Assets/Scenes/SampleScene.unity` as the
shared prototype scene.

Milestone 1 is expected to rename or replace that shared gameplay scene as
`Laboratory.unity`. Until then:

- `SampleScene.unity` is the shared smoke-test scene
- `testscene.unity` is not the milestone scene unless a task explicitly says so

## Locked MVP Decisions

### Run structure

- The MVP uses one gameplay scene: `Laboratory.unity`.
- `MainMenu`, `Playing`, `Paused`, and `GameOver` are in-scene run states.
- Starting a run resets gameplay systems and spawns the first disaster after
  `3 seconds`.
- Restart reloads `Laboratory.unity` for a clean reset.

### Milestone 1 camera and movement

- Camera is fixed orthographic top-down.
- Movement uses a `CharacterController`, not a `Rigidbody`.
- Movement is world-aligned on the X/Z axes.
- Milestone 1 does not include mouse look, sprint, jump, or camera follow.

### Wrong-potion behavior

- The wrong potion is consumed.
- The disaster stays active.
- The player immediately takes `+10 Panic`.
- Wrong-potion use does not award score.

### Default disaster tuning

Stages 1-3 use the same default tuning for all three MVP disasters:

- `1.5 Panic/sec` while active
- escalation at `20 seconds`
- `3.0 Panic/sec` after escalation

Stage 4 uses:

- `1.875 Panic/sec` while active
- escalation at `15 seconds`
- `3.75 Panic/sec` after escalation

Correct resolution immediately reduces Panic by `10`.

### Difficulty progression

| Stage   | Run time  | Max active disasters | Spawn interval |
|---------|-----------|----------------------|----------------|
| Stage 1 | 0:00-0:59 | 1                    | 12 seconds     |
| Stage 2 | 1:00-1:59 | 2                    | 10 seconds     |
| Stage 3 | 2:00-2:59 | 3                    | 8 seconds      |
| Stage 4 | 3:00+     | 3                    | 6 seconds      |

Additional rules:

- stage progression is time-based, not score-based
- if the active-disaster cap is full, no spawn backlog is queued
- disaster selection uses equal weighting across enabled disaster types

### Score rules

- `+100` for each resolved disaster
- `+50` if the disaster is resolved within `10 seconds` of spawning
- `+1` score for each full second survived
- combo scoring is not part of MVP

## MVP Checklist

### Core gameplay

- player movement
- interaction system
- brewing station
- ingredient collection

### Content

- 3 ingredients
- 3 potions
- 3 disasters

### UI

- main menu
- Panic meter
- score display
- game over screen

### Polish

- basic sounds
- basic particles
- basic animations

## Delivery map

| Milestone | Outcome                                                 |
|-----------|---------------------------------------------------------|
| 1         | Movement, camera, collision, and a laboratory blockout. |
| 2         | One reusable interaction pattern.                       |
| 3         | One ingredient-to-potion loop.                          |
| 4         | One complete disaster.                                  |
| 5         | A repeatable vertical slice from start to failure.      |
| 6         | The remaining MVP content on the same systems.          |
| 7         | Difficulty pressure and scoring.                        |
| 8         | Menus and complete run flow.                            |
| 9         | Audio and visual feedback.                              |
| 10        | Polish, balancing, and bug fixing.                      |

The detailed milestones below preserve the delivery commitments for each stage.
Use the board for current task status.

### Why the order matters

Each milestone creates evidence or reusable behavior needed by the next one:

```text
movement and room
  -> reusable interaction
  -> ingredient and brewing loop
  -> one complete disaster
  -> repeatable run and fail state
  -> remaining content on proven systems
  -> difficulty and score
  -> menus and full run flow
  -> presentation feedback
  -> final balance and stability
```

Moving directly to full content would multiply unproven prefabs and data.
Building menus before the run loop works would polish a flow that may still
change. The sequence keeps each milestone playable and makes later work reuse a
verified foundation.

## Milestones

### Milestone 1: movement and camera

Goal:

- establish the player control foundation

Deliverables:

- fixed top-down camera
- WASD player movement
- player collision
- basic laboratory blockout
- responsive movement feel

### Milestone 2: interaction system

Goal:

- allow the player to interact with lab objects through one reusable pattern

Deliverables:

- single interact key
- nearby interactable detection
- interaction prompt
- reusable interactable abstraction

### Milestone 3: ingredient to potion loop

Goal:

- create the first complete non-disaster gameplay loop

Deliverables:

- one ingredient station
- brewing station
- one carried ingredient or one carried potion
- carried item UI

### Milestone 4: first disaster

Goal:

- make the game technically playable with one disaster

Deliverables:

- Overheated Cauldron
- Panic increase while active
- Cooling Potion resolution
- wrong-potion penalty

### Milestone 5: core game loop

Goal:

- make the vertical slice repeatable from start to fail state

Deliverables:

- recurring disaster spawning
- Game Over
- restart flow
- basic score display

### Milestone 6: full MVP content

Goal:

- add the remaining ingredients, potions, and disasters using the same systems

### Milestone 7: difficulty and scoring

Goal:

- add replay pressure and scoring

### Milestone 8: menus and run flow

Goal:

- wrap the game in a complete start-to-finish structure

### Milestone 9: audio and visual feedback

Goal:

- improve readability and satisfaction

### Milestone 10: polish, balancing, and bug fixing

Goal:

- turn the prototype into a finished small game

## Hard Scope Boundaries

The following are out of scope until MVP is complete:

- multiplayer
- quest systems
- dialogue systems
- story campaign
- crafting trees
- skill trees
- large inventories
- multiple rooms
- open-world areas
- procedural generation
- combat systems

If a new idea does not help the current milestone become playable, add it to
the [board](../board.md) instead of implementing it now.

Hard boundaries protect completion capacity. They are not claims that the
excluded ideas are bad; they prevent networking, authored content, progression,
or world expansion from adding new production disciplines before the core game
can ship.

## Post-MVP Candidates

Possible expansion ideas after the MVP ships:

- additional laboratories
- new ingredient types
- new disaster families
- endless mode
- daily challenge mode
- laboratory upgrades
- cosmetic unlocks
- boss disasters
- achievements

## Definition of Done

Potion Panic is complete when a player can:

- launch the game
- play from start to finish
- resolve disasters
- lose by reaching 100 Panic
- earn a score
- restart and play again

without gameplay-breaking bugs.

This is the game-level finish line. Individual milestones and tickets need their
own narrower acceptance criteria and verification evidence.

## Related pages

- [Game Design](game-design.md)
- [Potion Panic Runtime Contract](technical-architecture.md)
- [Daily Workflow](../collaboration/team-workflow.md)
- [Active Plans](../plans/)
