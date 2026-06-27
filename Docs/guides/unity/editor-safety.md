# Unity Editor Safety Guide

Use this guide before changing scenes, prefabs, project settings, layers, tags,
or inspector references.

## Scene Safety

Do not edit, delete, or rename random scene objects casually.

Before changing an object, check:

- Is this object part of a prefab?
- Is it referenced by a script?
- Is it used by UI, animation, camera, or gameplay?
- Is it reused elsewhere?

Be extra careful with objects such as:

- `Player`
- `Main Camera`
- `GameManager`
- `EventSystem`
- `Canvas`
- `AudioManager`

## Prefabs As The Default For Reuse

When something will be reused, it should usually become a prefab.

Good prefab candidates:

- ingredient pickups
- stations
- disaster objects
- HUD panels
- reusable buttons
- VFX objects

Before applying prefab changes, check exactly what you are applying. Do not hit
`Apply All` blindly.

## Safe Prefab Overrides

Usually safe:

- position
- rotation
- scene-specific references
- unique text labels

Risky:

- removing required components
- changing script references
- changing colliders
- changing animator controllers
- changing the shared root hierarchy

When unsure, use a prefab variant instead of mutating the base prefab.

## Inspector References

Many Unity bugs come from incorrect inspector wiring.

When assigning a reference, verify:

- is this the correct object?
- is it from this scene or this prefab?
- should it point to a child object instead?
- will it still work if the prefab is reused?

Do not drag random objects into `None` fields just to remove warnings.

## Layers, Tags, Sorting, and Collision

Changing these can affect:

- raycasts
- collisions
- camera visibility
- lighting
- UI order

Do not casually change:

- layer
- tag
- sorting layer
- order in layer
- collision settings
- camera culling mask

Mention these changes explicitly in the task or review summary.

## Play Mode Discipline

Use Play Mode for testing, not for permanent editing.

Safe habit:

1. enter Play Mode to test
2. note useful values
3. exit Play Mode
4. apply the values outside Play Mode
5. test again

Always check whether Unity is currently in Play Mode before editing important
objects.

## Git Hygiene For Unity Files

Before committing:

- review changed files
- remove temporary test assets
- make sure scenes open without errors
- make sure prefabs are not accidentally broken
- make sure the commit contains only relevant files

Be careful with:

- `.unity`
- `.prefab`
- `.mat`
- `.anim`
- `.controller`
- `.meta`
- `ProjectSettings/*`

Do not delete `.meta` files manually.

## Changes That Need Coordination

Announce before making broad structural changes such as:

- replacing the player model
- renaming core objects
- changing camera setup
- changing input setup
- changing UI architecture
- changing render pipeline settings
- changing physics layers
- changing project settings

Small visual polish changes are usually safer. Structural changes need
coordination.

## Done Means Safe

A Unity-side task is done only when:

- it works in Play Mode
- it creates no relevant Console errors
- it does not break prefab links
- it does not break scene references
- it is readable in the hierarchy
- the commit contains only relevant files

Looking correct once is not enough.
