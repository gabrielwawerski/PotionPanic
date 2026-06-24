# Team Workflow Guide

Version: 1.2  
Scope: Two-person beginner/intermediate game-development team  
Primary use case: Unity projects, but usable for future software projects too

---

# 1. Purpose

This guide defines how the team collaborates on a project.

The goal is not simply to write code quickly. The goal is to:

- avoid blocking each other
- avoid merge conflicts
- keep the project playable
- make progress visible
- prevent scope creep
- build good development habits
- finish small projects instead of abandoning large ones

This workflow should be lightweight enough for a two-person team, but strict enough to prevent the most common beginner-team problems.

---

# 2. Repository Setup Checklist

Before development begins, confirm that both developers have the same project setup.

Required setup:

- Same Unity version installed.
- Same render pipeline selected, if applicable.
- Visible Meta Files enabled.
- Force Text serialization enabled.
- Unity `.gitignore` added.
- `main` branch created.
- Branch workflow agreed upon.
- Project opens on both machines.
- Main scene runs on both machines.
- Both developers can pull the repository and run the project.
- Both developers understand which folders should not be committed.

For Unity, do not commit generated cache folders such as:

```text
Library/
Temp/
Obj/
Build/
Builds/
Logs/
UserSettings/
```

The purpose of this checklist is to prevent the classic problem:

> It works on my machine.

---

# 3. Core Philosophy

A finished small project is more valuable than a large unfinished project.

A working build is more valuable than perfect architecture.

Small, tested improvements are better than large unfinished systems.

Communication is more important than individual productivity.

A feature is not valuable until it works in the project and the other person can pull it without breaking their work.

---

# 4. Prototype First

Features should first be proven with placeholder assets.

Use simple placeholders before investing time in final presentation.

Examples:

- cubes instead of finished 3D models
- colored materials instead of final textures
- debug text instead of polished UI
- placeholder sounds instead of final audio
- simple particles instead of final VFX
- test scenes instead of polished levels

The goal is to prove that the gameplay works before spending time making it look finished.

A feature that is fun with placeholder assets is worth polishing.

A feature that is not fun with placeholder assets usually should not be polished yet.

---

# 5. Core Collaboration Rule

Do not work silently on shared parts of the project.

Before starting work, each person should know:

- what the other person is working on
- which files, scenes, or prefabs are being edited
- what is safe to touch
- what is blocked
- what is ready for testing

The main collaboration failure to avoid is not slow progress. It is two people unknowingly changing the same thing and breaking the project.

---

# 6. Team Ownership

Each major area of the project should have a primary owner.

Ownership means:

- responsible for quality
- responsible for organization
- responsible for consistency
- responsible for decisions in that area
- responsible for keeping that area working

Ownership does **not** mean:

- exclusive access
- nobody else can help
- nobody else can modify files
- every change requires permission

If someone needs to work inside another person's area, they should say so first.

---

# 7. Recommended Two-Person Split

## Developer A: Gameplay / Systems

Primary responsibilities:

- player controls
- core gameplay mechanics
- interactions
- game state
- scoring
- AI
- technical systems
- save/load logic
- code architecture
- gameplay debugging

Example tasks:

- add player movement
- create interaction system
- implement panic meter logic
- add brewing rules
- implement disaster spawning
- add win/loss conditions

---

## Developer B: World / UX / Presentation

Primary responsibilities:

- scenes
- level layout
- environment blockout
- UI
- menus
- audio
- VFX
- art placeholders
- player feedback
- playtesting notes

Example tasks:

- block out the laboratory scene
- create placeholder stations
- build HUD layout
- add interaction prompts
- add sound effects
- tune camera feel
- polish scene readability

---

# 8. Milestone Rule

Only one major milestone should be active at a time.

Do not start future milestone work before the current milestone is playable.

Good milestone flow:

```text
Movement
Interaction
Brewing
Disasters
Panic Meter
Win/Loss Loop
Polish
```

Bad milestone flow:

```text
Movement + UI + Audio + Menus + Save System + Upgrades + Cutscenes
```

A milestone is complete when:

- the feature works in Play Mode
- the project remains playable
- the other developer can pull and test it
- the next milestone is not blocked by broken work

If a task does not support the current milestone, put it in the backlog.

---

# 9. Communication Rules

## Before Starting Work

Post a short message:

```text
Working on:
Files/scenes/prefabs I expect to touch:
Estimated scope:
Possible risk/blocker:
```

Example:

```text
Working on: Basic player movement
Files/scenes/prefabs I expect to touch: PlayerController.cs, Player prefab
Estimated scope: Small
Possible risk/blocker: May need collision changes
```

---

## Before Editing Shared Files

Say it clearly before editing:

```text
I am editing Laboratory.unity.
I am changing the Player prefab.
I am editing the HUD prefab.
```

For Unity scenes, important prefabs, project settings, input settings, or build settings, do not assume the other person is not touching them.

---

## After Finishing Work

Post a short summary:

```text
Finished:
Changed files:
Needs testing:
Blocked:
Next:
```

Example:

```text
Finished: Player movement with WASD
Changed files: PlayerController.cs, Player prefab
Needs testing: Collision around tables
Blocked: Nothing
Next: Interaction raycast
```

---

# 10. Meetings

Keep meetings short.

The purpose of a meeting is to decide what to build next, what is blocked, and what should be cut.

Recommended meeting length:

- daily/session start: 5-10 minutes
- daily/session end: 5-10 minutes
- weekly review: 20-30 minutes

Avoid long planning sessions that replace actual building.

A useful meeting answers:

- What is finished?
- What is blocked?
- What is next?
- What should not be touched at the same time?
- Is the current milestone still realistic?

If a meeting does not produce decisions, shorten it next time.

---

# 11. Task System

Every real task should have:

- title
- goal
- acceptance criteria
- owner
- status

Avoid vague tasks like:

```text
Improve gameplay
Work on UI
Fix stuff
Make game better
```

Use specific tasks like:

```text
Add player movement
Create interaction prompt
Add panic meter UI
Add first disaster type
Create main menu
```

---

# 12. Task Template

Use this format for tasks:

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
- Camera still follows or frames the player correctly.
- No console errors.
- Tested in Play Mode.

Files likely affected:
- Assets/Scripts/Player/PlayerController.cs
- Assets/Prefabs/Player.prefab

Notes:
- Keep this simple. Do not add stamina, dodging, or animation blending yet.
```

---

# 13. Task Board

Use a simple board with these columns:

```text
Backlog
Ready
In Progress
Review / Test
Done
```

## Column Meaning

### Backlog

Ideas and tasks that are not approved for immediate work.

### Ready

Tasks that are clear enough to start.

### In Progress

Tasks currently being worked on.

### Review / Test

Work is implemented, but needs testing or review before being considered finished.

### Done

The task is complete, tested, committed, and safe for the other person to pull.

Only move a task to Done when it meets the Definition of Done.

---

# 14. Definition of Done

A task is done only when:

- the feature works in Play Mode
- there are no console errors
- the project still opens
- the project still compiles
- the project still runs
- the change is committed
- the other person can pull and run it
- acceptance criteria are met
- existing features are not obviously broken

Not done:

- mostly works
- works only on one computer
- implemented but not tested
- committed with known console errors
- requires several fixes later
- breaks another feature

If it is not tested, it is not done.

---

# 15. Git Workflow

## Main Rule

Never work directly on:

```text
main
```

Use feature branches.

Example branch names:

```text
feature/player-movement
feature/lab-blockout
feature/interaction-system
feature/panic-meter-ui
fix/player-collision
fix/missing-prefab-reference
```

---

## Basic Git Flow

Before starting a task:

```bash
git checkout main
git pull
git checkout -b feature/my-task
```

After finishing a task:

```bash
git status
git add .
git commit -m "Add basic player movement"
git push
```

Then merge only after testing.

For a two-person team, either use pull requests or agree that the teammate quickly pulls and tests important changes before they go into `main`.

---

## Branch Rules

Branches should be short-lived.

Good:

```text
feature/player-movement
feature/interaction-prompt
fix/brewing-station-trigger
```

Bad:

```text
feature/everything-this-week
feature/random-updates
feature/big-progress
```

Merge small finished work often.

Large branches are harder to review, harder to merge, and more likely to break the project.

---

## Commit Rules

Good commits are small, clear, and describe one change.

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

A commit should answer:

> What changed?

If a commit contains unrelated work, split it into smaller commits when practical.

---

## Main Branch Rules

The `main` branch should always:

- compile
- run
- be playable
- contain no major console errors
- contain no broken scenes
- contain no missing critical references

Never leave `main` with:

- compiler errors
- broken scenes
- missing scripts
- broken prefabs
- unassigned critical references
- half-implemented experimental systems

If something may break the game, keep it on a feature branch.

---

# 16. Unity Scene Rules

Unity scenes are difficult to merge.

Avoid two people editing the same scene at the same time.

Example rule:

```text
Only one person owns Laboratory.unity at a time.
```

Before editing a scene, say so first.

Example:

```text
I am editing Laboratory.unity for the next hour.
Please avoid scene changes until I push.
```

Prefer working in:

- scripts
- prefabs
- prefab variants
- ScriptableObjects
- UI prefabs
- isolated test scenes

Avoid both people constantly editing the same main scene.

---

# 17. Unity Prefab Rules

Important prefabs should also be treated as shared files.

Examples:

- Player prefab
- Camera prefab
- HUD prefab
- GameManager prefab
- Interaction prompt prefab
- Brewing station prefab
- Disaster prefab

Before editing an important prefab, say so.

When possible:

- create smaller child prefabs
- avoid putting every system on one giant object
- avoid changing project-wide settings casually
- test prefab changes in Play Mode before committing

---

# 18. Unity Project Settings Rules

Be careful with:

- Input System settings
- physics layers
- tags
- sorting layers
- build settings
- render pipeline settings
- package changes
- quality settings

These changes can affect the whole project.

Before changing them, say what you are changing and why.

Example:

```text
I need to add a new Interactable layer for raycast detection.
```

---

# 19. Merge Conflict Rules

If a merge conflict happens:

1. Stop.
2. Do not guess.
3. Identify which file is conflicted.
4. Identify who owns that file or system.
5. Resolve together if it is a scene, prefab, project setting, or important system.
6. Test immediately after resolving.
7. Commit the resolved conflict clearly.

Scene and prefab conflicts should be handled carefully.

Do not randomly click through Unity YAML conflicts.

---

# 20. Testing Rules

Test every feature before merging.

Minimum test questions:

- Does the project open?
- Does the project compile?
- Does the main scene run?
- Are there console errors?
- Does the feature meet the task criteria?
- Did this accidentally break something else?
- Can the other person pull and run it?

For gameplay changes, test in Play Mode.

For UI changes, test at the target resolution if one exists.

For scene changes, check missing references and broken prefabs.

---

# 21. Review Process

For small changes:

- test locally
- commit
- push
- merge after teammate is aware

For important changes:

- teammate pulls the branch
- teammate tests in Play Mode
- issues are fixed before merge

Important changes include:

- main scene edits
- player prefab edits
- game manager changes
- input changes
- save/load changes
- core gameplay loop changes
- build settings changes

---

# 22. Daily Session Workflow

## Start of Session

Discuss:

```text
What are we building today?
What is the current milestone?
Who owns which task?
Which files/scenes should not be touched at the same time?
What needs testing from last session?
```

Keep this short. The goal is alignment, not a long meeting.

---

## End of Session

Discuss:

```text
What got finished?
What got merged?
What is still in progress?
What broke?
What needs testing?
What is next?
```

If something is broken, write it down.

Do not rely on memory.

---

# 23. Weekly Review

Once per week, play the latest build and review the project.

Questions:

- What did we finish?
- What is working?
- What is confusing?
- What is too large?
- What should be cut?
- What should be next?
- Is the current milestone still realistic?

Update:

- task board
- backlog
- milestone status
- project docs
- known issues

The weekly review should protect the project from scope creep.

---

# 24. Backlog Rule

New ideas go into:

```text
Backlog.md
```

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

Do not immediately implement new ideas.

Only build what supports the current milestone.

A good idea at the wrong time is still a distraction.

---

# 25. Scope Control Rule

Before adding a feature, ask:

```text
Does this help the current milestone become playable?
```

If yes, consider it.

If no, put it in the backlog.

Avoid adding systems because they might be useful later.

Do not build stretch features before the MVP works.

---

# 26. Documentation Rules

Update documentation when:

- systems change
- responsibilities change
- milestones change
- major decisions are made
- setup instructions change
- build instructions change

Documentation should reflect reality.

Bad documentation is worse than no documentation when it causes people to follow outdated instructions.

Recommended project docs:

```text
README.md
Backlog.md
Milestones.md
KnownIssues.md
Team Workflow Guide.md
```

Optional docs:

```text
Technical Architecture.md
Art Direction.md
Design Notes.md
Playtest Notes.md
```

---

# 27. Recommended Repository Structure

A simple structure is enough:

```text
/project-root
  /Assets
  /Packages
  /ProjectSettings
  /Docs
    Team Workflow Guide.md
    Backlog.md
    Milestones.md
    KnownIssues.md
  README.md
```

If you do not want a `Docs/` folder yet, keep the docs in the project root until the repository grows.

---

# 28. Project Health Checklist

At any moment, the project should usually satisfy:

- project opens
- project compiles
- main scene runs
- latest build is playable
- main branch is stable
- tasks are visible
- current milestone is clear
- backlog exists
- known issues are written down
- both people know what the other is doing

If several of these are false, stop adding features and stabilize the project.

---

# 29. Emergency Stabilization Rule

If the project becomes unstable:

- stop feature work
- identify what is broken
- create or update `KnownIssues.md`
- fix compiler errors first
- fix broken scenes second
- fix broken core gameplay third
- merge only after the project runs again

Do not continue building new features on top of a broken base.

---

# 30. Templates

## Work Start Template

```text
Working on:
Owner:
Branch:
Milestone:
Files/scenes/prefabs likely affected:
Expected result:
Risk/blocker:
```

## Work Finished Template

```text
Finished:
Branch:
Changed files:
How tested:
Needs review:
Known issues:
Next:
```

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

# 31. Golden Rules

1. Communicate before editing shared files.
2. Keep `main` stable.
3. Work in small tasks.
4. Merge often.
5. Test before merging.
6. Write things down.
7. Backlog new ideas.
8. Prefer playable builds.
9. Prototype with placeholders first.
10. Cut scope aggressively.
11. Do not build stretch features before the MVP.
12. Avoid two people editing the same Unity scene at the same time.
13. Resolve scene and prefab conflicts carefully.
14. Do not continue feature work on a broken project.
15. Finish small projects.

---

# 32. Success Criteria

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

---

# 33. Final Rule

Whenever unsure what to work on, ask:

> Does this bring us closer to a playable game?

If the answer is yes:

Build it.

If the answer is no:

Put it in the backlog.