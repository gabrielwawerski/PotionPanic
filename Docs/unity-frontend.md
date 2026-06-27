# Unity Frontend, Models, Animation, and Scene Work Guide

## Purpose of This Guide

This guide is for the team member who will mostly work on the Unity-facing side of the project: scenes, prefabs, UI, models, animations, visual feedback, layout, and general presentation.

You do not need to become the main gameplay programmer immediately. The goal is to work safely inside Unity without accidentally breaking gameplay logic, prefab structure, scene references, or project organization.

---

## Main Area of Responsibility

Your work will usually involve:

```text
UI layout
menus
HUD
buttons
icons
scene composition
prefabs
materials
models
animations
VFX
sound placement
camera-facing polish
visual feedback
Inspector configuration
basic Unity setup
```

Even if you write less code, your work still affects the project heavily. In Unity, many important connections are configured in the Editor.

A broken prefab, missing reference, wrong layer, wrong collider, wrong animation transition, or changed scene object can break the game just as much as a code bug.

---

## The Unity Builder Mindset

When changing anything in Unity, think in three layers:

```text
Visual
- How does it look?

Functional
- Does it still work?

Structural
- Is it organized so the team can maintain it?
```

A button is not only a visual rectangle with text. It may also have:

```text
anchoring
layout rules
hover state
pressed state
disabled state
OnClick event
sound effect
animation
linked script reference
controller/keyboard navigation
```

A model is not only a mesh. It may also have:

```text
scale
pivot point
collider
material
animation rig
prefab connection
orientation
naming
folder location
```

Your work should improve the game visually without making the project harder to maintain.

---

## Scene Safety

Do not edit, delete, or rename random scene objects casually.

Before changing an object, check:

```text
Is this object part of a prefab?
Is this object referenced by a script?
Is this object used by UI, animation, camera, or gameplay?
Is this object present only in this scene, or reused elsewhere?
```

Be especially careful with objects named like:

```text
GameController
SceneController
Player
EnemySpawner
Canvas
EventSystem
MainCamera
AudioManager
InputSystem
```

Renaming or deleting objects can break:

```text
serialized references
animation bindings
Timeline bindings
button events
prefab links
script lookups
scene logic
```

If you do not know what an object does, do not delete or rename it without checking.

---

## Prefabs as the Default for Reusable Objects

When something will be reused, it should usually become a prefab.

Good prefab candidates:

```text
ingredient pickup
enemy
cauldron
potion projectile
UI panel
button style
health bar
damage popup
door
decorative prop
animated object
```

Avoid copying the same object manually across scenes. Instead:

```text
create one prefab
reuse prefab instances
apply intentional prefab changes
override only scene-specific values
```

Before applying prefab changes, check exactly what you are applying. Do not press **Apply All** blindly.

---

## Prefab Overrides

A prefab instance can have local overrides.

Usually safe overrides:

```text
position
rotation
scene-specific references
unique text labels
minor visual variation
```

Risky overrides:

```text
removing required components
changing script references
changing collider setup
changing animator controller
changing root hierarchy
changing shared material unintentionally
```

When unsure, use a prefab variant instead of changing the base prefab.

---

## Clean Scene Hierarchy

A messy hierarchy makes the project harder to work in.

Avoid:

```text
Cube
Cube (1)
Sphere
New Game Object
Image
Image (1)
Panel
test
asdf
```

Prefer grouped structure:

```text
Gameplay
  Player
  Enemies
  Pickups
  Interactables

Environment
  Floor
  Walls
  Props
  Lighting

UI
  Canvas
  EventSystem

Systems
  GameController
  AudioService
  SceneLoader
```

Use empty parent objects to group related objects.

Use names that describe the object’s role:

```text
Cauldron_Main
IngredientPickup_Mushroom
EnemySpawnPoint_North
HUD_HealthBar_Player
Button_BrewPotion
Panel_BrewingMenu
```

Temporary objects should either be removed or clearly marked.

---

## UI Work

UI should work across screen sizes and be readable under gameplay pressure.

Learn and use:

```text
Canvas
RectTransform
anchors
pivots
layout groups
Content Size Fitter
Canvas Scaler
TextMeshPro
buttons
panels
UI prefabs
```

For each UI screen, check:

```text
Does it look correct at 16:9?
Does it survive smaller resolutions?
Does text fit?
Are buttons readable?
Are important elements aligned?
Is there enough contrast?
Can the player tell what is clickable?
What happens when a button is disabled?
```

Avoid placing everything manually by eye with fixed positions unless it is a temporary prototype.

---

## UI as Presentation, Not Gameplay Logic

UI can request actions, but it should not own the actual rules.

Bad structure:

```text
Brew button checks ingredients,
removes ingredients,
creates potion,
updates inventory,
closes menu.
```

Better structure:

```text
Brew button asks PotionBrewer to brew.
PotionBrewer validates ingredients.
PotionBrewer consumes ingredients.
PotionBrewer creates the potion.
UI displays success or failure.
```

Your UI responsibilities are usually:

```text
display information
send button clicks
show and hide panels
play UI animations
make states readable
show disabled/available actions
```

Gameplay rules should stay in gameplay systems.

---

## Animation Work

Animations should support gameplay state. They should not secretly control important gameplay rules unless the team intentionally designs it that way.

Before creating or editing an animation, clarify:

```text
Is this purely visual?
Does this need to trigger gameplay timing?
Does this need an animation event?
Can the player interrupt it?
Does it lock movement?
What state should the object return to afterward?
```

Common animation risks:

```text
animation changes object position unexpectedly
animation overrides scale
animation disables important objects
animation event calls a missing method
transition never exits
loop is enabled when it should not be
root motion is enabled accidentally
wrong Avatar or rig assigned
```

Simple starting rule:

```text
Code decides the state.
Animator displays the state.
Animation events are used sparingly.
```

---

## Models, Scale, Pivot, and Colliders

Before importing or using a model, check:

```text
Is the scale correct?
Is the pivot point useful?
Does it face the correct direction?
Is it too high-poly for the game?
Are materials assigned correctly?
Does it need a collider?
Should the collider be simple instead of mesh-based?
Does it need to be a prefab?
```

Visuals and collision should usually be treated separately:

```text
Visual mesh
- what the player sees

Collider
- what the game uses for physics and interactions
```

Avoid complex MeshColliders unless they are actually needed.

A model that looks correct but has a bad pivot, bad scale, or wrong collider will create repeated Unity problems.

---

## Materials

Changing a shared material can affect every object that uses it.

Before editing a material, ask:

```text
Am I changing one object or many objects?
Should this object have its own material?
Should this be a new material asset?
Is this a temporary color test?
```

Use clear material names:

```text
MAT_Cauldron_Iron
MAT_Potion_Fire
MAT_Ingredient_Mushroom_Red
MAT_UI_Button_Default
```

Avoid:

```text
New Material
Material 1
test_mat
red
```

---

## Visual Feedback

Visual feedback is part of gameplay clarity.

Important events should be readable:

```text
player takes damage
enemy takes damage
enemy dies
ingredient is picked up
potion is brewed
potion is ready
cauldron is usable
button is disabled
cooldown is active
attack is charging
danger area is visible
```

Useful feedback tools:

```text
animation
sound
particle effect
screen shake
color flash
UI change
icon
progress bar
outline or highlight
floating text
```

Feedback should clarify what happened. It should not become visual noise.

---

## Layers, Tags, Sorting, and Collision

Layers and tags can affect gameplay logic, raycasts, collisions, camera visibility, lighting, and UI.

Do not casually change:

```text
Layer
Tag
Sorting Layer
Order in Layer
Collision settings
Camera culling mask
```

Examples:

```text
If an interactable is on the wrong layer, the player may not detect it.
If UI sorting order is wrong, a menu may appear behind another panel.
If an enemy has the wrong tag, targeting logic may ignore it.
```

When changing layers or tags, mention it in the task or commit.

---

## Inspector References

Many Unity scripts depend on fields assigned in the Inspector.

Missing fields like these may indicate a bug:

```text
None (Transform)
None (Animator)
None (Health)
None (Button)
None (TextMeshProUGUI)
```

Do not randomly drag objects into fields unless you know what the field expects.

When assigning references, verify:

```text
Is this the correct object?
Is it from this prefab or this scene?
Should it reference a child object?
Should it reference a shared asset?
Will this still work if the prefab is reused?
```

---

## Play Mode Discipline

Changes made during Play Mode usually disappear after exiting Play Mode.

Use Play Mode for testing, not for permanent editing.

Safe habit:

```text
enter Play Mode to test
notice useful values
copy or write them down
exit Play Mode
apply the values outside Play Mode
test again
```

Always check whether Unity is currently in Play Mode before editing important objects.

---

## Git Hygiene

Unity projects are easy to damage with careless version control.

Before starting work:

```text
pull latest changes
confirm the correct branch
check existing changed files
understand what task you are changing
```

Before committing:

```text
review changed files
avoid unrelated changes
remove temporary test assets
make sure scenes open without errors
make sure prefabs are not accidentally broken
write a clear commit message
```

Be careful with:

```text
.unity scene files
.prefab files
.mat files
.anim files
.controller files
.meta files
ProjectSettings files
```

Do not delete `.meta` files manually.

---

## Changes That Need Coordination

Ask before making broad structural changes.

Examples:

```text
replacing the player model
changing scene hierarchy root structure
renaming core objects
changing input setup
changing camera setup
changing UI architecture
importing a large asset pack
changing render pipeline settings
changing physics layers
changing project settings
```

Small visual changes are usually safe. Structural changes need coordination.

---

## Skills to Learn First

Prioritize these Unity skills:

```text
Scene view navigation
GameObject hierarchy
Inspector
Transform tools
prefabs and prefab variants
materials
basic lighting
Canvas and UI layout
TextMeshPro
Animator basics
Animation clips
importing models
colliders
layers and tags
Play Mode testing
Git workflow
```

You do not need to master everything immediately. Focus on the parts you touch often.

---

## Safe UI Workflow

For UI tasks:

```text
pull the latest project version
open the correct scene
find or create the relevant UI panel under Canvas
use clear object names
use anchors and layout groups where possible
use TextMeshPro for text
turn reused UI into prefabs
connect button OnClick only when the target is known
test in Play Mode
check other resolutions when relevant
exit Play Mode before permanent edits
review changed files before committing
```

UI is successful when it is clear, stable, and does not own gameplay rules.

---

## Safe Model or Prop Workflow

For model or prop tasks:

```text
import the model into the correct folder
check scale
check orientation
check pivot
assign or create cleanly named materials
add a simple collider if needed
create a prefab
place prefab instances in the scene
test collision and interactions if relevant
review prefab and material changes before committing
```

Do not scatter raw imported assets randomly through the project.

---

## Safe Animation Workflow

For animation tasks:

```text
identify which object or prefab needs animation
confirm whether the animation is visual-only or gameplay-timed
create or edit animation clips
set up Animator states clearly
avoid unnecessary complex transitions
check loop settings
check root motion
test transitions in Play Mode
verify the object returns to the correct state
commit only relevant animation, controller, and prefab files
```

Keep Animator graphs readable. A tangled Animator is hard to debug.

---

## Questions to Ask Before Changing Something

Useful checks:

```text
Is this visual-only, or does it affect gameplay?
Is this object part of a prefab?
Is this value shared by many objects?
Is this referenced by a script?
Will this work in another resolution?
Will this work if the scene reloads?
Am I changing the base prefab or only this instance?
Am I currently in Play Mode?
Will this create messy Git conflicts?
```

These questions prevent most Unity-side accidents.

---

## Division of Work

### Main Programmer Usually Owns

```text
gameplay rules
player movement logic
combat rules
inventory rules
brewing logic
save/load
game state
scene loading
spawning logic
system architecture
```

### Frontend / Unity-Side Teammate Usually Owns or Helps With

```text
UI layout
HUD readability
menus
icons
models
materials
animations
VFX
scene dressing
prefab visual setup
feedback polish
basic Inspector setup
```

### Shared Responsibility

```text
prefabs
button connections
animation events
colliders
layers and tags
camera feel
interaction readability
```

Shared responsibility requires communication. These areas sit between code and scene work.

---

## Definition of Done

A frontend, model, UI, or animation task is done when:

```text
it is in the correct folder
it has clear names
it works in Play Mode
it creates no console errors
it does not break prefab links
it does not break scene references
it is readable in the hierarchy
it is committed on the correct branch
the commit contains only relevant files
the main programmer can understand what changed
```

A task is not done just because it looked correct once.

---

## Common Mistakes to Avoid

Avoid:

```text
editing during Play Mode and losing changes
renaming core objects casually
deleting .meta files
using Apply All on prefabs without checking
changing shared materials accidentally
leaving objects named Cube or Image
placing UI with fixed positions only
making UI own gameplay rules
using complex MeshColliders everywhere
changing layers or tags without telling anyone
committing huge unrelated files
importing large asset packs directly into the main project
```

These mistakes are common but preventable.

---

## Final Rule

Your work should make the game clearer, more readable, and more usable without making the project harder to maintain.

Think:

```text
clear visuals
clean hierarchy
safe prefabs
correct references
simple animations
readable UI
minimal accidental changes
good Git hygiene
```

The goal is not only to make things look better. The goal is to make the game easier to understand for the player and safer to build for the team.
