# Getting Started

This guide is for a first-time collaborator who is new to Unity, Rider, or
both.

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
- Generated folders such as `Library/`, `Temp/`, and `Logs/` are absent or
  ignored.

## Open the project in Unity

1. Open Unity Hub.
2. Add the repo root folder.
3. Open it with Unity `6000.5.1f1`.
4. Wait for package import and script compilation to finish.
5. Confirm the Console has no new compile errors.

Repo truth today:

- The current shared prototype scene is `Assets/Scenes/SampleScene.unity`.
- `Assets/Scenes/testscene.unity` exists, but do not treat it as the shared
  milestone scene unless the task says so.
- `Laboratory.unity` is the planned canonical gameplay scene after
  Milestone 1.

## Open the code in Rider

1. Open `PotionPanic.sln` in Rider.
2. If Rider does not pick up Unity context, set Rider as the External Script
   Editor in Unity.
3. If the solution looks stale, use Unity's `Open C# Project` action to
   regenerate project files.

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

## Open the docs board

The board is the default place to pick, update, and track tasks.

For human use:

```powershell
npm install
npm run docs:ui
```

Equivalent manual command:

```powershell
npm run docs:dev
```

Open `http://127.0.0.1:6420/board` if the browser does not open automatically.

If you want Windows sign-in to start the local docs server without opening a
browser:

```powershell
npm run docs:startup:install
```

This installs a per-user Startup shortcut that runs the LAN-accessible server
with `npm run docs:dev`. Remove it later with:

```powershell
npm run docs:stop
npm run docs:startup:uninstall
```

Use `npm run docs:stop` whenever you want to shut down the background docs
server without hunting for the process manually.

If you add or rename a Markdown page under the normal docs folders while
`npm run docs:dev` is running, the auto-generated sidebar should refresh
without a manual server restart. The top navigation bar is still manually
curated.

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
5. If the task changes tests or gameplay code, open
   `Window > General > Test Runner` and run the relevant `EditMode` or
   `PlayMode` suite.
6. In Git, review `git status` before committing.

## Common first-day problems

### Wrong Unity version

If Unity Hub opens the project with a different editor version, install and use
`6000.5.1f1`.

### Rider project looks broken or stale

Open the project from Unity again after setting Rider as the external editor.

### `npm` is missing

Install Node.js and npm, then rerun `npm install`.

### Generated folders appear in Git

Do not add them. Check `.gitignore` and confirm the path is one of
`Library/`, `Temp/`, `obj/`, `Logs/`, or `UserSettings/`.

### Scene names do not match every doc yet

The repo still uses `SampleScene.unity` today. `Laboratory.unity` is the
planned canonical scene name after Milestone 1.

## Before you pick a task

1. Read [`../collaboration/team-workflow.md`](../collaboration/team-workflow.md).
2. Confirm the current milestone in
   [`../project/mvp-scope.md`](../project/mvp-scope.md).
3. Open the board and pick a small task from `To Do`.
4. Tell the other collaborator which files, scenes, or prefabs you expect to
   touch.
5. Create a short-lived feature branch.
