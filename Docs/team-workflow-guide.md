# Team Workflow Guide

Version: 1.3
Status: Active workflow guide
Scope: Two-person beginner/intermediate Unity team
Primary repo: Potion Panic

This guide defines how the team works together without blocking each other, breaking the project, or letting scope grow faster than the game.

Use this guide as the practical day-to-day workflow. Use these project docs as the source of truth for the actual game:

- `docs/Potion Panic.md` for game design, scope, milestones, and tuning targets
- `docs/Potion Panic - Technical Architecture.md` for implementation structure and system behavior
- `docs/plans/implementation-readiness-review.md` for locked MVP decisions and readiness notes

---

## Quick Start: Daily Workflow

At the start of a work session:

1. Pull the latest `master`.
2. Confirm the current milestone.
3. Pick one small task from `Ready`.
4. Say what you are working on and which files, scenes, or prefabs you expect to touch.
5. Create a short-lived feature branch.
6. Build and test the change locally.
7. Commit only the related files.
8. Push the branch.
9. Review or test before merging.
10. Write down what changed, what was tested, and what is still risky.

The goal is not to write the most code. The goal is to keep the game playable and keep both people aware of the project state.

---

## Non-Negotiable Rules

1. Do not work directly on `master`.
2. Keep `master` compiling, runnable, and playable.
3. Communicate before editing shared Unity files.
4. Work in small tasks.
5. Test before merging.
6. Write down blockers, known issues, and unfinished work.
7. Put new ideas in the backlog instead of interrupting the current milestone.
8. Prototype with placeholders before polishing.
9. Avoid two people editing the same Unity scene at the same time.
10. Stop feature work when the project is unstable.

If unsure what to do next, ask:

> Does this bring us closer to a playable game?

If yes, consider it for the current milestone. If no, put it in the backlog.

---

## Repository Setup

Before development begins, both developers should confirm the same baseline setup.

Required setup:

- Unity version matches `ProjectSettings/ProjectVersion.txt`.
- Current recorded Unity version is `6000.5.1f1`.
- The project opens on both machines.
- The main gameplay scene runs on both machines.
- Visible Meta Files are enabled.
- Force Text serialization is enabled.
- `.gitignore` is present at the repo root.
- `.gitattributes` is present at the repo root.
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

This repo already has ignore rules for these paths. If one appears in Git status, stop and check `.gitignore` before committing.

---

## Team Ownership

Each major area should have a primary owner. Ownership means responsibility for quality, organization, consistency, and decisions in that area.

Ownership does not mean exclusive access. The other developer can help, review, test, or change files when needed. If someone needs to work inside another person's area, they should say so first.

Recommended split:

| Area | Primary Owner | Typical Work |
| --- | --- | --- |
| Gameplay / Systems | Developer A | Player controls, interactions, game state, scoring, disaster logic, save/load, technical debugging |
| World / UX / Presentation | Developer B | Scene blockout, environment layout, UI, menus, audio, VFX, placeholder art, playtest notes |

This split is a default. Adjust it when a milestone needs a different division of work.

---

## Milestones and Scope

Only one major milestone should be active at a time.

Do not start future milestone work before the current milestone is playable. A milestone is complete when:

- the feature works in Play Mode
- the project remains playable
- the other developer can pull and test it
- the next milestone is not blocked by broken work

For Potion Panic, milestone intent lives in `docs/Potion Panic.md`. Implementation handoff plans live under `docs/plans/`.

Current useful references:

- `docs/plans/implementation-readiness-review.md`
- `docs/plans/milestone-1-implementation-plan.md`

If a task does not support the current milestone, put it in the backlog.

---

## Task Board

Use a simple task board:

```text
Backlog
Ready
In Progress
Review / Test
Done
```

Column meaning:

| Column | Meaning |
| --- | --- |
| Backlog | Ideas and tasks not approved for immediate work |
| Ready | Tasks clear enough to start |
| In Progress | Tasks currently being worked on |
| Review / Test | Implemented work that needs review or testing |
| Done | Complete, tested, committed, and safe for the other person to pull |

Every real task should have:

- title
- goal
- acceptance criteria
- owner
- status
- milestone
- likely affected files

Avoid vague tasks:

```text
Improve gameplay
Work on UI
Fix stuff
Make game better
```

Use specific tasks:

```text
Add player movement
Create interaction prompt
Add panic meter UI
Add first disaster type
Create main menu
```

---

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
I am editing Laboratory.unity for the next hour.
I am changing the Player prefab.
I am editing the HUD prefab.
```

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

The main collaboration failure to avoid is two people unknowingly changing the same scene, prefab, or project setting.

---

## Git Workflow

Never work directly on `master`.

Before starting a task:

```bash
git checkout master
git pull --ff-only
git checkout -b feature/my-task
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

Avoid vague branch names:

```text
feature/everything-this-week
feature/random-updates
feature/big-progress
```

Before committing:

```bash
git status
git diff
git add path/to/file1 path/to/file2
git diff --staged
git commit -m "Add basic player movement"
git push -u origin feature/my-task
```

Prefer staging specific files instead of `git add .`. This reduces accidental commits of generated Unity files or unrelated local changes.

Good commit messages:

```text
Add basic player movement
Create laboratory blockout
Add interaction prompt UI
Fix player collision with tables
Add panic meter display
```

Bad commit messages:

```text
stuff
changes
update
fix
final
asdf
```

Merge only after the branch has been tested. For important changes, the teammate should pull the branch and test it in Play Mode before it goes into `master`.

Important changes include:

- main scene edits
- player prefab edits
- game manager changes
- input changes
- save/load changes
- core gameplay loop changes
- build settings changes
- package changes

---

## Unity Shared File Rules

Unity scenes, prefabs, and project settings are easy to conflict and hard to merge. Treat them as shared files.

### Scenes

Avoid two people editing the same scene at the same time.

Default rule:

```text
Only one person owns Laboratory.unity at a time.
```

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

When possible, create smaller child prefabs instead of putting every system on one large object.

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

Example:

```text
I need to add an Interactable layer for raycast detection.
```

---

## Merge Conflict Rules

If a merge conflict happens:

1. Stop.
2. Do not guess.
3. Identify which file is conflicted.
4. Identify who owns that file or system.
5. Resolve together if it is a scene, prefab, project setting, or important system.
6. Test immediately after resolving.
7. Commit the resolved conflict clearly.

Scene and prefab conflicts should be handled carefully. Do not randomly click through Unity YAML conflicts.

---

## Testing and Definition of Done

Test every feature before merging.

Minimum test questions:

- Does the project open?
- Does the project compile?
- Does the main scene run?
- Are there console errors?
- Does the feature meet the task acceptance criteria?
- Did this accidentally break something else?
- Can the other person pull and run it?

For gameplay changes, test in Play Mode.

For UI changes, test at the target resolution if one exists.

For scene changes, check missing references and broken prefabs.

A task is done only when:

- acceptance criteria are met
- the feature works in Play Mode
- there are no relevant console errors
- the project still opens, compiles, and runs
- existing features are not obviously broken
- the change is committed
- the other person can pull and run it

Not done:

- mostly works
- works only on one computer
- implemented but not tested
- committed with known console errors
- requires several fixes later
- breaks another feature

If it is not tested, it is not done.

---

## Meetings and Reviews

Keep meetings short and decision-focused.

Recommended length:

- session start: 5-10 minutes
- session end: 5-10 minutes
- weekly review: 20-30 minutes

At session start, answer:

```text
What are we building today?
What is the current milestone?
Who owns which task?
Which files/scenes should not be touched at the same time?
What needs testing from last session?
```

At session end, answer:

```text
What got finished?
What got merged?
What is still in progress?
What broke?
What needs testing?
What is next?
```

Once per week, play the latest build and review:

- what is working
- what is confusing
- what is too large
- what should be cut
- what should be next
- whether the current milestone is still realistic

The weekly review exists to protect the project from scope creep.

---

## Backlog and Scope Control

New ideas go into the backlog. Do not immediately implement them.

Examples:

- multiplayer
- achievements
- bosses
- upgrades
- cosmetics
- extra rooms
- daily challenge mode
- advanced AI
- online leaderboards

Before adding a feature, ask:

```text
Does this help the current milestone become playable?
```

If yes, consider it. If no, put it in the backlog.

A good idea at the wrong time is still a distraction.

---

## Documentation Rules

Update documentation when:

- systems change
- responsibilities change
- milestones change
- major decisions are made
- setup instructions change
- build instructions change

Current repo docs live in `docs/`, not `Docs/`.

Primary docs:

```text
docs/Potion Panic.md
docs/Potion Panic - Technical Architecture.md
docs/team-workflow-guide.md
docs/plans/implementation-readiness-review.md
```

Do not create duplicate docs with different names unless the team intentionally replaces the old doc. Stale docs cause bad decisions.

Useful future docs, if needed:

```text
docs/backlog.md
docs/known-issues.md
docs/playtest-notes.md
```

---

## Project Health Checklist

At any point, the project should usually satisfy:

- project opens
- project compiles
- main scene runs
- latest build is playable
- `master` is stable
- tasks are visible
- current milestone is clear
- backlog exists
- known issues are written down
- both people know what the other is doing

If several of these are false, stop adding features and stabilize the project.

---

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

---

## Task Template

```markdown
## Task Name

Owner:
Status:
Milestone:

Goal:
Describe what this task should achieve.

Acceptance Criteria:
- Criterion 1
- Criterion 2
- Criterion 3

Files likely affected:
- File or folder name

Notes:
- Any useful context
```

Example:

```markdown
## Add Player Movement

Owner: Developer A
Status: Ready
Milestone: Movement

Goal:
Create basic WASD player movement for the prototype.

Acceptance Criteria:
- Player moves with WASD.
- Movement speed is adjustable in the Inspector.
- Player collides with walls and tables.
- Camera still frames the player correctly.
- No relevant console errors.
- Tested in Play Mode.

Files likely affected:
- Assets/Scripts/Player/PlayerController.cs
- Assets/Prefabs/Player.prefab

Notes:
- Keep this simple. Do not add stamina, dodging, or animation blending yet.
```

---

## Bug Report Template

```text
Bug:
Where it happens:
Steps to reproduce:
Expected result:
Actual result:
Screenshot/video:
Likely related files:
Severity:
```

---

## Weekly Review Template

```text
Finished this week:
Still in progress:
Broken or risky:
Cut from scope:
Added to backlog:
Next milestone tasks:
```

---

## Success Criteria

A successful collaboration is not:

- writing the most code
- making the biggest system
- adding the most features
- having the most elaborate architecture
- planning every detail perfectly

A successful collaboration means:

- both people understand the project
- the project remains playable
- work is visible
- problems are communicated early
- tasks are small enough to finish
- the team cuts unnecessary scope
- the project reaches completion

Finishing a small game together is more valuable than abandoning a larger one.
