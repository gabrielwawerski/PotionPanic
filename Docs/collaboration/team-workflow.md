# Daily Workflow

Use this guide for recurring team work after completing
[Project Setup](../onboarding/getting-started.md). It explains how a task moves
from the board to a tested handoff while keeping shared Unity files safe.

## Before Starting Work

1. Confirm the repository is clean enough to identify your own changes.
2. Update master:

       git checkout master
       git pull --ff-only

3. Confirm the active milestone in
   [MVP Scope](../project/mvp-scope.md).
4. Choose one small, clear task from To do on the board.
5. Read the task's goal, acceptance criteria, likely files, and open blockers.
6. Keep unrelated ideas in Backlog instead of interrupting the milestone.

## Start Safely

### Announce the Work

Do not work silently on shared parts of the project. Before starting, post:

    Working on:
    Owner:
    Branch:
    Milestone:
    Files/scenes/prefabs likely affected:
    Expected result:
    Risk/blocker:

Say directly when you will edit a shared Unity file, for example:

    I am editing SampleScene.unity for the next hour.
    I am changing the Player prefab.

After Milestone 1 renames or replaces the shared gameplay scene, use its real
name instead of SampleScene.unity.

### Create a Focused Branch

Create a short-lived branch after updating master:

    git checkout -b feature/my-task

Use names that describe the task, such as feature/player-movement,
feature/lab-blockout, fix/player-collision, or fix/missing-prefab-reference.

### Protect Shared Unity Files

Always announce before editing:

- Assets/Scenes/*.unity
- Assets/**/*.prefab
- ProjectSettings/*
- Packages/manifest.json
- Packages/packages-lock.json

Before editing a coordinated scene, use the
[Coordinated Leasing Guide](../guides/coordinated-leasing.md) to connect, set a
task context, choose the path, and reserve it. Coordination is advisory and
does not replace the announcement.

If the endpoint is missing, invalid, or unhealthy, select the local Disabled
switch. Preserve local work and use the manual collaboration fallback for every
protected-file edit. Reconnect only after the service health is restored.

## Implement a Small Playable Slice

Keep the task narrow enough to build, test, and hand off. Prefer scripts,
prefabs, prefab variants, ScriptableObjects, UI prefabs, and isolated test
scenes when they avoid concurrent edits to the shared gameplay scene.

Prototype with placeholders before polishing. Stop feature work when the
project is unstable. A task that does not support the active milestone belongs
in Backlog until the milestone changes.

## Verify Before Handoff

For most tasks:

1. Wait for Unity compilation or package import to finish.
2. Open the affected scene. For a general smoke test, use
   Assets/Scenes/SampleScene.unity.
3. Press Play and confirm that Play Mode starts cleanly.
4. Check the Console for new errors related to the task.
5. Stop Play Mode before reviewing the final scene diff.
6. Run the relevant suite from Window > General > Test Runner when the task
   changes tests or gameplay code:
   - EditMode for pure logic or editor-facing behavior.
   - PlayMode for scene, runtime, or integration behavior.
7. Review git status and git diff before staging.

A task is ready for review only when its acceptance criteria pass, the project
still opens and runs, relevant Console errors are absent, and existing features
are not obviously broken.

## Hand Off, Review, and Merge

Stage only related files and review the staged diff:

    git status
    git diff
    git add path/to/file1 path/to/file2
    git diff --staged
    git commit -m "feat(docs): explain the new MVP scope"
    git push -u origin feature/my-task

Do not use git add . for normal task work. Review or test the branch before
merging. Keep master compiling, runnable, and playable.

Move the board task to Test / Review when review is needed, then Done only when
it is complete, tested, committed, and safe for the other developer to pull.
Post the handoff:

    Finished:
    Branch:
    Changed files:
    How tested:
    Needs review:
    Known issues:
    Next:

## Supporting Policies

### Task Board

| Column | Meaning |
| --- | --- |
| Backlog | Ideas and tasks not approved for immediate work. |
| To do | Tasks clear enough to start. |
| Doing | Tasks currently being worked on. |
| Test / Review | Implemented work that needs review or testing. |
| Done | Complete, tested, committed, and safe for the other person to pull. |

Every real task needs a title, goal, acceptance criteria, status, milestone,
and likely affected files. Add an assignee when the task has a clear owner.
Avoid vague tasks such as Improve gameplay or Fix stuff.

### Ownership and Scope

Each major area has a primary owner responsible for its quality, organization,
consistency, and decisions. Ownership is not exclusive access. Say so before
working inside another person's area.

| Area | Primary owner | Typical work |
| --- | --- | --- |
| Gameplay / systems | Developer A | Player controls, interactions, game state, scoring, disaster logic, save/load, and technical debugging. |
| World / UX / presentation | Developer B | Scene blockout, environment layout, UI, menus, audio, VFX, placeholder art, and playtest notes. |

Adjust this default split when a milestone needs a different division of work.
Only one major milestone should be active at a time. A milestone is complete
when its feature works in Play Mode, the project remains playable, the other
developer can pull and test it, and the next milestone is not blocked by broken
work.

### Shared Unity Files and Merge Conflicts

Avoid two people editing the same scene at the same time. Today the shared
gameplay scene is SampleScene.unity; treat Laboratory.unity the same way after
it becomes the canonical scene.

For important prefabs, claim the edit in the team channel before changing the
Player, Camera, HUD, GameManager, interaction prompt, brewing station, or
disaster prefab. Prefer smaller child prefabs where that reduces concurrent
edits.

Before changing Input System settings, physics layers, tags, sorting layers,
build settings, render settings, quality settings, or package files, state what
you will change and why.

If a merge conflict happens:

1. Stop and identify the conflicted file.
2. Identify the owner of that file or system.
3. Resolve together when it involves a scene, prefab, project setting, or
   important system.
4. Test immediately after resolving.
5. Commit the resolution clearly.

Do not click through Unity YAML conflicts without understanding the file.

### Documentation Ownership

Update evergreen documentation when setup, systems, responsibilities, workflow
rules, or stable project decisions change. The [Documentation Atlas](../ATLAS.md)
routes each topic to its owner.

Keep task-specific execution notes in board tickets. Keep current long-form
implementation plans in [plans](../plans/index.md), then archive them when
complete or superseded. Do not create duplicate evergreen documents with
different names.

### Project Health and Stabilization

The project should normally open, compile, run the shared gameplay scene, keep
master stable, show visible tasks and a clear milestone, record known issues,
and leave both developers aware of current work.

If several of these conditions are false:

1. Stop feature work.
2. Record what is broken.
3. Fix compiler errors first, broken scenes second, and broken core gameplay
   third.
4. Merge only after the project runs again.
