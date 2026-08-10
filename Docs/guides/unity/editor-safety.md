# Editor safety

Use this guide before changing scenes, prefabs, inspector references, shared
materials, layers, tags, project settings, or packages. These files are not
opaque binaries: Unity serializes object identities, component fields, and asset
references. That makes them reviewable, but overlapping edits can still be
difficult or unsafe to merge.

Use [Unity Coordination](../coordinated-leasing.md) for developer identity,
scene claims, save conflicts, and Coordination window actions.

## How Unity stores project relationships

A scene or prefab records GameObjects, components, serialized fields, and links
to other assets. Asset links use GUIDs from `.meta` files rather than relying
only on a visible filename.

```text
Player prefab
  -> serialized PlayerController component
  -> serialized InputActionReference
  -> GUID from the input asset's .meta file
```

This explains several safety rules:

- Renaming an asset inside Unity can preserve its GUID and references.
- Deleting or regenerating its `.meta` file creates a different identity and can
  break every reference to the old GUID.
- Replacing a YAML conflict without understanding object IDs can connect the
  right field to the wrong object even when the file still parses.
- An apparently small inspector change can modify a shared prefab used in many
  scenes.

## Inspect before editing

Before changing an object or asset, identify:

1. whether it belongs to the scene or a prefab instance;
2. whether the field is inherited from a base prefab or overridden locally;
3. which scripts, animations, UI, cameras, or other assets reference it;
4. whether another contributor is editing the same shared file;
5. which behavior and verification will prove the change is safe.

Objects such as `Player`, `Main Camera`, `GameManager`, `EventSystem`, `Canvas`,
and shared audio or lighting systems often have many dependants. A rename,
component removal, or hierarchy move can break code, animation paths, serialized
references, or scene assumptions.

## Scenes compose the current level

Use the shared scene for scene-specific composition: room layout, lighting,
camera placement, spawn locations, and the objects needed to run the level. Use
prefabs for repeated or independently owned units.

Before editing a shared scene:

1. update your branch and announce the path;
2. reserve the scene when it matches the active Coordination rule;
3. open the intended scene and confirm its path;
4. make the narrow change;
5. test before and after saving;
6. review the `.unity` diff and any new `.meta` files.

Do not use `testscene.unity` as a substitute for the current shared milestone
scene unless the task explicitly says so. A successful test in another scene
does not prove the configured gameplay scene works.

## Prefabs define reusable compositions

Make an object a prefab when it should be instantiated, reused across scenes, or
reviewed independently from one scene. Good candidates include ingredient
stations, disasters, pickups, reusable UI panels, and visual effects.

### Understand overrides

A prefab instance can override values inherited from its asset. Overrides are
useful for scene position, a local label, or an intentional scene-specific
reference. They become risky when the instance silently differs in required
components, colliders, scripts, or child hierarchy.

Before applying overrides:

- inspect the override list;
- decide which changes belong to every instance;
- revert accidental changes individually;
- apply only the intended fields or components;
- reopen another instance and verify the shared result.

`Apply All` is unsafe when the instance contains experimental or scene-specific
changes. A prefab variant is appropriate when a group of objects shares a stable
base but needs a deliberate, reusable set of differences.

## Inspector references are part of the implementation

A serialized reference is a dependency. Treat assigning it with the same care as
calling a method in code.

Check:

- Does the field point to the correct component rather than only the correct
  GameObject name?
- Is a scene object being assigned to a prefab asset that must work in other
  scenes?
- Will duplicating or instantiating the prefab preserve the relationship?
- Does the reference remain valid when a child is renamed or moved?
- Is the field required, and does the component report a clear setup error when
  it is missing?

Do not drag an arbitrary object into a `None` field merely to remove a warning.
That hides the setup defect until a less obvious behavior fails.

## `.meta` files preserve asset identity

Every tracked Unity asset and folder normally has a corresponding `.meta` file.
Keep the asset and its `.meta` file together when moving or deleting it. Prefer
moving and renaming inside Unity so the editor preserves the relationship.

Review new `.meta` files when adding assets. A missing `.meta` file can cause
Unity to generate a new GUID on another machine. A duplicated `.meta` file can
make two paths claim the same identity.

Never delete `.meta` files as a generic cleanup step.

## Play Mode changes are temporary

Unity can let you edit values while the game is running. Most scene and
component changes made in Play Mode are discarded when Play Mode ends.

Safe workflow:

1. enter Play Mode to observe behavior;
2. note useful values separately;
3. exit Play Mode;
4. apply the intended values in edit mode;
5. run the test again.

Before a structural edit, check the Play button and editor tint. A value that
looked correct once is not evidence that it was serialized into the project.

## Shared materials affect every user

Several renderers can reference one material asset. Editing that material
changes every renderer that uses it.

Before changing color, texture, shader, transparency, or render settings:

- find whether the material is shared;
- decide whether the change belongs to every user;
- create a separate material when only one object should differ;
- avoid creating accidental per-instance material copies at runtime.

The scene may look correct while an unrelated prefab or UI element has changed
elsewhere. Inspect the material asset and known users, not only the selected
object.

## Layers, tags, sorting, and collision are project contracts

These settings connect systems that may not reference each other directly:

- layers affect physics queries, collisions, and camera culling;
- tags support lookups and classification;
- sorting layers and order affect visual overlap;
- the collision matrix enables or disables entire categories of interaction.

A change can make a raycast miss, a camera hide an object, or two colliders stop
interacting without any C# diff. Record the intended relationship in the task,
announce the shared settings change, and test every affected path.

## Project settings and packages have repository-wide effects

Announce before changing `ProjectSettings/`, `Packages/manifest.json`, or
`Packages/packages-lock.json`.

Examples include Input System configuration, build scenes, render pipeline,
quality, physics, tags, layers, package versions, and editor serialization.
These changes can trigger imports, alter generated solution files, or change
behavior for every contributor.

Do not edit a package lock entry by hand to make a diff disappear. Change the
manifest through the intended package workflow and let Unity resolve a
consistent lockfile.

## Review Unity files in Git

Before committing, inspect the complete change set and the exact files or hunks
selected for the commit. The IDE diff is the normal human review path.

### Rider or WebStorm

1. Open the Commit tool window.
2. Inspect every changelist and unversioned file, including files that will not
   be committed.
3. Select only the task's files and hunks.
4. Open the diff for every selected scene, prefab, `.meta`, setting, package,
   material, or asset file.
5. Confirm the selected commit contains no generated or unrelated files.

### VS Code

1. Open Source Control and inspect both **Changes** and **Staged Changes**.
2. Stage only the task's files or selected ranges.
3. Open every staged file in the diff editor.
4. Confirm the staged set contains the expected Unity asset and its `.meta`
   file when applicable.
5. Leave unrelated and generated files unstaged.

<details>
<summary>PowerShell diagnostic fallback</summary>

```powershell
git status --short
git diff -- Assets ProjectSettings Packages
```

</details>

Check for:

- unexpected scenes, prefabs, settings, or materials;
- temporary test objects and assets;
- missing or extra `.meta` files;
- broad prefab override application;
- references to deleted GUIDs;
- generated folders or IDE project files that should not be tracked.

Text diffs can be large because Unity records object graphs. Review the type of
change, affected object names, components, references, and file set rather than
approving a diff because it is hard to read.

A clean text diff does not prove that the Unity object graph is valid. After a
serialized conflict or broad asset change, open the result in Unity, wait for
import and compilation, inspect references and overrides, and run the affected
behavior.

## Recover from a merge conflict

For a scene, prefab, project setting, or other important serialized asset:

1. Stop and preserve both sides of the conflict.
2. Identify the contributors and intended changes.
3. Decide which serialized objects or settings must survive.
4. Resolve together or rebuild the smaller change from a known-good version.
5. Open the result in Unity and wait for import and compilation.
6. Inspect references and prefab overrides.
7. Run the affected behavior immediately.
8. Review the resolved diff before committing.

Do not choose “ours” or “theirs” for an entire Unity file until you know which
valid work that discards. A file that opens can still contain incorrect
references or missing objects.

## Changes that require explicit coordination

Always announce structural work such as:

- replacing or reorganizing the player;
- renaming core objects;
- changing camera or input setup;
- changing UI architecture;
- changing render pipeline or quality settings;
- changing physics layers or collision rules;
- applying broad base-prefab changes;
- editing project or package configuration.

The current Coordination rule automatically covers scenes below
`Assets/Scenes/`. Prefabs and project-wide settings still require the manual
announcement unless a verified rule is added. See
[Unity Coordination](../coordinated-leasing.md) for the exact claim and save
behavior.

## Safe handoff

Report:

- every scene, prefab, material, setting, package, and `.meta` file changed;
- whether the path was coordinated or manually announced;
- the prefab overrides and inspector references the reviewer should inspect;
- Play Mode and Console results;
- relevant EditMode or PlayMode results;
- any manual gate that remains unperformed.

## Related pages

- [Unity Coordination](../coordinated-leasing.md)
- [Daily Workflow](../../collaboration/team-workflow.md)
- [Presentation Workflows](presentation-workflows.md)
