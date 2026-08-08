# Project Setup

Complete this guide once on a new machine. You are ready to take work only
after the final checklist passes. For recurring work after setup, use
[Daily Workflow](../collaboration/team-workflow.md).

## Install the Tools

Install these once on each machine:

- Unity Hub
- Unity Editor 6000.5.1f1
- JetBrains Rider
- Git and Git LFS
- Node.js and npm

## Clone and Prepare the Repository

    git clone <repo-url>
    cd PotionPanic
    git lfs install
    git status

Git status should be clean after the clone. Generated Unity folders such as
Library/, Temp/, and Logs/ must be absent or ignored.

## Open the Docs Board

The local board is where the team picks, updates, and tracks tasks.

    npm install
    npm run docs:ui

If the browser does not open, use http://127.0.0.1:6420/PotionPanic/board. The
published docs site is read-only and suitable for browsing and sharing. See the
repository [README](https://github.com/gabrielwawerski/PotionPanic/blob/master/README.md)
for manual server commands and optional Windows startup management.

## Open the Project in Unity

1. Open Unity Hub and add the repository root folder.
2. Open it with Unity 6000.5.1f1.
3. Wait for package import and script compilation to finish.
4. Confirm that the Console has no new compile errors.

The current shared prototype scene is Assets/Scenes/SampleScene.unity.
Assets/Scenes/testscene.unity is not the shared milestone scene unless a task
explicitly says so. Laboratory.unity is the planned canonical gameplay scene
after Milestone 1.

## Open the Code in Rider

1. Open PotionPanic.sln in Rider.
2. If Rider lacks Unity context, set Rider as Unity's External Script Editor.
3. If the solution looks stale, use Unity's Open C# Project action.

Rider should show the Unity project without unresolved core references, and
changes made in Rider should return to Unity after focus changes.

## Run a First Smoke Test

1. In Unity, open Assets/Scenes/SampleScene.unity.
2. Press Play and confirm that Play Mode starts cleanly.
3. Check the Console for new errors.
4. Stop Play Mode.

Do not start feature work if this fails.

## Set Up Collaboration Safety

The Coordination window adds advisory presence and leases for protected scenes.
It supports the announcement workflow; it does not replace it.

1. Open Window > Potion Panic > Coordination.
2. Get a developer token from the operator through the approved secret channel.
3. Enter it only in Unity's credential prompt.
4. Confirm Connection becomes Connected.
5. Enter a short Task context.

Never put a token in local settings, Git, a URL, a ticket, or a log. Follow the
[Coordinated Leasing Guide](../guides/coordinated-leasing.md) before editing a
scene and whenever a Coordination control or warning is unfamiliar.

If the service is unavailable, use the local Disabled switch only after the
team agrees to a manual collaboration fallback. Preserve local work, announce
the protected-file edit, and reconnect after service health is restored.

## Ready-to-Work Checklist

You are ready to take a task when:

- Unity uses 6000.5.1f1 and opens the project without relevant Console errors.
- Rider opens PotionPanic.sln with Unity context.
- Assets/Scenes/SampleScene.unity enters Play Mode cleanly.
- Git LFS is installed and git status is understood.
- The local docs board opens.
- The Coordination window connects with your own credential.
- You know that scenes, prefabs, ProjectSettings, and package files need an
  announcement before editing.

## Common Setup Problems

### Wrong Unity version

Install and use 6000.5.1f1 when Unity Hub selects another editor version.

### Rider looks broken or stale

Set Rider as the External Script Editor and run Unity's Open C# Project.

### npm is missing

Install Node.js and npm, then rerun npm install.

### Generated folders appear in Git

Do not add them. Confirm the path is one of Library/, Temp/, obj/, Logs/, or
UserSettings/, then check .gitignore.

## Next Step

Open [Daily Workflow](../collaboration/team-workflow.md), confirm the current
milestone, and choose a small task from To Do.

## Related pages

- [Daily Workflow](../collaboration/team-workflow.md)
- [Coordinated Leasing](../guides/coordinated-leasing.md)
- [Project Overview](../project/)
