# Team Workflow

Version: 2.0
Status: Active workflow guide
Scope: Two-person beginner/intermediate Unity team
Primary repo: Potion Panic

Use this guide for day-to-day collaboration. Use the `project/` docs for game
truth and the docs board for task execution and planning history.

## Daily Flow

At the start of a work session:

1. Pull the latest `master`.
2. Confirm the current milestone in `Docs/project/mvp-scope.md`.
3. Pick one small task from `To do`.
4. Say what you are working on and which files, scenes, or prefabs you expect
   to touch.
5. Create a short-lived feature branch.
6. Build and test the change locally.
7. Commit only the related files.
8. Push the branch.
9. Review or test before merging.
10. Write down what changed, what was tested, and what is still risky.

The goal is not to write the most code. The goal is to keep the game playable
and keep both people aware of the project state.

## Non-Negotiable Rules

1. Do not work directly on `master`.
2. Keep `master` compiling, runnable, and playable.
3. Communicate before editing shared Unity files.
4. Work in small tasks.
5. Test before merging.
6. Write down blockers, known issues, and unfinished work.
7. Put new ideas on the board instead of interrupting the current milestone.
8. Prototype with placeholders before polishing.
9. Avoid two people editing the same Unity scene at the same time.
10. Stop feature work when the project is unstable.

If unsure what to do next, ask:

> Does this bring us closer to a playable game?

If yes, consider it for the current milestone. If no, add a board task for it.

## Repository Setup

Before taking a task on a new machine, complete
`Docs/onboarding/getting-started.md` end to end.

Required setup:

- Unity version matches `ProjectSettings/ProjectVersion.txt`.
- Current recorded Unity version is `6000.5.1f1`.
- `PotionPanic.sln` opens in Rider on both machines.
- The project opens on both machines.
- The current shared prototype scene enters Play Mode on both machines.
- The current shared prototype scene is `Assets/Scenes/SampleScene.unity`.
- `Assets/Scenes/testscene.unity` is not the shared milestone scene unless the
  task explicitly says so.
- Visible Meta Files are enabled.
- Force Text serialization is enabled.
- `.gitignore` is present at the repo root.
- `.gitattributes` is present at the repo root.
- Git LFS is installed locally with `git lfs install`.
- Both developers can pull the repository and run the project.
- Both developers understand which generated folders must not be committed.

Do not commit generated Unity cache folders:

```text
Library/
Temp/
Obj/
Build/
Builds/
Logs/
UserSettings/
```

If one appears in Git status, stop and check `.gitignore` before committing.

## Ownership

Each major area should have a primary owner. Ownership means responsibility for
quality, organization, consistency, and decisions in that area.

Ownership does not mean exclusive access. The other developer can help, review,
test, or change files when needed. If someone needs to work inside another
person's area, they should say so first.

Recommended split:

| Area | Primary owner | Typical work |
| --- | --- | --- |
| Gameplay / systems | Developer A | Player controls, interactions, game state, scoring, disaster logic, save/load, technical debugging |
| World / UX / presentation | Developer B | Scene blockout, environment layout, UI, menus, audio, VFX, placeholder art, playtest notes |

This split is a default. Adjust it when a milestone needs a different division
of work.

## Milestones and Scope

Only one major milestone should be active at a time.

Do not start future milestone work before the current milestone is playable. A
milestone is complete when:

- the feature works in Play Mode
- the project remains playable
- the other developer can pull and test it
- the next milestone is not blocked by broken work

Milestone intent lives in `Docs/project/mvp-scope.md`. Runtime boundaries live
in `Docs/project/technical-architecture.md`. Use the board to track the active
task breakdown, reviews, and execution notes.

If a task does not support the current milestone, keep it in the board's
`Backlog` column until the milestone changes.

## Task Board

Use the shared docs board for real work.

Column meaning:

| Column | Meaning |
| --- | --- |
| Backlog | Ideas and tasks not approved for immediate work |
| To do | Tasks clear enough to start |
| Doing | Tasks currently being worked on |
| Test / Review | Implemented work that needs review or testing |
| Done | Complete, tested, committed, and safe for the other person to pull |

Every real task should have:

- title
- goal
- acceptance criteria
- owner noted in the task body or handoff message until assignee support exists
- status
- milestone
- likely affected files

Avoid vague tasks like `Improve gameplay` or `Fix stuff`.

## Communication

Do not work silently on shared parts of the project.

Before starting work, post:

```text
Working on:
Owner:
Branch:
Milestone:
Files/scenes/prefabs likely affected:
Expected result:
Risk/blocker:
```

Before editing a shared Unity file, say it directly:

```text
I am editing SampleScene.unity for the next hour.
I am changing the Player prefab.
I am editing the HUD prefab.
```

When Milestone 1 renames or replaces the shared gameplay scene as
`Laboratory.unity`, use that scene name in the same message format.

After finishing work, post:

```text
Finished:
Branch:
Changed files:
How tested:
Needs review:
Known issues:
Next:
```

The main collaboration failure to avoid is two people unknowingly changing the
same scene, prefab, or project setting.

## Git Workflow

Never work directly on `master`.

Before starting a task:

```powershell
git checkout master
git pull --ff-only
git checkout -b feature/my-task
```

Run this once per machine before your first branch if you have not already:

```powershell
git lfs install
```

Use short branch names that describe the task:

```text
feature/player-movement
feature/lab-blockout
feature/interaction-system
feature/panic-meter-ui
fix/player-collision
fix/missing-prefab-reference
```

Before committing:

```powershell
git status
git diff
git add path/to/file1 path/to/file2
git diff --staged
git commit -m "feat(docs): explain the new MVP scope"
git push -u origin feature/my-task
```

Prefer staging specific files instead of `git add .`.

## Shared Unity File Rules

Unity scenes, prefabs, and project settings are easy to conflict and hard to
merge. Treat them as shared files.

### Scenes

Avoid two people editing the same scene at the same time.

Default rule:

```text
Only one person owns the current shared gameplay scene at a time.
```

Right now that means `SampleScene.unity`. After Milestone 1 renames or
replaces it, treat `Laboratory.unity` the same way.

Prefer working in:

- scripts
- prefabs
- prefab variants
- ScriptableObjects
- UI prefabs
- isolated test scenes

### Prefabs

Important prefabs should be claimed before editing:

- Player prefab
- Camera prefab
- HUD prefab
- GameManager prefab
- Interaction prompt prefab
- Brewing station prefab
- Disaster prefab

When possible, create smaller child prefabs instead of putting every system on
one large object.

### Project Settings

Be careful with:

- Input System settings
- physics layers
- tags
- sorting layers
- build settings
- render pipeline settings
- package changes
- quality settings

Before changing project-wide settings, say what you are changing and why.

## Merge Conflict Rules

If a merge conflict happens:

1. Stop.
2. Do not guess.
3. Identify which file is conflicted.
4. Identify who owns that file or system.
5. Resolve together if it is a scene, prefab, project setting, or important
   system.
6. Test immediately after resolving.
7. Commit the resolved conflict clearly.

Do not randomly click through Unity YAML conflicts.

## Testing and Definition of Done

Test every feature before merging.

Minimum test questions:

- Does the project open?
- Does the project compile?
- Does the shared gameplay scene run?
- Are there console errors?
- Does the feature meet the task acceptance criteria?
- Did this accidentally break something else?
- Can the other person pull and run it?

Minimum local verification for most tasks:

1. Open Unity and wait for compilation or package import to finish.
2. Open the scene affected by the task. For general smoke tests today, use
   `Assets/Scenes/SampleScene.unity`.
3. Press Play and let the scene run long enough to confirm Play Mode entered
   cleanly.
4. Check the Console for new errors relevant to the task.
5. Stop Play Mode before reviewing the final scene diff.

When a task adds or changes automated tests, also run the relevant suite in
`Window > General > Test Runner`:

- `EditMode` for pure logic or editor-facing tests
- `PlayMode` for scene, runtime, or integration behavior

A task is done only when:

- acceptance criteria are met
- the feature works in Play Mode
- there are no relevant console errors
- the project still opens, compiles, and runs
- existing features are not obviously broken
- the change is committed
- the other person can pull and run it

If it is not tested, it is not done.

## Documentation Rules

Update evergreen docs when:

- systems change
- setup instructions change
- responsibilities change
- major stable decisions are made
- the team changes its review or workflow rules

Current evergreen docs live in `Docs/`.

Primary docs:

- `Docs/index.md`
- `Docs/onboarding/getting-started.md`
- `Docs/project/game-design.md`
- `Docs/project/mvp-scope.md`
- `Docs/project/technical-architecture.md`
- `Docs/collaboration/team-workflow.md`

Unity craft guides live under `Docs/guides/unity/`.

Do not create duplicate docs with different names unless the team intentionally
replaces the old doc.

Keep these in board tickets or archive pages instead of `Docs/`:

- milestone implementation plans
- one-off readiness reviews
- task-specific execution notes
- temporary decision logs

## Project Health Checklist

At any point, the project should usually satisfy:

- project opens
- project compiles
- shared gameplay scene runs
- latest build is playable
- `master` is stable
- tasks are visible
- current milestone is clear
- board exists
- known issues are written down
- both people know what the other is doing

If several of these are false, stop adding features and stabilize the project.

## Emergency Stabilization

If the project becomes unstable:

1. Stop feature work.
2. Identify what is broken.
3. Write the issue down.
4. Fix compiler errors first.
5. Fix broken scenes second.
6. Fix broken core gameplay third.
7. Merge only after the project runs again.

Do not continue building new features on top of a broken base.
