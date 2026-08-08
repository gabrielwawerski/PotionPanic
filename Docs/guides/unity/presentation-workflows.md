# Unity presentation workflows

Use this guide for UI, layout, models, animation, materials, VFX, and other
presentation work. It owns presentation-side working methods; use
[Editor Safety](editor-safety.md) before structural asset or reference changes.

## Main Responsibility Areas

Presentation-side work often includes:

- UI layout
- menus
- HUD
- icons
- scene composition
- prefabs
- materials
- models
- animation
- VFX
- sound placement
- visual feedback

Even when a task includes little code, it can still break the game through bad
references, broken prefabs, or messy scene structure.

## Clean Hierarchy

A readable hierarchy makes collaboration much safer.

Avoid names such as:

- `Cube`
- `Image (1)`
- `New Game Object`
- `test`

Prefer grouped structure such as:

```text
Gameplay
Environment
UI
Systems
```

Use names that describe the object's role.

## UI Workflow

For UI tasks:

1. open the correct scene
2. find or create the relevant panel under the Canvas
3. use clear object names
4. use anchors and layout groups where possible
5. use TextMeshPro for text
6. turn reused UI into prefabs
7. connect button actions only when the target is understood
8. test in Play Mode

UI should display information and request actions. It should not own gameplay
rules.

## Animation Workflow

Before editing an animation, decide:

- is it visual-only?
- does it need to trigger gameplay timing?
- can the player interrupt it?
- does it lock movement?

Simple default:

- code decides the state
- the Animator displays the state
- animation events stay rare and deliberate

## Models, Scale, Pivot, and Colliders

Before importing or reusing a model, check:

- scale
- pivot point
- facing direction
- polygon cost
- materials
- collider needs

Visual mesh and gameplay collider should usually be treated separately.

## Materials

Changing a shared material can change many objects at once.

Before editing a material, ask:

- am I changing one object or many?
- should this object have its own material?
- should this be a new material asset?

Use clear names instead of generic defaults like `New Material`.

## Visual Feedback

Important events should be readable:

- ingredient picked up
- potion brewed
- disaster escalated
- correct potion applied
- wrong potion applied
- Panic reaching danger levels

Useful tools:

- animation
- sound
- particles
- color flash
- UI change
- icon
- outline

Feedback should clarify what happened, not create noise.

## Division Of Work

Main programmer usually owns:

- gameplay rules
- movement logic
- interaction rules
- disaster logic
- score and Panic systems
- scene-loading and game-state logic

Presentation-side teammate usually owns or supports:

- UI layout
- menus
- materials
- animation
- VFX
- model setup
- feedback polish

Shared responsibility:

- prefabs
- button connections
- animation events
- colliders
- layers and tags
- interaction readability

## Presentation handoff checklist

Your work should make the game clearer, more readable, and more usable without
making the project harder to maintain.

Aim for:

- clear visuals
- safe prefabs
- correct references
- readable hierarchy
- simple animations
- readable UI
- minimal accidental changes

## Related pages

- [Editor Safety](editor-safety.md)
- [Game Design](../../project/game-design.md)
- [Daily Workflow](../../collaboration/team-workflow.md)
