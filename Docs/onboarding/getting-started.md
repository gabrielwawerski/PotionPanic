# Project setup

Complete this guide once on each development machine. At the end, the local
checkout, editable documentation, Unity project, Rider integration, smoke-test
scene, and your Coordination identity should all work independently.

Use [Daily Workflow](../collaboration/team-workflow.md) after setup. It covers
recurring task work and assumes this guide already passes.

## What you are setting up

Potion Panic uses several tools with different responsibilities:

| Part | Responsibility | Stored where |
| --- | --- | --- |
| Git and Git LFS | Version source, Markdown, Unity assets, and large binary assets. | Local repository and remote Git host. |
| Docboard and VitePress | Render documentation and provide the editable local task board. | Sibling `Docboard` checkout plus this repository's `Docs/`. |
| Unity | Import assets, serialize scenes and prefabs, compile Unity assemblies, and run the game. | Repository plus generated local folders such as `Library/`. |
| Rider | Edit C# through the Unity-generated solution and project files. | `PotionPanic.sln` and generated project files. |
| Coordination | Show scene presence and claims and guard coordinated saves. | Unity editor, Windows Credential Manager, and the remote service. |

A failure in one part does not automatically mean the others are broken. For
example, the published docs can remain readable while the local Docboard
checkout is missing, and Rider can open a stale solution while Unity is still
importing packages.

## Install the tools

Install these on the machine:

- Unity Hub
- Unity Editor `6000.5.1f1`
- JetBrains Rider
- Git
- Git LFS
- Node.js with npm

Verify the command-line tools in PowerShell:

```powershell
git --version
git lfs version
node --version
npm --version
```

Each command must print a version. The repository does not currently pin an
exact Node.js version, but Node and npm must be recent enough to install the
checked-in lockfile and run VitePress.

## Prepare the sibling checkouts

`PotionPanic/package.json` uses this local dependency:

```json
"@gabrielwawerski/docboard": "file:../Docboard"
```

The directory layout therefore matters. Clone both repositories under the same
parent directory and keep the names `Docboard` and `PotionPanic`:

```text
C:\Dev\
  Docboard\
  PotionPanic\
```

Example clone sequence using the team-provided repository URLs:

```powershell
Set-Location C:\Dev
git clone <docboard-repo-url> Docboard
git clone <potionpanic-repo-url> PotionPanic
Set-Location PotionPanic
git lfs install
git status
```

Expected result:

- `git status` reports a clean checkout.
- `git lfs install` completes without an error.
- `C:\Dev\Docboard\package.json` exists.
- Generated Unity folders such as `Library/`, `Temp/`, and `Logs/` are absent
  before the first Unity import or remain ignored afterward.

If your repositories live under another parent directory, the same sibling
relationship still applies. Do not replace the package dependency with a
machine-specific absolute path.

## Install and open the local docs

From the PotionPanic root:

```powershell
npm install
npm run docs:ui
```

`npm install` installs VitePress and resolves the reusable board tooling from
the sibling Docboard checkout. `docs:ui` starts the docs server when necessary
and opens the editable board.

Expected result:

- The terminal reports a VitePress server on port `6420`.
- The browser opens
  `http://127.0.0.1:6420/PotionPanic/board`.
- Board edits made through the local site can update repository Markdown.

If the browser does not open, enter the URL directly. The published site at
<https://gabrielwawerski.github.io/PotionPanic/> is useful for reading and
sharing, but it is read-only.

Use these commands when diagnosing the local server:

```powershell
npm run docs:dev:local
npm run docs:build
npm run docs:stop
```

- `docs:dev:local` exposes the site only on this machine.
- `docs:build` proves VitePress can resolve the current configuration, pages,
  and links.
- `docs:stop` stops the process listening on port `6420`.

If `npm install` cannot find `../Docboard`, fix the sibling checkout. Repeated
installs cannot repair a missing local package source.

## Open the Unity project

1. Open Unity Hub.
2. Add the `PotionPanic` repository root, not the `Assets/` directory.
3. Open it with Unity `6000.5.1f1`.
4. Wait for package import and script compilation to finish.
5. Open the Console and confirm there are no new compile errors.

The first import creates local folders such as `Library/`, `Temp/`, and
`UserSettings/`. They are machine state, not project source, and must stay out
of Git.

Current scene names:

- `Assets/Scenes/SampleScene.unity` is the shared smoke-test scene.
- `Assets/Scenes/testscene.unity` is not the milestone scene unless a task says
  otherwise.
- `Laboratory.unity` is the accepted Milestone 1 target name. It is not the
  current shared scene until the milestone task performs and verifies the
  rename or replacement.

Stop setup and fix compilation before feature work if Unity reports missing
packages, assembly errors, or a failed import.

## Connect Rider to Unity

1. Open `PotionPanic.sln` in Rider.
2. In Unity, confirm Rider is the External Script Editor.
3. If Rider lacks Unity context or shows stale projects, use Unity's
   `Open C# Project` action to regenerate the solution.
4. Wait for Rider indexing to finish before treating unresolved references as
   code defects.

The solution is generated from Unity assemblies. Editing it does not configure
the Unity project by itself. Unity remains the authority for installed packages,
assembly definitions, scenes, and serialized references.

Expected result:

- Rider recognizes the Unity project and its assemblies.
- Core Unity types resolve.
- Saving a C# file and returning focus to Unity triggers compilation.

## Run the first smoke test

1. In Unity, open `Assets/Scenes/SampleScene.unity`.
2. Wait for compilation to finish.
3. Press Play.
4. Confirm Unity enters Play Mode and produces no new relevant Console errors.
5. Stop Play Mode.

This smoke test proves that the current checkout can import and run its shared
scene on this machine. It does not prove that future gameplay features or the
remote Coordination service work.

Do not start a task if the scene cannot open, Unity cannot enter Play Mode, or
the baseline already contains unexplained errors. Record the failure so it is
not mistaken for a regression from later work.

## Set up your Coordination identity

The Unity Coordination tool is an advisory safety layer for shared scene work.
It complements a direct team announcement; it does not lock files or replace
Git.

1. Open `Window > Potion Panic > Coordination`.
2. Ask the operator for your developer token through the approved secret
   channel.
3. Enter the token only in Unity's credential prompt.
4. Confirm `Connection` becomes `Connected`.
5. Enter a short task context so other contributors can understand your claims.

The developer token belongs in Windows Credential Manager. The session token
is memory-only. Never copy either into Git, local JSON settings, a URL, a
ticket, a log, or ordinary chat.

If authentication fails, use `Forget credentials`, obtain a newly issued token,
and authenticate again. Follow the
[Unity Coordination Guide](../guides/coordinated-leasing.md) before editing a
coordinated scene or when a window state is unfamiliar.

If the service is unavailable, use the local Disabled switch only after the
team agrees to a manual collaboration fallback. Preserve local work, announce
the protected-file edit, and reconnect after service health is restored.

## Ready-to-work checklist

The machine is ready when all of these statements are true:

- Git, Git LFS, Node.js, npm, Unity Hub, Unity `6000.5.1f1`, and Rider are
  installed.
- Docboard and PotionPanic are sibling checkouts.
- `npm install`, the local board, and `npm run docs:build` work.
- Unity imports the repository without relevant compile errors.
- Rider resolves the Unity assemblies.
- `Assets/Scenes/SampleScene.unity` enters Play Mode cleanly.
- `git status` contains no unexplained generated files.
- The Coordination window recognizes your own credential.
- You know that scenes, prefabs, `ProjectSettings/`, and package files require
  an announcement before editing.

## Common setup failures

### Unity Hub selects another editor

Install `6000.5.1f1` and explicitly select it for this project. The recorded
version comes from `ProjectSettings/ProjectVersion.txt`.

### Rider shows stale or missing Unity references

Wait for Unity compilation, set Rider as the external editor, and regenerate
the C# project from Unity. Opening the solution repeatedly does not repair a
failed Unity import.

### npm cannot resolve Docboard

Confirm the sibling path is exactly `../Docboard` from the PotionPanic root and
that its `package.json` exists.

### Port 6420 is already in use

Run `npm run docs:stop`, then start the local server again. Do not terminate an
unknown process until you identify it.

### Generated folders appear in Git

Do not stage them. Confirm the path is generated, inspect `.gitignore`, and
remove it from the proposed change rather than committing machine state.

### Scene names differ from the target docs

Use the current repository path, `Assets/Scenes/SampleScene.unity`, until an
accepted task changes it. Target design does not prove implementation state.

## Next step

Open [Daily Workflow](../collaboration/team-workflow.md), confirm the active
milestone, and choose a small task from the board.

## Related pages

- [Daily Workflow](../collaboration/team-workflow.md)
- [Unity Coordination Guide](../guides/coordinated-leasing.md)
- [Project Overview](../project/)
