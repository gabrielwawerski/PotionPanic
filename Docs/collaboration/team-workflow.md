# Daily workflow

Use this guide for recurring work after completing
[Project Setup](../onboarding/getting-started.md). Human contributors normally
use the Git interface in Rider, WebStorm, or VS Code. PowerShell commands are
included as a fallback when the interface is unclear or a precise diagnostic is
needed.

A normal task moves through one visible sequence:

```text
inspect -> update master -> choose -> announce -> branch -> implement
-> verify -> commit -> push -> hand off -> review -> merge
```

Skipping an early step usually creates work later. A silent scene edit becomes a
merge conflict; an unexplained local change becomes part of the wrong commit; an
unverified handoff becomes another developer's debugging session.

## Understand what the Git interface shows

Rider, WebStorm, VS Code, and PowerShell operate on the same repository. Their
labels differ, but the underlying Git state is shared.

| Term           | Meaning                                                                          | Why it matters                                                                              |
|----------------|----------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------|
| Working tree   | The files currently on disk, including committed, modified, and untracked files. | Switching branches or discarding changes can replace these files.                           |
| Changelist     | A JetBrains grouping of local changes. It is not a branch or commit.             | A changelist helps organize work, but only the selected files and hunks belong in a commit. |
| Staging area   | The exact snapshot VS Code prepares under `Staged Changes` for the next commit.  | Unstaged changes stay on disk but are excluded from that commit.                            |
| Commit         | A local Git snapshot with an author and message.                                 | A commit is not available to the other developer until it is pushed.                        |
| Local branch   | A named line of work in this checkout.                                           | Branch switching changes which committed version is checked out.                            |
| Remote branch  | The last fetched view of a branch on `origin`, such as `origin/master`.          | Fetch before trusting it; the other developer may have pushed since the last fetch.         |
| Fetch          | Download remote branch information without changing working files.               | Fetch is the safe first synchronization action.                                             |
| Pull or Update | Fetch and integrate remote work into the current branch.                         | Use it only after confirming the current branch and local state.                            |
| Push           | Upload local commits to the remote branch.                                       | A push changes what the other developer can fetch and review.                               |

JetBrains changelists and VS Code staging use different mechanics. The required
result is the same: review the exact files and hunks that will enter the commit,
and leave unrelated work outside it.

## 1. Inspect the current checkout

Do this before fetching, switching branches, pulling, or starting a task.

### Rider or WebStorm

1. Read the current branch in the Git branch control.
2. Open the **Commit** tool window and inspect every changelist and unversioned
   file.
3. Open **Git Log** and read the current `HEAD` commit and its relationship to
   `origin/master`.

### VS Code

1. Read the branch name in the status bar.
2. Open **Source Control** and inspect **Changes**, **Staged Changes**, and
   untracked files.
3. Open **Source Control Graph** and inspect `HEAD`, the current branch, and
   `origin/master`.

A safe starting state has the expected branch and no unexplained local changes.
Existing changes belong to their current owner until proven otherwise. Do not
discard, stage, include, shelve, stash, or move them merely to make the
interface look clean. Finish or hand off existing work before starting another
task in the same checkout.

<details>
<summary>PowerShell fallback</summary>

```powershell
git branch --show-current
git status --short
git log -1 --oneline
```

A clean worktree produces no entries from `git status --short`.

</details>

## 2. Fetch and update `master`

Fetch first. Fetch updates remote branch information without changing the
working tree, so it lets you inspect the relationship before integrating
anything.

### Rider or WebStorm

1. Use **Git > Fetch**.
2. Check out `master` from the branch control.
3. Confirm the Commit tool window contains no local changes.
4. In Git Log, compare `master` with `origin/master`.
5. If local `master` has no outgoing commits and the histories have not
   diverged, use **Git > Update Project**.
6. Confirm `master` and `origin/master` now identify the same commit.

### VS Code

1. Use **Fetch** from Source Control or Source Control Graph.
2. Select the branch indicator and check out `master`.
3. Confirm **Changes** and **Staged Changes** are empty.
4. In Source Control Graph, compare `master` with `origin/master`.
5. If local `master` has no outgoing commits and the histories have not
   diverged, use **Pull**.
6. Confirm `master` and `origin/master` now identify the same commit.

Do not use VS Code's **Sync Changes** for this step. It combines incoming and
outgoing operations, while updating the shared base should never push something
by accident.

Stop if local `master` has an outgoing commit, the histories diverge, or the IDE
proposes an unexpected merge or rebase. Inspect the graph with the other
developer before changing history. Do not force-push `master`.

<details>
<summary>PowerShell fallback</summary>

```powershell
git fetch origin
git switch master
git status --short
git pull --ff-only
```

`--ff-only` stops instead of creating a merge when local and remote `master`
have diverged.

</details>

Confirm the active milestone in [MVP Scope](../project/mvp-scope.md). Only one
major milestone should drive feature work at a time.

## 3. Choose a task that is ready

Open the [board](../board.md) and choose one task from `To do`.

| Column        | Meaning                                                                |
|---------------|------------------------------------------------------------------------|
| Backlog       | Ideas and unapproved future work.                                      |
| To do         | Clear enough to start, with a goal and acceptance criteria.            |
| Doing         | Actively owned work.                                                   |
| Test / Review | Implemented work awaiting verification or review.                      |
| Done          | Complete, tested, committed, and safe for another contributor to pull. |

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

Create the branch from the updated local `master` before changing task files.

### Rider or WebStorm

Open the branch control, choose **New Branch**, enter the branch name, and check
out the new branch. Confirm the branch control shows the new name.

### VS Code

Select the branch indicator, choose **Create new branch**, enter the branch
name, and confirm the status bar shows it. The equivalent Command Palette action
is **Git: Create Branch**.

Use a type and subject that describe the work:

```text
feature/player-movement
feature/lab-blockout
fix/player-collision
docs/evergreen-v2
```

Keep one reviewable objective on the branch. Do not combine gameplay, project
settings, scene cleanup, and unrelated documentation because they happened in
the same session.

<details>
<summary>PowerShell fallback</summary>

```powershell
git switch -c feature/my-task
```

</details>

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

If the endpoint is missing, invalid, or unhealthy, follow the current
uncoordinated-save policy in the Unity Coordination Guide. Preserve local work
and use the agreed manual collaboration fallback for every protected-file edit.

Prefer scripts, ScriptableObjects, focused prefabs, prefab variants, UI prefabs,
and isolated test scenes when they let contributors work without sharing one
large scene edit.

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

Use
the [Coding and Implementation Guide](../guides/unity/coding-and-implementation.md)
for responsibility, dependency, testing, and debug decisions.

## 8. Verify the actual change

Choose proof from the files and behavior that changed:

| Change                             | Minimum evidence                                                                                             |
|------------------------------------|--------------------------------------------------------------------------------------------------------------|
| Markdown or docs configuration     | `npm test`, `npm run docs:build`, rendered page and navigation review.                                       |
| Pure C# or editor logic            | Relevant EditMode tests plus Unity compilation and Console review.                                           |
| Runtime or scene integration       | Relevant PlayMode tests, affected scene in Play Mode, and Console review.                                    |
| Scene, prefab, or inspector wiring | Open the asset, inspect references and overrides, run the affected behavior, and review the serialized diff. |
| Project settings or packages       | Reimport or restart when required, run affected behavior, and inspect every changed shared file.             |

For a normal Unity smoke test:

1. Wait for compilation and import to finish.
2. Open the affected scene. Use `Assets/Scenes/SampleScene.unity` for the
   current general smoke.
3. Enter Play Mode and exercise the acceptance criteria.
4. Check the Console for new relevant errors or warnings.
5. Exit Play Mode before reviewing serialized changes.
6. Run the relevant EditMode or PlayMode suite from
   `Window > General > Test Runner`.
7. Review the complete local change set in the IDE's diff view.

Passing a different suite does not prove the changed behavior. Report a baseline
failure separately instead of describing it as a regression or silently ignoring
it.

## 9. Select, commit, and push only the task

Review the proposed commit as a reviewer would. Confirm the branch name, file
list, individual diffs, test evidence, and commit message before committing.

### Rider or WebStorm changelists

1. Open the Commit tool window.
2. Keep task files together in a clearly named changelist when that helps
   separate them from unrelated local work.
3. Select only the files and hunks that belong to the task. Leave unrelated
   changes unselected.
4. Open the diff for every selected file.
5. Enter a conventional commit message such as
   `docs(workflow): explain IDE Git workflow`.
6. Choose **Commit**.
7. Open **Git > Push**, inspect the outgoing commit and target branch, then
   choose **Push**.

A changelist is organization, not isolation. Unselected changes remain in the
working tree and must not appear in the committed diff.

### VS Code staging

1. Open Source Control.
2. Stage each task file with **Stage Changes**. Use **Stage Selected Ranges**
   when one file also contains unrelated work.
3. Open every file under **Staged Changes** and review its staged diff.
4. Enter a conventional commit message and choose **Commit**.
5. Use **Publish Branch** for its first push, or use **Push** for an existing
   remote branch.

Do not use **Commit All** or stage the entire Changes group during normal task
work. Those shortcuts can include unrelated or generated files.

If push is rejected, stop. Fetch, inspect the graph, and coordinate before
integrating remote work. Do not accept an automatic update whose merge or rebase
result you have not reviewed. Never force-push a shared branch.

<details>
<summary>PowerShell fallback</summary>

```powershell
git status
git diff
git add path/to/file1 path/to/file2
git diff --staged
git commit -m "type(scope): describe the change"
git push -u origin feature/my-task
```

Do not use `git add .` for normal task work.

</details>

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
Move it to `Done` only after the accepted evidence exists and the branch is safe
for another contributor to fetch.

## 10. Review and merge through the IDE

The reviewer checks the acceptance criteria, risky shared files, test evidence,
and remaining limitations. A successful local run by the author does not replace
review of scene, prefab, package, project-setting, or Coordination changes.

### Review the feature branch

1. Start from a clean checkout and use **Fetch**.
2. Check out the remote feature branch as a local tracking branch.
3. Inspect its commits and diff against `master`.
4. Run the required automated and manual verification.
5. Confirm the branch has no uncommitted review artifacts before merging.

In JetBrains, use the branch control and Git Log. In VS Code, use the branch
indicator, **Git: Checkout to**, and Source Control Graph.

### Merge into local `master`

1. Check out `master`.
2. Fetch again, inspect `master` against `origin/master`, and update only when
   the histories have not diverged.
3. Merge the verified feature branch into the current `master`:
    - JetBrains: choose the feature branch and **Merge into Current**.
    - VS Code: run **Git: Merge Branch** and select the feature branch.
4. Review the resulting graph and changed files.
5. Rerun verification affected by the merge.
6. Push `master` with the IDE's separate **Push** action.
7. Confirm `origin/master` contains the reviewed result before deleting the
   local or remote feature branch.

Git may fast-forward when `master` is an ancestor of the feature branch. It may
create a merge commit when both histories contain work. Both outcomes are
acceptable when the resulting history and files are reviewed. Do not rebase or
otherwise rewrite a branch after another developer may have pulled it.

If a conflict appears, stop the merge until both developers understand the
competing changes. For Unity YAML, do not choose all of **Yours** or **Theirs**
merely to make the conflict dialog disappear. Follow
[Editor Safety](../guides/unity/editor-safety.md), reopen the result in Unity,
inspect references and overrides, and rerun the affected behavior.

<details>
<summary>PowerShell fallback</summary>

```powershell
git fetch origin
git switch --track origin/feature/my-task
# Run review and verification.
git switch master
git pull --ff-only
git merge feature/my-task
git push origin master
```

</details>

Keep `master` compiling, runnable, and playable. A rejected push, unexplained
divergence, or unresolved Unity conflict is a stop condition, not permission to
force the operation.

## Ownership and collaboration boundaries

The default split helps route decisions; it does not grant exclusive access:

| Area                        | Primary owner | Typical work                                                                         |
|-----------------------------|---------------|--------------------------------------------------------------------------------------|
| Gameplay and systems        | Developer A   | Controls, interactions, runtime state, scoring, disasters, technical debugging.      |
| World, UX, and presentation | Developer B   | Scene blockout, environment, UI, menus, audio, VFX, placeholder art, playtest notes. |

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
- [JetBrains commit and push reference](https://www.jetbrains.com/help/rider/Commit_and_push_changes.html?keymap=primary_intellij)
- [VS Code staging and commit reference](https://code.visualstudio.com/docs/sourcecontrol/staging-commits)
- [VS Code branch and merge reference](https://code.visualstudio.com/docs/sourcecontrol/branches-worktrees)
