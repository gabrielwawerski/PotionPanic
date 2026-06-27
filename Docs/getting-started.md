# Getting Started

This guide is for a first-time collaborator who is new to Unity, Rider, or both.

## Install the tools

Install these once on each machine:

- Unity Hub
- Unity Editor `6000.5.1f1`
- JetBrains Rider
- Git
- Git LFS
- Node.js and npm

## Clone and prep the repo

```powershell
git clone <repo-url>
cd PotionPanic
git lfs install
git status
```

Expected result:

- `git status` is clean after the clone.
- Generated folders such as `Library/`, `Temp/`, and `Logs/` are absent or ignored.

## Open the project in Unity

1. Open Unity Hub.
2. Add the repo root folder.
3. Open it with Unity `6000.5.1f1`.
4. Wait for package import and script compilation to finish.
5. Confirm the Console has no new compile errors.

Repo truth today:

- The current shared prototype scene is `Assets/Scenes/SampleScene.unity`.
- `Assets/Scenes/testscene.unity` exists, but do not treat it as the shared milestone scene unless the task says so.
- The docs describe `Laboratory.unity` as the intended canonical gameplay scene after Milestone 1.

## Open the code in Rider

1. Open `PotionPanic.sln` in Rider.
2. If Rider does not pick up Unity context, set Rider as the External Script Editor in Unity.
3. If the solution looks stale, use Unity's Open C# Project action to regenerate project files.

Good first checks:

- Rider shows the Unity project without unresolved core references.
- Changes made in Rider appear back in Unity after focus returns.

## Do a first smoke test

1. In Unity, open `Assets/Scenes/SampleScene.unity`.
2. Press Play.
3. Let the scene run long enough to confirm the editor entered Play Mode.
4. Open the Console and check for new errors.
5. Stop Play Mode.

If this fails, do not start feature work yet.

## Open the backlog board

For human use:

```powershell
.\scripts\setup-backlog.ps1
.\scripts\backlog-ui.ps1
```

If you install the Windows Startup autolaunch later, sign-in only starts the Backlog browser server in the background. It does not open the board automatically; run `.\scripts\backlog-ui.ps1` when you want the browser UI.

If you need MCP setup details for Codex or Gemini CLI, use the manual setup section in `README.md`.

## Know which files need coordination

Always announce before editing:

- `Assets/Scenes/*.unity`
- `Assets/**/*.prefab`
- `ProjectSettings/*`
- `Packages/manifest.json`
- `Packages/packages-lock.json`

These files are easy to conflict and hard to merge.

## Run tests and checks

Use this minimum verification loop before handing work off:

1. Wait for Unity compilation to finish.
2. Open the scene affected by the task.
3. Press Play.
4. Check the Console.
5. If the task changes tests or gameplay code, open `Window > General > Test Runner` and run the relevant `EditMode` or `PlayMode` suite.
6. In Git, review `git status` before committing.

## Common first-day problems

### Wrong Unity version

If Unity Hub opens the project with a different editor version, install and use `6000.5.1f1`.

### Rider project looks broken or stale

Open the project from Unity again after setting Rider as the external editor.

### `backlog` command is missing

Run `.\scripts\setup-backlog.ps1` first. It installs `backlog.md` if needed and prints the MCP commands.

### Generated folders appear in Git

Do not add them. Check `.gitignore` and confirm the path is one of `Library/`, `Temp/`, `obj/`, `Logs/`, or `UserSettings/`.

### Scene names do not match every doc yet

The repo still uses `SampleScene.unity` today. `Laboratory.unity` is the planned canonical scene name after Milestone 1.

## Before you pick a task

1. Read `Docs/team-workflow-guide.md`.
2. Confirm the current milestone.
3. Pick a small task from `To do`.
4. Tell the other collaborator which files, scenes, or prefabs you expect to touch.
5. Create a short-lived feature branch.
