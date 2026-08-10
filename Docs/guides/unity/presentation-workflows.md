# Presentation workflows

Use this guide for UI, layout, models, animation, materials, VFX, sound
placement, and moment-to-moment feedback. Presentation communicates gameplay
state and makes actions readable. It should not become a second owner of the
gameplay rules it displays.

Read [Editor Safety](editor-safety.md) before structural scene, prefab,
material, or reference changes.

## Presentation starts from information

Before choosing an effect, identify what the player needs to know:

- what happened;
- what caused it;
- whether it helped or hurt;
- how important it is;
- what changed;
- what action is possible next.

For example, a wrong potion needs more than a large particle burst. The player
must understand that the potion was consumed, the disaster remains active, and
Panic increased. Color, sound, animation, and UI should reinforce the same
result instead of competing for attention.

## Keep the hierarchy readable

Organize objects by stable responsibility rather than creation order:

```text
Laboratory
  Environment
  Gameplay
  Systems
  UI
  Lighting
  Audio
```

Inside a prefab, use role-based names:

```text
BrewingStation
  InteractionPoint
  PotionSpawnPoint
  Visuals
    Cauldron
    Liquid
  Feedback
    BrewParticles
    AudioSource
```

Names such as `Cube`, `Image (1)`, `New Game Object`, and `test` force the next
contributor to inspect components to discover purpose. Keep temporary objects
out of the handoff.

## Build UI for different resolutions

Unity UI positions elements relative to anchors, pivots, parent rectangles, and
the Canvas scaler. A panel that looks correct at one Game view size can overlap
or leave the screen at another.

Use:

- anchors to describe which edges or region an element follows;
- pivots to describe the point used for position and scaling;
- layout groups for repeated rows, columns, and spacing;
- content-size behavior only where the content truly determines size;
- a Canvas scaler configured for the project's intended resolution behavior.

Example HUD intent:

- Panic belongs in a stable, always-visible screen region;
- score should remain legible without competing with immediate danger;
- the carried-item display should change when inventory state changes;
- the interaction prompt should appear near the player's focus and disappear
  when no target is valid.

Test at several aspect ratios and resolutions. Do not fix one layout by adding
arbitrary offsets that break the others.

## UI presents state and requests actions

Gameplay systems own current Panic, score, inventory, and run state. UI reads or
observes those owners and renders them.

```text
PanicSystem changes Panic
  -> PanicChanged event
  -> HUD updates fill and text
  -> warning presenter changes color or animation
```

The meter must not calculate the authoritative Panic value. A menu button may
request “start run” or “restart,” but the run-state owner decides whether the
transition is valid.

Keep button callbacks visible and reviewable. Confirm the assigned target and
method rather than connecting the first similarly named object in the inspector.

## Animation displays state

Decide which system owns timing before building an animation:

- Code owns gameplay state and validation.
- Animator parameters select the appropriate visual state.
- Animation clips control visual interpolation.
- Animation events are reserved for narrow, deliberate integration points.

If collecting an ingredient must happen immediately on accepted interaction, do
not make a fragile animation event the only owner of the inventory change. The
gameplay action can complete, then the animation presents pickup feedback.

Ask whether the player can interrupt the animation, whether input should remain
available, and what happens if the object is disabled or destroyed mid-clip.

## Import models deliberately

Before placing a model, check:

- source and Unity scale;
- forward and up axes;
- pivot position;
- mesh and material count;
- normal and tangent import;
- animation clips and rig type;
- polygon cost for the intended number of instances;
- whether the license and source permit project use.

A bad pivot makes placement and animation difficult. Incorrect scale spreads
compensating transforms through prefabs. Fix reusable import problems at the
asset or prefab boundary rather than applying unrelated scale values to every
scene instance.

## Separate visual meshes and gameplay colliders

The most accurate visual mesh is rarely the best collider. Gameplay colliders
should be stable, understandable, and as simple as the interaction needs.

For a station, use a clear physical collider and a deliberate interaction region
rather than relying on the decorative cauldron mesh. For a moving character,
keep collision ownership on the root while child visuals animate.

After changing scale, pivot, or hierarchy, inspect collider position in the
Scene view and exercise the interaction in Play Mode.

## Treat materials as shared assets

Editing one material asset changes every renderer that references it. Before a
change, identify whether the intended scope is one object, one prefab family, or
the whole visual language.

Create a separate material when one object needs a lasting difference. Avoid
runtime code that repeatedly accesses APIs which instantiate material copies
unless that allocation and lifetime are intentional.

Use names that state purpose, such as `Potion_Cooling_Glass`, rather than
`New Material 3`.

## Build a feedback hierarchy

Not every event deserves the same intensity:

| Priority  | Potion Panic examples                                                | Presentation goal                                                |
|-----------|----------------------------------------------------------------------|------------------------------------------------------------------|
| Primary   | Disaster escalation, wrong potion, high Panic, Game Over.            | Interrupt enough attention to change the player's next decision. |
| Secondary | Ingredient pickup, brew completion, correct resolution, score award. | Confirm cause and result without hiding another threat.          |
| Ambient   | Laboratory motion, magical particles, environmental sound.           | Support mood without competing with state information.           |

Use a combination of motion, color, sound, particles, UI, and timing only when
each channel adds information or feel. When every event flashes, shakes, and
plays a loud sound, the player loses the priority signal.

Consider comfort and accessibility. Avoid relying on color or sound alone for
critical state. Keep flashes, screen shake, motion, contrast, and text size
within readable limits and expose options where the effect could cause
discomfort.

## Worked example: brewing feedback

When the player brews a Cooling Potion:

1. The brewing system validates the carried Blue Mushroom and changes inventory
   to the resulting potion.
2. The carried-item UI updates to the Cooling Potion icon and name.
3. A short cauldron animation and blue particle effect confirm transformation.
4. A distinct sound marks completion.
5. The feedback ends quickly enough that the player can respond to the next
   disaster.

If validation fails because no ingredient is carried, success particles and
sound must not play. Present a smaller rejection cue tied to the actual reason.
Presentation follows the gameplay result rather than predicting it.

## Divide work by authority

Gameplay and system work usually owns rules, validation, runtime state, and
events. Presentation work owns hierarchy, layout, visual assets, animation,
effects, audio placement, and readability.

The boundary is shared where data crosses into presentation:

- prefab composition;
- button connections;
- Animator parameters and selected animation events;
- colliders and interaction regions;
- layers, tags, and sorting;
- UI presenters and feedback timing.

Agree on the state or event contract before both contributors edit the same
prefab or scene.

## Presentation handoff

Report:

- scenes, prefabs, materials, models, animation, UI, and `.meta` files changed;
- shared assets whose change affects several objects;
- resolutions and aspect ratios checked;
- Play Mode behavior and Console result;
- prefab overrides and inspector references requiring review;
- accessibility or readability limitations still open.

The handoff is complete when the presentation communicates the correct state,
survives the intended layouts and object reuse, and does not take ownership of
gameplay rules.

## Related pages

- [Editor Safety](editor-safety.md)
- [Game Design](../../project/game-design.md)
- [Daily Workflow](../../collaboration/team-workflow.md)
