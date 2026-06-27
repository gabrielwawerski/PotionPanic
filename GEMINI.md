

<!-- BACKLOG.MD MCP GUIDELINES START -->

<CRITICAL_INSTRUCTION>

## BACKLOG WORKFLOW INSTRUCTIONS

This project uses Backlog.md MCP for all task and project management activities.

**CRITICAL GUIDANCE**

- If your client supports MCP resources, read `backlog://workflow/overview` to understand when and how to use Backlog for this project.
- If your client only supports tools or the above request fails, call `backlog.get_backlog_instructions()` to load the tool-oriented overview. Use the `instruction` selector when you need `task-creation`, `task-execution`, or `task-finalization`.

- **First time working here?** Read the overview resource IMMEDIATELY to learn the workflow
- **Already familiar?** You should have the overview cached ("## Backlog.md Overview (MCP)")
- **When to read it**: BEFORE creating tasks, or when you're unsure whether to track work

These guides cover:
- Decision framework for when to create tasks
- Search-first workflow to avoid duplicates
- Links to detailed guides for task creation, execution, and finalization
- MCP tools reference

You MUST read the overview resource to understand the complete workflow. The information is NOT summarized here.

</CRITICAL_INSTRUCTION>

<!-- BACKLOG.MD MCP GUIDELINES END -->

# Repository Guidelines

## Project Structure & Module Organization

Keep gameplay code in `Assets/Scripts/Runtime` and editor-only helpers in
`Assets/Scripts/Editor`. Put automated tests in `Assets/Tests/EditMode` and
`Assets/Tests/PlayMode`, scenes in `Assets/Scenes`, shared render settings in
`Assets/Settings`, and longer project docs in `Docs/`. Treat `ProjectSettings/`
and `Packages/` as shared configuration. The current shared prototype scene is
`Assets/Scenes/SampleScene.unity`; docs also reference the planned rename to
`Laboratory.unity`.

## Build, Test, and Development Commands

Use PowerShell from the repo root.

- `git lfs install` sets up required asset handling on a new machine.
- `.\scripts\setup-backlog.ps1` installs or verifies Backlog.md tooling.
- `.\scripts\backlog-ui.ps1` opens the shared backlog board in the browser.
- Open the repo in Unity Hub with editor `6000.5.1f1`, then open
  `PotionPanic.sln` in Rider for code work.
- For a smoke test, open `Assets/Scenes/SampleScene.unity`, press Play, and
  confirm no new Console errors.

## Coding Style & Naming Conventions

Follow `.editorconfig`: UTF-8, LF endings, and 2-space indentation for `*.cs`,
JSON, YAML, and Unity config files. Keep C# lines near the 90-character limit.
Use PascalCase for classes, asmdefs, and public members. Match namespaces to
the existing `PotionPanic.*` pattern. Keep runtime and editor code in separate
assemblies, and avoid mixing gameplay changes with scene/prefab edits unless
the task needs both.

## Testing Guidelines

Use EditMode tests for pure logic and editor-facing behavior; use PlayMode
tests for scene or runtime integration. Before handoff, wait for Unity
compilation to finish, run the affected scene in Play Mode, review the Console,
and run the relevant suite from `Window > General > Test Runner`. Name new test
files after the subject under test, such as `PlayerMovementTests.cs`.

## Commit & Review Guidelines

Recent history uses conventional-style commits such as
`feat(scripts): ...` and `chore(backlog): ...`; keep that format with an
imperative summary. Work on short-lived `feature/...` or `fix/...` branches and
stage specific files instead of `git add .`. In reviews or PRs, list the task
or milestone, affected scenes/prefabs/settings, local test evidence, and any
remaining risk. Announce before editing `Assets/Scenes/*.unity`,
`Assets/**/*.prefab`, `ProjectSettings/*`, or `Packages/*`, and never commit
generated folders like `Library/`, `Temp/`, `Logs/`, `obj/`, or `UserSettings/`.
