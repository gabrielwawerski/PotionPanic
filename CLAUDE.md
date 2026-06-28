# Repository Guidelines

## Project Structure & Module Organization

Keep gameplay code in `Assets/Scripts/Runtime` and editor-only helpers in
`Assets/Scripts/Editor`. Put automated tests in `Assets/Tests/EditMode` and
`Assets/Tests/PlayMode`, scenes in `Assets/Scenes`, shared render settings in
`Assets/Settings`, and longer project docs in `Docs/`. Treat `ProjectSettings/`
and `Packages/` as shared configuration. The current shared prototype scene is
`Assets/Scenes/SampleScene.unity`; docs also reference the planned rename to
`Laboratory.unity`.

## Build, Test, And Development Commands

Use PowerShell from the repo root.

- `git lfs install` sets up required asset handling on a new machine.
- `.\Scripts\setup-docs.ps1` installs or verifies the VitePress docs tooling.
- `.\Scripts\docs-ui.ps1` opens the shared editable board in the browser.
- `npm run docs:dev` starts the local docs server manually.
- `npm run docs:build` builds the static docs site for verification.
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

Recent history uses conventional-style commits such as `feat(scripts): ...` and
`docs(workflow): ...`; keep that format with an imperative summary. Work on
short-lived `feature/...` or `fix/...` branches and stage specific files
instead of `git add .`. In reviews or PRs, list the task or milestone, affected
scenes/prefabs/settings, local test evidence, and any remaining risk. Announce
before editing `Assets/Scenes/*.unity`, `Assets/**/*.prefab`,
`ProjectSettings/*`, or `Packages/*`, and never commit generated folders like
`Library/`, `Temp/`, `Logs/`, `obj/`, or `UserSettings/`.
