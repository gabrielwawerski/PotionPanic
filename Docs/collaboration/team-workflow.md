# Daily workflow

Use this guide for recurring work after completing
[Project Setup](../onboarding/getting-started.md). A normal task should move
through one visible sequence:

```text
understand current state -> choose work -> announce -> branch -> implement
-> verify -> hand off -> review -> merge
```

Skipping an early step usually creates work later. A silent scene edit becomes
a merge conflict; an unexplained dirty worktree becomes an accidental commit;
an unverified handoff becomes another developer's debugging session.

## 1. Understand the worktree

Start from the repository root:

```powershell
git branch --show-current
git status --short
git log -1 --oneline
```

Read the output before switching branches or pulling:

- A clean worktree has no `git status --short` entries.
- Existing changes belong to their current owner until proven otherwise.
- Unrelated changes are not permission to reset, delete, stage, or include
  them in the new task.
- If the current branch already contains unfinished work, finish or hand off
  that work before starting another task in the same checkout.

Do not use destructive Git cleanup to manufacture a clean start. Ask the owner
when a local change cannot be explained.

## 2. Update the shared base

With a clean worktree, update `master` without creating a merge commit:

```powershell
git checkout master
git pull --ff-only
```

`--ff-only` stops when local and remote history have diverged. That stop is
useful: it prevents a routine update from silently creating a merge that needs
review.

Confirm the active milestone in [MVP Scope](../project/mvp-scope.md). Only one
major milestone should drive feature work at a time.

## 3. Choose a task that is ready

Open the [board](../board.md) and choose one task from `To do`.

| Column | Meaning |
| --- | --- |
| Backlog | Ideas and unapproved future work. |
| To do | Clear enough to start, with a goal and acceptance criteria. |
| Doing | Actively owned work. |
| Test / Review | Implemented work awaiting verification or review. |
| Done | Complete, tested, committed, and safe for another contributor to pull. |

Before moving a task to `Doing`, read its goal, acceptance criteria, affected
files, dependencies, and blockers. Split a task when its parts cannot be built,
tested, and reviewed as one coherent change.

Keep unrelated ideas in Backlog. Starting future milestone work early makes the
current milestone harder to finish and hides which dependency is actually
blocking progress.

## 4. Announce ownership and risk

Post a start message before editing shared project areas:

```text
Working on:
Owner:
Branch:
Milestone:
Files/scenes/prefabs likely affected:
Expected result:
Risk/blocker:
```

Name a shared Unity file directly:

```text
I am editing Assets/Scenes/SampleScene.unity for the next hour.
I am changing the Player prefab.
```

An announcement communicates intent to the other person even when software is
offline. The Coordination service communicates current machine-observed state.
The two mechanisms answer different questions, so one does not replace the
other.

## 5. Create a focused branch

Create a short-lived branch from the updated base:

```powershell
git checkout -b feature/my-task
```

Use the type and subject that describe the work:

```text
feature/player-movement
feature/lab-blockout
fix/player-collision
docs/evergreen-v2
```

Keep one reviewable objective on the branch. Do not combine gameplay, project
settings, scene cleanup, and unrelated documentation because they happened in
the same session.

## 6. Protect shared Unity files

Always announce before editing:

- `Assets/Scenes/*.unity`
- `Assets/**/*.prefab`
- `ProjectSettings/*`
- `Packages/manifest.json`
- `Packages/packages-lock.json`

Scenes and prefabs are serialized object graphs. Project settings and package
files change behavior across the whole checkout. All are difficult to reconcile
when two people make overlapping changes without coordination.

The current automated rule in `coordination.json` covers scene files below
`Assets/Scenes/`. Before editing one, open
[Unity Coordination](../guides/coordinated-leasing.md), connect, set a task
context, select the path, and reserve it. Prefabs, project settings, and
packages still rely on the manual announcement unless a verified rule covers
them.

If the endpoint is missing, invalid, or unhealthy, select the local Disabled
switch. Preserve local work and use the manual collaboration fallback for every
protected-file edit. Reconnect only after the service health is restored.

Prefer scripts, ScriptableObjects, focused prefabs, prefab variants, UI
prefabs, and isolated test scenes when they let contributors work without
sharing one large scene edit.

## 7. Implement one playable or verifiable slice

Connect the task's acceptance criteria to the smallest behavior that can prove
them. For example, Milestone 1 movement does not need inventory, brewing, or a
general ability system. It needs the accepted input, movement rule, player
setup, and enough scene integration to observe the result.

During implementation:

- keep unrelated worktree changes untouched;
- create only the systems the current slice needs;
- use placeholders before expensive presentation work;
- make failure state visible through tests, Console messages, or focused debug
  information;
- stop feature work if compilation, the shared scene, or core gameplay becomes
  unstable.

Use the [Coding and Implementation Guide](../guides/unity/coding-and-implementation.md)
for responsibility, dependency, testing, and debug decisions.

## 8. Verify the actual change

Choose proof from the files and behavior that changed:

| Change | Minimum evidence |
| --- | --- |
| Markdown or docs configuration | `npm test`, `npm run docs:build`, rendered page and navigation review. |
| Pure C# or editor logic | Relevant EditMode tests plus Unity compilation and Console review. |
| Runtime or scene integration | Relevant PlayMode tests, affected scene in Play Mode, and Console review. |
| Scene, prefab, or inspector wiring | Open the asset, inspect references and overrides, run the affected behavior, and review the serialized diff. |
| Project settings or packages | Reimport or restart when required, run affected behavior, and inspect every changed shared file. |

For a normal Unity smoke test:

1. Wait for compilation and import to finish.
2. Open the affected scene. Use `Assets/Scenes/SampleScene.unity` for the
   current general smoke.
3. Enter Play Mode and exercise the acceptance criteria.
4. Check the Console for new relevant errors or warnings.
5. Exit Play Mode before reviewing serialized changes.
6. Run the relevant EditMode or PlayMode suite from
   `Window > General > Test Runner`.
7. Review `git status` and `git diff`.

Passing a different suite does not prove the changed behavior. A baseline
failure must be reported separately rather than described as a regression or
silently ignored.

## 9. Stage and hand off only the task

Review before staging, then stage explicit paths:

```powershell
git status
git diff
git add path/to/file1 path/to/file2
git diff --staged
git commit -m "type(scope): describe the change"
git push -u origin feature/my-task
```

Do not use `git add .` for normal task work. The staged diff is the proposed
commit, so read it as a reviewer would.

Post a handoff that another contributor can execute without reconstructing the
session:

```text
Finished:
Branch:
Changed files:
How tested:
Needs review:
Known issues:
Next:
```

Move the task to `Test / Review` while work still needs independent checking.
Move it to `Done` only after the accepted evidence exists and the branch is
safe to pull.

## 10. Review and merge

The reviewer checks the acceptance criteria, risky shared files, test evidence,
and remaining limitations. A successful local run by the author does not
replace review of scene, prefab, package, project-setting, or coordination
changes.

Keep `master` compiling, runnable, and playable. Resolve merge conflicts with
the owner of the affected system or shared Unity file, then rerun the relevant
verification. Do not click through Unity YAML conflicts without understanding
which serialized objects and references changed.

## Ownership and collaboration boundaries

The default split helps route decisions; it does not grant exclusive access:

| Area | Primary owner | Typical work |
| --- | --- | --- |
| Gameplay and systems | Developer A | Controls, interactions, runtime state, scoring, disasters, technical debugging. |
| World, UX, and presentation | Developer B | Scene blockout, environment, UI, menus, audio, VFX, placeholder art, playtest notes. |

Say so before working inside the other person's area. Adjust ownership when the
active milestone needs a different split.

## Documentation during a task

Update the evergreen owner when stable setup, workflow, game, scope,
architecture, or tool behavior changes. Use the
[Evergreen Documentation Contract](../evergreen-documentation.md) to identify
the owner and evidence source.

Keep task execution in tickets, long-form implementation in active plans, and
durable execution decisions in chronicles. Preserve historical records rather
than rewriting them as current tutorials.

## Stabilization workflow

When the project no longer opens, compiles, runs the shared scene, or preserves
its core loop:

1. Stop feature work.
2. Record the failure and the last known working state.
3. Separate baseline failures from the new regression.
4. Fix compiler errors first, broken scenes second, and broken core gameplay
   third.
5. Verify the recovery before merging or resuming feature work.

Adding new behavior on top of an unexplained broken base makes diagnosis less
reliable and transfers risk to every later task.

## Related pages

- [Project Setup](../onboarding/getting-started.md)
- [Project Overview](../project/)
- [Unity Guides](../guides/unity/)
- [Unity Coordination](../guides/coordinated-leasing.md)
- [Active Plans](../plans/)
