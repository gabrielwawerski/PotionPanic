---
title: 'Coordinated File Leasing Remaining Hardening Implementation Plan'
status: archived
archivedAt: 2026-08-08
originalPath: 'Docs/plans/coordinated-file-leasing-remaining-hardening.md'
supersededBy: '../coordinated-file-leasing-release-acceptance.md'
---

# Coordinated File Leasing Remaining Hardening Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete the Unity-side protocol hardening, release configuration,
verification, documentation, and manual Cloudflare acceptance needed to close
PP-7 without changing the placeholder endpoint before a real deployment.

**Architecture:** The in-progress backend Tasks 1 and 2 own server-side
project routing, bounded snapshot production and state capacity, indexed token
lookups and session limits, and revocation cleanup. This plan consumes that
contract. Unity parses each 16 KiB snapshot envelope but buffers and applies a
snapshot only after one bounded, internally consistent set is complete; request
completion follows that atomic apply. Wrangler declares the Worker
declaratively and requires its secrets, while deployment and cross-network
acceptance remain manual actions performed by an authenticated operator.

**Tech Stack:** Unity `6000.5.1f1`, C# editor scripting and Unity Test
Framework EditMode tests, TypeScript, Vitest, Cloudflare Workers, Durable
Objects, Wrangler `4.120.0`, PowerShell, and VitePress.

## Global Constraints

- Start only after the backend Tasks 1 and 2 changes are present and their
  focused Worker tests are green; do not duplicate, revert, or fold those
  changes into a Unity task.
- Preserve unrelated worktree changes. Do not edit scenes, prefabs,
  `ProjectSettings/`, or `Packages/` for this plan.
- Each WebSocket envelope is at most 16 KiB UTF-8. A client snapshot buffer is
  at most 256 KiB UTF-8; the backend rejects state-growing requests whose
  resulting snapshot would exceed that aggregate limit.
- `path` permits NFC normalization and slash normalization, then folds only
  ASCII `A` through `Z`. It must not apply locale-sensitive or full-Unicode
  case folding.
- Branch, task context, UI input, local settings, and serialized Git/task
  metadata are each limited to 256 UTF-16 code units. `string.Length` is the
  required C# measure; JavaScript `string.length` is the required TypeScript
  measure.
- Tokens, sessions, `.dev.vars`, `UserSettings/`, logs, caches, and generated
  lease state remain untracked. GitHub Actions verify code only and never
  authenticate to or deploy Cloudflare.
- Keep `coordination.json` on
  `https://potion-panic-coordination.example.workers.dev` until an authenticated
  `wrangler deploy` returns the actual `workers.dev` URL.
- Do not mark PP-7 complete, archive a plan, or describe deployment or
  two-machine acceptance as complete without dated evidence from both machines.

Define this PowerShell helper once before running any Unity test command in
this plan. Do not add `-quit`; Unity `6000.5.1f1` can exit before the Test Runner
writes results when `-quit` is combined with `-runTests`.

```powershell
function Invoke-CoordinationEditMode {
  param(
    [Parameter(Mandatory)] [string] $Filter,
    [Parameter(Mandatory)] [string] $Results,
    [Parameter(Mandatory)] [string] $Log
  )

  $unity = 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe'
  $arguments = @(
    '-batchmode', '-nographics', '-projectPath', (Get-Location).Path,
    '-runTests', '-testPlatform', 'editmode', '-testFilter', $Filter,
    '-testResults', (Join-Path (Get-Location) $Results),
    '-logFile', (Join-Path (Get-Location) $Log)
  )
  $start = @{
    FilePath = $unity
    ArgumentList = $arguments
    Wait = $true
    PassThru = $true
    WindowStyle = 'Hidden'
  }
  $process = Start-Process @start
  return $process.ExitCode
}
```

---

## File Map and Ownership

| File | Responsibility in this remaining work |
| --- | --- |
| `Assets/Scripts/Editor/Coordination/CoordinationProtocol.cs` | Snapshot chunk DTO fields, chunk metadata validation, and the public 16 KiB/256 KiB limits consumed by Unity. |
| `Assets/Scripts/Editor/Coordination/CoordinationSnapshotAssembler.cs` | One bounded, atomic in-memory assembly for one snapshot ID at a time. |
| `Assets/Scripts/Editor/Coordination/CoordinationService.cs` | Route chunks through the assembler, apply a completed snapshot once, drain request handles once on close, bound outbound metadata, and reconnect after a credential save. |
| `Assets/Scripts/Editor/Coordination/CoordinationPathMatcher.cs` | Shared path normalization and ASCII-only canonical folding. |
| `Assets/Scripts/Editor/Coordination/CoordinationEditorInfrastructure.cs` | Bound the Git branch before it can become serialized protocol context. |
| `Assets/Scripts/Editor/Coordination/CoordinationUserSettings.cs` | Reject and refuse to serialize an over-limit local task context. |
| `Assets/Scripts/Editor/Coordination/CoordinationCredentialWindow.cs` | Invoke a supplied callback only after Credential Manager has stored the token. |
| `Assets/Scripts/Editor/Coordination/CoordinationWindow.cs` and `CoordinationWindowViewModel.cs` | Limit the task-context editor control and clamp programmatic updates before saving. |
| `Assets/Tests/EditMode/Coordination/CoordinationSnapshotAssemblerTests.cs` | Assembly-level proof of ordering, duplicate, incomplete, inconsistent, aggregate-cap, and atomic-completion behavior. |
| `Assets/Tests/EditMode/Coordination/CoordinationProtocolTests.cs`, `CoordinationServiceTests.cs`, `CoordinationPathMatcherTests.cs`, `CoordinationUserSettingsTests.cs`, `CoordinationWindowViewModelTests.cs` | Contract and integration regression coverage for limits, close draining, credential save, and shared path vectors. |
| `Tools/CoordinationServer/test/fixtures/canonical-path-vectors.json` | The single tracked Unicode path-vector source consumed by both Vitest and Unity tests. |
| `Tools/CoordinationServer/test/protocol.test.ts` | Consume the shared path fixture rather than maintaining a TypeScript-only vector list. |
| `Tools/CoordinationServer/wrangler.jsonc` | Declarative Durable Object export, required secret names, public `workers.dev`, disabled preview URLs, and full observability. |
| `Tools/CoordinationServer/.dev.vars.example` | Tracked local-secret shape with no secret values. |
| `Tools/CoordinationServer/README.md`, `README.md`, [Docs/onboarding/getting-started.md](../../onboarding/getting-started.md), [Docs/collaboration/team-workflow.md](../../collaboration/team-workflow.md), [Docs/unity-guides/editor-safety.md](../../unity-guides/editor-safety.md) | Server operation and evergreen user guidance. |
| [Docs/tickets/PP-7.md](../../tickets/PP-7.md) | Dated commands, results, reviewer result, external blockers, and later two-machine acceptance evidence. |
| `coordination.json` | Replace the example endpoint only after an actual authenticated deployment has supplied the exact URL. |
| `.github/workflows/coordination-server.yml` | Remains verification-only: `npm ci`, typecheck, tests, and `wrangler deploy --dry-run`; no Cloudflare credential, secret, or deployment step. |

## Task 1: Assemble and Atomically Apply Unity Snapshot Chunks

**Files:**

- Create: `Assets/Scripts/Editor/Coordination/CoordinationSnapshotAssembler.cs`
- Create: `Assets/Tests/EditMode/Coordination/CoordinationSnapshotAssemblerTests.cs`
- Modify: `Assets/Scripts/Editor/Coordination/CoordinationProtocol.cs`
- Modify: `Assets/Scripts/Editor/Coordination/CoordinationService.cs`
- Modify: `Assets/Tests/EditMode/Coordination/CoordinationProtocolTests.cs`
- Modify: `Assets/Tests/EditMode/Coordination/CoordinationServiceTests.cs`

**Interfaces:**

- Consumes: each backend `snapshot` envelope with `snapshotId: string`,
  `chunkIndex: int`, `chunkCount: int`, identical `stateVersion`, optional
  `requestId`, `presence`, `leases`, and `serverTime`.
- Produces:

  ```csharp
  public enum CoordinationSnapshotAssemblyStatus
  {
    Awaiting,
    Duplicate,
    Completed,
    Rejected
  }

  public sealed class CoordinationSnapshotAssembler
  {
    public const int MaximumAggregateBytes = 256 * 1024;
    public CoordinationSnapshotAssemblyStatus TryAdd(
      CoordinationServerEnvelope chunk,
      int serializedUtf8Bytes,
      out CoordinationServerEnvelope completed,
      out string error);
    public void Reset();
  }
  ```

  `completed` is a synthetic `type == "snapshot"` envelope with the complete
  presence and lease arrays, the original ID, request ID, state version, and
  server time. It is non-null only for `Completed`.

- [ ] **Step 1: Write failing chunk-contract and assembler tests.**

  Extend `CoordinationProtocolTests` so a snapshot without all six chunk fields
  is rejected and a single valid chunk parses. Add these tests to
  `CoordinationSnapshotAssemblerTests` using two synthetically valid chunks.
  Define `private const string SnapshotId =
  "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";` in that fixture:

  ```csharp
  [Test]
  public void CompletesOutOfOrderChunksWithoutApplyingTheFirstChunk()
  {
    var assembler = new CoordinationSnapshotAssembler();
    var second = Snapshot("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa", 1, 2, 8);
    var first = Snapshot("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa", 0, 2, 8);

    Assert.That(assembler.TryAdd(second, 128, out var early, out _),
      Is.EqualTo(CoordinationSnapshotAssemblyStatus.Awaiting));
    Assert.That(early, Is.Null);
    Assert.That(assembler.TryAdd(first, 128, out var completed, out _),
      Is.EqualTo(CoordinationSnapshotAssemblyStatus.Completed));
    Assert.That(completed.presence, Has.Length.EqualTo(2));
  }

  [Test]
  public void RejectsInconsistentMetadataAndDropsThePartialAssembly()
  {
    var assembler = new CoordinationSnapshotAssembler();
    Assert.That(assembler.TryAdd(Snapshot(SnapshotId, 0, 2, 8), 128, out _, out _),
      Is.EqualTo(CoordinationSnapshotAssemblyStatus.Awaiting));
    Assert.That(assembler.TryAdd(Snapshot(SnapshotId, 1, 3, 8), 128, out _, out var error),
      Is.EqualTo(CoordinationSnapshotAssemblyStatus.Rejected));
    Assert.That(error, Is.EqualTo("snapshot_metadata_inconsistent"));
  }
  ```

  Add cases for an exact duplicate index, a conflicting duplicate index,
  incomplete assembly, a different snapshot ID replacing an existing partial
  assembly without applying it, and a cumulative `262145` byte input. Exact duplicate data returns
  `Duplicate` without changing byte count; conflicting data and any aggregate
  breach return `Rejected`, clear the partial assembly, and produce no completed
  envelope.

  In `CoordinationServiceTests`, send a correlated two-chunk snapshot in
  reversed order and assert `SnapshotReceived == 1`,
  `RequestCompleted == 1`, and `NewestAppliedStateVersion` changes only after
  the second chunk. Send only the first chunk and assert all three counts stay
  at zero. Send the same completed set again and assert the request cannot
  complete a second time.

- [ ] **Step 2: Run the new tests and verify the expected RED state.**

  Run from the repository root:

  ```powershell
  $exitCode = Invoke-CoordinationEditMode -Filter 'PotionPanic.Tests.EditMode.Coordination.CoordinationSnapshotAssemblerTests' -Results 'Logs\coordination-snapshot-red.xml' -Log 'Logs\coordination-snapshot-red.log'
  if ($exitCode -eq 0) { throw 'Expected the snapshot assembler RED run to fail.' }
  ```

  Expected: the test run fails because `CoordinationSnapshotAssembler` and the
  snapshot metadata fields do not yet exist.

- [ ] **Step 3: Add the DTO validation and bounded assembler.**

  Add `snapshotId`, `chunkIndex`, and `chunkCount` to
  `CoordinationServerEnvelope`. In `HasRequiredServerFields`, require a UUID v4
  snapshot ID, `chunkIndex >= 0`, `chunkCount > 0`, `chunkIndex < chunkCount`,
  a non-null presence array, a non-null lease array, and non-null server time.
  Do not change the 16 KiB `Encoding.UTF8.GetByteCount(json)` gate.

  Implement `CoordinationSnapshotAssembler` with one `SnapshotAssembly` that
  holds its ID, count, state version, request ID presence and value, server
  time, per-index `CoordinationServerEnvelope` values, and cumulative UTF-8
  bytes. Compare metadata exactly, including whether `requestId` was omitted or
  supplied. Accept indexes in any order. When an index already exists, compare
  the original serialized chunk bytes or a canonical serialization of every
  DTO field; an identical payload returns `Duplicate`, while a differing
  payload clears the assembly and returns
  `snapshot_duplicate_inconsistent`. A chunk with a different snapshot ID
  replaces the incomplete assembly and becomes the first chunk of the new
  assembly; the discarded partial snapshot never mutates protocol state. Reject
  a total over `MaximumAggregateBytes` with `snapshot_aggregate_too_large` and
  clear the partial assembly.

  On completion, concatenate presence and lease arrays in ascending
  `chunkIndex`, return one synthetic snapshot, then clear the stored assembly.
  The assembler must never call `CoordinationProtocolState` or publish an
  event itself.

- [ ] **Step 4: Route snapshots through the assembler before state application.**

  In `CoordinationService.ApplySocketMessage`, parse the raw JSON first. For
  `type == "snapshot"`, calculate `Encoding.UTF8.GetByteCount(json)` and call
  `snapshotAssembler.TryAdd`. Publish an `error` envelope with the returned
  code only for `Rejected`; return immediately for `Awaiting` and `Duplicate`.
  For `Completed`, replace the parsed chunk with `completed`, then execute the
  existing stale-version check, `protocolState.TryApplyServerEnvelope`,
  `SnapshotReceived`, and `CompleteRequest` exactly once. A stale completed
  snapshot still calls `CompleteRequest(completed, true)` once and does not
  publish `SnapshotReceived`.

  Call `snapshotAssembler.Reset()` when the socket closes, credentials are
  forgotten, the service is disabled, or it shuts down. Do not reset it merely
  because an unrelated state-carrying envelope arrives.

- [ ] **Step 5: Run the focused GREEN tests.**

  ```powershell
  if ((Invoke-CoordinationEditMode -Filter 'PotionPanic.Tests.EditMode.Coordination.CoordinationSnapshotAssemblerTests' -Results 'Logs\coordination-snapshot-green.xml' -Log 'Logs\coordination-snapshot-green.log') -ne 0) { throw 'Snapshot assembler tests failed.' }
  if ((Invoke-CoordinationEditMode -Filter 'PotionPanic.Tests.EditMode.Coordination.CoordinationServiceTests' -Results 'Logs\coordination-service-snapshot-green.xml' -Log 'Logs\coordination-service-snapshot-green.log') -ne 0) { throw 'Coordination service snapshot tests failed.' }
  ```

  Expected: both fixtures pass with zero failures, skips, or inconclusive
  tests. The logs contain no new C# compiler error.

- [ ] **Step 6: Commit the coherent Unity chunk change.**

  ```powershell
  git add Assets/Scripts/Editor/Coordination/CoordinationProtocol.cs Assets/Scripts/Editor/Coordination/CoordinationSnapshotAssembler.cs Assets/Scripts/Editor/Coordination/CoordinationService.cs Assets/Tests/EditMode/Coordination/CoordinationProtocolTests.cs Assets/Tests/EditMode/Coordination/CoordinationSnapshotAssemblerTests.cs Assets/Tests/EditMode/Coordination/CoordinationServiceTests.cs
  git commit -m "fix(coordination): assemble bounded snapshot chunks"
  ```

## Task 2: Bound Unity Metadata, Drain Requests, Reconnect on Credential Save, and Share Path Vectors

**Files:**

- Create: `Tools/CoordinationServer/test/fixtures/canonical-path-vectors.json`
- Modify: `Assets/Scripts/Editor/Coordination/CoordinationService.cs`
- Modify: `Assets/Scripts/Editor/Coordination/CoordinationCredentialWindow.cs`
- Modify: `Assets/Scripts/Editor/Coordination/CoordinationEditorInfrastructure.cs`
- Modify: `Assets/Scripts/Editor/Coordination/CoordinationPathMatcher.cs`
- Modify: `Assets/Scripts/Editor/Coordination/CoordinationUserSettings.cs`
- Modify: `Assets/Scripts/Editor/Coordination/CoordinationWindow.cs`
- Modify: `Assets/Scripts/Editor/Coordination/CoordinationWindowViewModel.cs`
- Modify: `Assets/Tests/EditMode/Coordination/CoordinationServiceTests.cs`
- Modify: `Assets/Tests/EditMode/Coordination/CoordinationPathMatcherTests.cs`
- Modify: `Assets/Tests/EditMode/Coordination/CoordinationUserSettingsTests.cs`
- Modify: `Assets/Tests/EditMode/Coordination/CoordinationWindowViewModelTests.cs`
- Modify: `Tools/CoordinationServer/test/protocol.test.ts`

**Interfaces:**

- Consumes: `ICoordinationWebSocketClient.Closed`, the existing
  `RequestSendFailed` event, `ICoordinationGitContext.GetBranch()`, local
  `CoordinationUserSettings.taskContext`, and `CoordinationCredentialWindow`.
- Produces:

  ```csharp
  public static class CoordinationProtocol
  {
    public const int MaximumContextLength = 256;
    public static bool IsValidContext(string value);
    public static string ClampContext(string value);
  }

  public static void ShowForProject(
    string projectId,
    ICredentialStore credentialStore,
    Action credentialsSaved);
  ```

  `credentialsSaved` runs only after `ICredentialStore.Write` returns. The
  default service supplies a callback that starts one connection attempt.

- [ ] **Step 1: Write failing close, metadata, credential, and shared-vector tests.**

  Add service tests that queue two requests, raise `Closed` twice, execute the
  queued dispatcher, and assert two `RequestSendFailed` events with
  `"The coordination socket closed."`, no `RequestCompleted` event, and no
  duplicate notification. Add a save-token test that begins with an empty
  `MemoryCredentialStore`, invokes the callback captured by the credential
  request delegate after writing a token, and asserts exactly one HTTP session
  request and one WebSocket connect attempt.

  Add these limit tests:

  ```csharp
  [Test]
  public void Rejects257Utf16CodeUnitsInLocalTaskContext()
  {
    var json = "{\"schemaVersion\":1,\"serverBaseUrlOverride\":\"\",\"taskContext\":\""
      + new string('x', 257) + "\",\"disabled\":false}";
    Assert.That(CoordinationUserSettings.TryParse(json, out _, out _), Is.False);
  }

  [Test]
  public void CountsSurrogatePairsAsTwoUtf16CodeUnits()
  {
    Assert.That(CoordinationProtocol.IsValidContext(string.Empty), Is.True);
    Assert.That(CoordinationProtocol.IsValidContext(string.Concat(
      Enumerable.Repeat("\ud83d\ude00", 129))), Is.False);
  }
  ```

  Use `string.Empty` as the empty-string control in the test; use
  `string.Concat(Enumerable.Repeat("\ud83d\ude00", 128))` for the accepted
  256-unit case and `129` for rejection. Add assertions that an over-limit
  view-model assignment is clamped to a valid UTF-16 boundary and persisted,
  and that over-limit Git branch and task values are clamped before serialization
  without making `TrySend` fail or leaving the request untracked.

  Create this exact shared fixture:

  ```json
  [
    { "input": "Assets\\Scenes\\\u00c4\\\u0130.unity", "normalized": "Assets/Scenes/\u00c4/\u0130.unity", "canonical": "assets/scenes/\u00c4/\u0130.unity" },
    { "input": "ASSETS/MiXeD.unity", "normalized": "ASSETS/MiXeD.unity", "canonical": "assets/mixed.unity" },
    { "input": "Assets/Scenes/Cafe\u0301.unity", "normalized": "Assets/Scenes/Caf\u00e9.unity", "canonical": "assets/scenes/caf\u00e9.unity" },
    { "input": "Assets/Scenes/Caf\u00e9.unity", "normalized": "Assets/Scenes/Caf\u00e9.unity", "canonical": "assets/scenes/caf\u00e9.unity" },
    { "input": "Assets/Scenes/\u03a3.unity", "normalized": "Assets/Scenes/\u03a3.unity", "canonical": "assets/scenes/\u03a3.unity" },
    { "input": "Assets/Scenes/\u00df.unity", "normalized": "Assets/Scenes/\u00df.unity", "canonical": "assets/scenes/\u00df.unity" }
  ]
  ```

  Read this one file in both `CoordinationPathMatcherTests` and
  `test/protocol.test.ts`; do not copy the vectors into either source file.

- [ ] **Step 2: Run the new tests and verify the expected RED state.**

  ```powershell
  Push-Location Tools/CoordinationServer
  npm test -- test/protocol.test.ts
  Pop-Location
  $exitCode = Invoke-CoordinationEditMode -Filter 'PotionPanic.Tests.EditMode.Coordination.CoordinationServiceTests' -Results 'Logs\coordination-client-hardening-red.xml' -Log 'Logs\coordination-client-hardening-red.log'
  if ($exitCode -eq 0) { throw 'Expected the client hardening RED run to fail.' }
  ```

  Expected: the worker test cannot load the shared fixture and the Unity test
  fails because the callback and exact-once drain behavior do not yet exist.

- [ ] **Step 3: Implement exact-once request draining and bounded metadata.**

  Add a private `DrainPendingRequests(string message)` to
  `CoordinationService`. Under the `pendingRequests` lock, copy all handles and
  clear the dictionary. After the lock, raise one `RequestSendFailed` event per
  copied handle through the main-thread dispatcher. Call it at the beginning of
  the dispatched `OnSocketClosed` handler, before the `shutdown`,
  `credentialUnavailable`, `4003`, or disabled returns. Existing
  `ReportRequestSendFailure` continues to remove before raising, so a send
  failure racing with close leaves exactly one owner of each handle. Do not turn
  a close into `RequestCompleted`.

  Add `CoordinationProtocol.IsValidContext(string value)` as a public
  non-null `value.Length <= MaximumContextLength` check. Add
  `CoordinationProtocol.ClampContext(string value)` to return an empty string
  for null, retain valid input, and truncate longer input to at most 256 UTF-16
  code units without leaving a high surrogate at the end. Use validation in
  `TryParseClientEnvelope`, `CoordinationUserSettings.TryParse`, and
  `CoordinationUserSettings.ToJson`. Make `ToJson` throw `ArgumentException`
  for invalid stored task context; callers must never write an invalid local
  settings file. Clamp the view-model setter before saving. In `TrySend`, clamp
  both Git branch and task metadata before JSON serialization and before adding
  the request to `pendingRequests`; oversized metadata must not turn a connected
  save or acquisition into a silent false return. Render task context with:

  ```csharp
  var taskContext = EditorGUILayout.TextField(
    "Task context", viewModel.TaskContext, CoordinationProtocol.MaximumContextLength);
  ```

  In `GitCoordinationContext.GetBranch`, return
  `CoordinationProtocol.ClampContext(branch)`. Keep the `TrySend` clamp as a
  second defensive boundary so injected test contexts and future providers
  cannot serialize uncontrolled metadata.

- [ ] **Step 4: Implement the credential-save reconnect callback.**

  Change `CoordinationCredentialWindow.ShowForProject` to take an `Action
  credentialsSaved`, store it on the window, and invoke it after
  `TrySubmitToken` succeeds. Do not invoke it for a blank token, a failed
  credential-store write, or Forget. Change the service dependency from
  `Action requestCredentials` to `Action<Action> requestCredentials`; the
  default factory passes:

  ```csharp
  onSaved => CoordinationCredentialWindow.ShowForProject(
    configuration.projectId, new WindowsCredentialStore(), onSaved)
  ```

  `PromptForCredentials` supplies a callback that clears
  `credentialUnavailable`, clears `hasPromptedForCredentials`, calls
  `EnsureConnectionCancellation()`, and starts `StartConnectionAttempt()` when
  the service is neither shut down nor disabled. The callback does not retry a
  successful existing connection and does not retain the developer token.

- [ ] **Step 5: Replace C# full-Unicode folding and consume the shared fixture.**

  Replace `normalizedPath.ToLowerInvariant()` with an ordinal loop that maps
  only characters from `'A'` through `'Z'` by adding 32. Keep NFC normalization
  and slash normalization in `TryNormalize`; do not add culture, locale, or
  `ToLowerInvariant` calls anywhere in the canonical-key path. Make the C# test
  load `Tools/CoordinationServer/test/fixtures/canonical-path-vectors.json`
  from `CoordinationProjectPaths.ProjectDirectory`; use `JsonUtility` with a
  wrapper object, because it cannot deserialize a top-level array. Make the
  Vitest test load the same file using `readFile` and `fileURLToPath` relative
  to `import.meta.url`.

- [ ] **Step 6: Run the focused GREEN tests.**

  ```powershell
  Push-Location Tools/CoordinationServer
  npm run typecheck
  npm test -- test/protocol.test.ts
  Pop-Location
  if ((Invoke-CoordinationEditMode -Filter 'PotionPanic.Tests.EditMode.Coordination.CoordinationServiceTests' -Results 'Logs\coordination-client-hardening-green.xml' -Log 'Logs\coordination-client-hardening-green.log') -ne 0) { throw 'Coordination client hardening tests failed.' }
  if ((Invoke-CoordinationEditMode -Filter 'PotionPanic.Tests.EditMode.Coordination.CoordinationPathMatcherTests' -Results 'Logs\coordination-path-vectors-green.xml' -Log 'Logs\coordination-path-vectors-green.log') -ne 0) { throw 'Canonical path vector tests failed.' }
  ```

  Expected: TypeScript and Unity tests pass; every shared fixture produces the
  exact normalized and canonical strings in both runtimes.

- [ ] **Step 7: Commit the client hardening.**

  ```powershell
  git add Assets/Scripts/Editor/Coordination/CoordinationService.cs Assets/Scripts/Editor/Coordination/CoordinationCredentialWindow.cs Assets/Scripts/Editor/Coordination/CoordinationEditorInfrastructure.cs Assets/Scripts/Editor/Coordination/CoordinationPathMatcher.cs Assets/Scripts/Editor/Coordination/CoordinationUserSettings.cs Assets/Scripts/Editor/Coordination/CoordinationWindow.cs Assets/Scripts/Editor/Coordination/CoordinationWindowViewModel.cs Assets/Tests/EditMode/Coordination/CoordinationServiceTests.cs Assets/Tests/EditMode/Coordination/CoordinationPathMatcherTests.cs Assets/Tests/EditMode/Coordination/CoordinationUserSettingsTests.cs Assets/Tests/EditMode/Coordination/CoordinationWindowViewModelTests.cs Tools/CoordinationServer/test/fixtures/canonical-path-vectors.json Tools/CoordinationServer/test/protocol.test.ts
  git commit -m "fix(coordination): harden Unity transport metadata"
  ```

## Task 3: Declare a Deployable but Undeployed Worker Configuration

**Files:**

- Create: `Tools/CoordinationServer/.dev.vars.example`
- Modify: `Tools/CoordinationServer/wrangler.jsonc`
- Modify: `.gitignore`
- Verify only: `.github/workflows/coordination-server.yml`

**Interfaces:**

- Consumes: named export `CoordinationObject` from
  `Tools/CoordinationServer/src/index.ts` and `Env.ADMIN_TOKEN` /
  `Env.TOKEN_HMAC_KEY`.
- Produces: a declarative configuration requiring both secret names before
  local development or deployment and binding `COORDINATION_OBJECT` to the
  exported SQLite-backed Durable Object class.

- [ ] **Step 1: Write the configuration checks before editing it.**

  Add a small assertion in the existing Worker test suite that reads
  `wrangler.jsonc` as JSONC and asserts `exports.CoordinationObject` equals
  `{ type: "durable-object", storage: "sqlite" }`, the required secrets equal
  `TOKEN_HMAC_KEY` and `ADMIN_TOKEN`, `workers_dev` is `true`, `preview_urls`
  is `false`, and `observability` equals `{ enabled: true, head_sampling_rate: 1 }`.
  The test must also assert that `migrations` is absent because Wrangler treats
  it as mutually exclusive with `exports`.

- [ ] **Step 2: Run the RED configuration checks.**

  ```powershell
  Push-Location Tools/CoordinationServer
  npm test -- test/protocol.test.ts
  Pop-Location
  ```

  Expected: the configuration assertion fails because the current file has the
  legacy `migrations` array and lacks release settings.

- [ ] **Step 3: Replace the legacy lifecycle declaration with this exact JSONC.**

  Keep `name`, `main`, `compatibility_date`, and the
  `durable_objects.bindings` entry. Remove the complete `migrations` property
  and add these top-level properties:

  ```json
  {
    "exports": {
      "CoordinationObject": {
        "type": "durable-object",
        "storage": "sqlite"
      }
    },
    "secrets": {
      "required": ["TOKEN_HMAC_KEY", "ADMIN_TOKEN"]
    },
    "workers_dev": true,
    "preview_urls": false,
    "observability": {
      "enabled": true,
      "head_sampling_rate": 1
    }
  }
  ```

  Create `Tools/CoordinationServer/.dev.vars.example` with exactly:

  ```dotenv
  # Generate independent local values. Never copy production secrets here.
  TOKEN_HMAC_KEY=
  ADMIN_TOKEN=
  ```

  Change the ignore rule from the one-file
  `/Tools/CoordinationServer/.dev.vars` form to
  `/Tools/CoordinationServer/.dev.vars*`, then add a negated exception for
  `!/Tools/CoordinationServer/.dev.vars.example` so the template stays
  tracked. Do not add `.env` files; local operators use `.dev.vars` exclusively.

- [ ] **Step 4: Confirm the GitHub workflow remains verification-only.**

  Inspect `.github/workflows/coordination-server.yml`. Its only Wrangler
  command must remain:

  ```yaml
  - run: npx wrangler deploy --dry-run
  ```

  It must contain neither `CLOUDFLARE_API_TOKEN` nor `wrangler deploy` without
  `--dry-run`. If either appears, remove the deployment credential or command;
  do not replace it with a GitHub secret.

- [ ] **Step 5: Run GREEN configuration validation.**

  ```powershell
  Push-Location Tools/CoordinationServer
  npm run typecheck
  npm test
  npx wrangler deploy --dry-run
  Pop-Location
  git check-ignore -v Tools/CoordinationServer/.dev.vars Tools/CoordinationServer/.dev.vars.example
  ```

  Expected: typecheck, every Worker test, and the dry run pass. `git
  check-ignore` reports an ignore rule for `.dev.vars` and produces no output
  for `.dev.vars.example`.

- [ ] **Step 6: Commit the release configuration.**

  ```powershell
  git add Tools/CoordinationServer/wrangler.jsonc Tools/CoordinationServer/.dev.vars.example .gitignore Tools/CoordinationServer/test/protocol.test.ts
  git commit -m "chore(coordination): declare Worker release configuration"
  ```

## Task 4: Publish Secure Server and Evergreen Coordination Guidance

**Files:**

- Modify: `Tools/CoordinationServer/README.md`
- Modify: `README.md`
- Modify: [Docs/onboarding/getting-started.md](../../onboarding/getting-started.md)
- Modify: [Docs/collaboration/team-workflow.md](../../collaboration/team-workflow.md)
- Modify: [Docs/unity-guides/editor-safety.md](../../unity-guides/editor-safety.md)

**Interfaces:**

- Consumes: the routes in `Tools/CoordinationServer/src/index.ts`, the
  `scripts/issue-token.mjs` command line, generated Worker URL, local
  `coordination.json`, and Unity's Coordination window.
- Produces: one consistent operator workflow covering local secrets, manual
  deploy, token issue/revocation, monitoring, outage fallback, and key rotation
  without putting a credential in GitHub, Git, a URL, a ticket, or a command
  history.

- [ ] **Step 1: Write documentation assertions into the root documentation tests.**

  Add root test assertions that the server README contains the two secret names,
  `.dev.vars.example`, `npx wrangler deploy`, `npx wrangler tail`, and the
  `scripts/issue-token.mjs` invocation; that the root README says the
  coordination Worker deploy is manual and GitHub is verification-only; and
  that the onboarding and workflow docs name the local Disabled switch and
  manual collaboration fallback. The assertions must reject a real-looking
  bearer token and require both assignments in `.dev.vars.example` to have an
  empty value. A documented assignment from a PowerShell variable is permitted;
  a literal secret value is not.

- [ ] **Step 2: Run the RED documentation suite.**

  ```powershell
  npm test
  ```

  Expected: the test fails until the missing operating guidance is added.

- [ ] **Step 3: Add the exact secure PowerShell procedures.**

  In `Tools/CoordinationServer/README.md`, add these commands and prose:

  ```powershell
  Copy-Item .dev.vars.example .dev.vars
  function New-UrlSafeSecret {
    $bytes = [System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
    try {
      return [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
    }
    finally {
      [Array]::Clear($bytes, 0, $bytes.Length)
    }
  }
  $localHmac = New-UrlSafeSecret
  $localAdmin = New-UrlSafeSecret
  @("TOKEN_HMAC_KEY=$localHmac", "ADMIN_TOKEN=$localAdmin") | Set-Content .dev.vars
  Remove-Variable localHmac, localAdmin
  npx wrangler dev --local
  ```

  Document manual production deployment as an authenticated operator action.
  Generate two independent 256-bit URL-safe values in the approved password
  manager first, then enter them through hidden prompts. The temporary file
  must be outside the repository, used for one atomic deploy, and removed in a
  `finally` block:

  ```powershell
  npx wrangler login
  npx wrangler whoami
  $hmacSecure = Read-Host 'TOKEN_HMAC_KEY from password manager' -AsSecureString
  $adminSecure = Read-Host 'ADMIN_TOKEN from password manager' -AsSecureString
  $hmacPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($hmacSecure)
  $adminPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($adminSecure)
  $secretFile = Join-Path $env:TEMP ("potion-panic-secrets-{0}.env" -f [guid]::NewGuid())
  try {
    $hmacValue = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($hmacPointer)
    $adminValue = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($adminPointer)
    @("TOKEN_HMAC_KEY=$hmacValue", "ADMIN_TOKEN=$adminValue") | Set-Content $secretFile
    npx wrangler deploy --strict --secrets-file $secretFile
    if ($LASTEXITCODE -ne 0) { throw 'Wrangler deployment failed.' }
  }
  finally {
    Remove-Item -LiteralPath $secretFile -Force -ErrorAction SilentlyContinue
    Remove-Variable hmacValue, adminValue -ErrorAction SilentlyContinue
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($hmacPointer)
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($adminPointer)
  }
  npx wrangler secret list
  npx wrangler deployments list
  ```

  If `wrangler whoami` lists more than one account, record the chosen account ID
  in `wrangler.jsonc` before deployment. Confirm or create the account's
  `workers.dev` subdomain. After deploy, require the output to list the
  `COORDINATION_OBJECT` binding and SQLite `CoordinationObject` export, then
  verify `GET /health` returns HTTP 200, service
  `potion-panic-coordination`, and a parseable `serverTime`.

  State that the deploy output is the only source for the real `workers.dev`
  URL. Put it in `coordination.json` only after the deploy succeeds. Use this
  session-scoped administrative-token handling to issue a developer token:

  ```powershell
  $secureAdmin = Read-Host 'ADMIN_TOKEN' -AsSecureString
  $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureAdmin)
  try {
    $env:ADMIN_TOKEN = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    $workerBaseUrl = Read-Host 'Paste the exact Worker URL printed by wrangler deploy'
    node scripts/issue-token.mjs $workerBaseUrl 'Developer name'
  }
  finally {
    Remove-Item Env:ADMIN_TOKEN -ErrorAction SilentlyContinue
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
  }
  ```

  Instruct the operator to paste only the URL printed by the successful
  deployment when prompted. Do not use the checked-in example endpoint. Deliver
  the printed developer
  token once through an approved secret channel, then paste it only into the
  Unity credential window.

  Add `npx wrangler tail` as the monitoring command and require filtering out
  secrets from any captured evidence. Add the documented outage fallback:
  select Disabled in the Unity Coordination window, preserve local work,
  announce protected-file edits manually, and reconnect only after health is
  restored. Explain rotation precisely: rotating `ADMIN_TOKEN` affects future
  administrative calls; rotating `TOKEN_HMAC_KEY` invalidates all stored token
  HMACs, so an operator must first record developer IDs, set the new HMAC key,
  revoke old developer records using the still-valid administrative secret,
  issue every developer a new token, and treat existing session creation as
  invalid until reissued.

  In the root and evergreen docs, explain that advisory leases do not replace
  pre-edit announcements, the local Disabled switch does not delete work, and
  a missing/invalid server remains an offline/manual-collaboration condition.
  Do not add a standalone auth page, a secret download, or a GitHub deployment
  workflow.

- [ ] **Step 4: Run GREEN documentation verification.**

  ```powershell
  npm test
  npm run docs:build
  rg -n --glob '!node_modules/**' --glob '!package-lock.json' 'Bearer\s+[A-Za-z0-9_-]{20,}' README.md Docs Tools/CoordinationServer
  ```

  Expected: root tests and VitePress build pass. The final search has no long
  bearer value; the dedicated test confirms that `.dev.vars.example` contains
  empty assignments rather than a secret.

- [ ] **Step 5: Commit the guidance.**

  ```powershell
  git add Tools/CoordinationServer/README.md README.md Docs/onboarding/getting-started.md Docs/collaboration/team-workflow.md Docs/unity-guides/editor-safety.md
  git commit -m "docs(coordination): document secure Worker operations"
  ```

## Task 5: Run the Full Local Release Gate and Record Reviewable Evidence

**Files:**

- Modify: [Docs/tickets/PP-7.md](../../tickets/PP-7.md)
- Verify only: all backend, root, docs, and Unity files named in this plan

**Interfaces:**

- Consumes: merged backend Tasks 1 and 2, the completed Unity hardening,
  Wrangler configuration, root documentation suite, and Unity Test Runner.
- Produces: a PP-7 entry with dates, exact commands, test totals, manual smoke
  result, reviewer result, and the explicit remaining external blockers.

- [ ] **Step 1: Execute the complete backend and root verification gate.**

  ```powershell
  Push-Location Tools/CoordinationServer
  npm ci
  npm run typecheck
  npm test
  npm audit --audit-level=moderate
  npx wrangler deploy --dry-run
  Pop-Location
  npm test
  npm run docs:build
  git status --short
  ```

  Expected: every command exits zero; `npm audit --audit-level=moderate` reports
  zero vulnerabilities; the dry run reports no authentication or missing-secret
  failure; and `git status --short` contains only intentional tracked changes.
  A dry run does not deploy a Worker.

- [ ] **Step 2: Execute the full Unity coordination suite.**

  ```powershell
  if ((Invoke-CoordinationEditMode -Filter 'PotionPanic.Tests.EditMode.Coordination' -Results 'Logs\coordination-release-editmode.xml' -Log 'Logs\coordination-release-editmode.log') -ne 0) { throw 'Full Coordination EditMode suite failed.' }
  ```

  Expected: all Coordination EditMode tests pass with zero failures, skips, and
  inconclusive results. The final log has no C# compilation error or warning.

- [ ] **Step 3: Perform the manual Play Mode smoke.**

  Open the repository in Unity `6000.5.1f1`, wait for compilation, open
  `Assets/Scenes/SampleScene.unity`, open `Window > Potion Panic >
  Coordination`, enter Play Mode, wait until it is running, exit Play Mode, and
  inspect the Console. Record the observed Coordination window state and every
  new error, warning, assertion, or exception. Do not save the scene.

  Expected: Play Mode enters and exits; the Coordination window opens; there
  are no new relevant Console errors, exceptions, or assertions; no `.unity`,
  `.prefab`, `ProjectSettings`, or `Packages` file changes.

- [ ] **Step 4: Obtain an independent code review before release work.**

  Give a reviewer the diff, the full command outputs, the full EditMode XML,
  and the manual smoke log. The reviewer must check these measurable properties:

  1. no snapshot fragment alters client state before a complete assembly;
  2. no request can complete or fail twice across send failure and socket close;
  3. all 256-unit bounds use UTF-16 code units and cannot be bypassed through
     the UI, local settings, Git branch, or serialized envelope;
  4. C# and TypeScript consume the identical Unicode fixture and preserve
     non-ASCII case;
  5. Wrangler contains declarative `exports`, required secrets, `workers_dev`,
     disabled preview URLs, observability, and no legacy `migrations`;
  6. tracked files contain no token and the GitHub workflow performs no deploy.

  Record the reviewer's name, commit reviewed, and either `approved` or each
  blocking finding in PP-7. Resolve every blocking finding and repeat the
  affected command before proceeding.

- [ ] **Step 5: Append factual local evidence to PP-7.**

  Add a dated `Remaining hardening local gate` entry containing the branch or
  commit, backend test total, audit result, dry-run result, root test result,
  docs-build result, Unity EditMode total, Play Mode result, reviewer result,
  and exact log/XML paths. End the entry with these unresolved blockers:

  ```text
  Wrangler authentication has not been performed in this checkout.
  No Worker has been deployed and coordination.json still contains its example endpoint.
  No production secrets or developer tokens have been issued.
  Two Windows machines on different networks have not completed acceptance.
  PP-7 remains open.
  ```

- [ ] **Step 6: Commit the evidence only after every local gate is green.**

  ```powershell
  git add Docs/tickets/PP-7.md
  git commit -m "docs(coordination): record hardening verification"
  ```

## Task 6: Complete the External Cloudflare Release and Two-Machine Acceptance

**Files:**

- Modify after successful deploy only: `coordination.json`
- Modify after each observed acceptance run: [Docs/tickets/PP-7.md](../../tickets/PP-7.md)
- Verify only: deployed Worker dashboard/logs and two Windows Unity machines

**Interfaces:**

- Consumes: an operator's Cloudflare account, the actual deploy output,
  manually issued developer tokens, two Windows machines using Unity
  `6000.5.1f1`, and two different networks.
- Produces: a deployed URL in `coordination.json` and evidence that the release
  acceptance criteria were observed, or a PP-7 blocker stating exactly which
  external action failed.

- [x] **Step 1: Authenticate and deploy manually.**

  From `Tools/CoordinationServer`, an authorized operator follows the README's
  hidden-prompt procedure: `npx wrangler login`, `npx wrangler whoami`, account
  and `workers.dev` subdomain confirmation, then one
  `npx wrangler deploy --strict --secrets-file <temporary-file>` containing both
  required secrets. Remove the temporary file in `finally`. Run
  `npx wrangler secret list` and `npx wrangler deployments list`. Record the
  exact displayed Worker URL, Worker name, deployment ID and timestamp,
  selected account ID, and Wrangler version.
  Do not run this through GitHub Actions and do not paste either secret into a
  Markdown file, shell history, or PP-7.

- [x] **Step 2: Verify the deployed endpoint before changing client config.**

  Store the exact emitted base URL in a current PowerShell variable and verify
  it before client configuration:

  ```powershell
  $workerBaseUrl = Read-Host 'Paste the exact Worker URL printed by wrangler deploy'
  Invoke-RestMethod -Method Get -Uri "$workerBaseUrl/health"
  ```

  Expected: HTTP 200 with `service` equal to `potion-panic-coordination` and a
  `serverTime`. After this succeeds, replace only `serverBaseUrl` in
  `coordination.json` with that exact base URL, run `npm test` and
  `npm run docs:build`, and commit that single configuration update. If deploy
  or health fails, retain the example URL and add the actual error text to
  PP-7; do not claim a deployment.

- [ ] **Step 3: Issue and provision one developer token per machine.**

  Machine A is provisioned and connected. Machine B is deferred, so this step
  remains incomplete.

  Use the secure issue-token procedure from the server README once for each
  developer. Deliver each output to its owner once. On each Windows machine,
  paste the token in `Window > Potion Panic > Coordination`, save it, and
  verify the credential-save callback creates a 24-hour opaque session and
  connects. Verify `UserSettings/PotionPanic/coordination.local.json` contains
  no token and `git status --short` does not show `.dev.vars`, user settings,
  log files, or a generated lease-state file.

- [ ] **Step 4: Run the two-machine, different-network acceptance matrix.**

  Use Machine A and Machine B on different Internet connections. Record the
  start/end timestamp, machine role, Unity version, network description, path,
  request IDs where available, observed owner/branch/task/expiry, and result
  for every row below in PP-7.

  | Scenario | Required observed result |
  | --- | --- |
  | Presence and reservation | A opens `Assets/Scenes/SampleScene.unity`; B sees A. A reserves it; B sees the reservation owner and expiry. |
  | Reservation cancellation | A cancels its reservation from the Coordination window. Both clients receive the correlated `lease.released` state, the reservation disappears, and the path becomes reservable. Repeat from a recreated A session to prove developer ownership does not depend on the original connection. |
  | Simultaneous acquire | A and B issue acquire against the same unclaimed coordinated path at the same time; exactly one receives `lease.granted`, and the other receives `lease.denied` with the same remote owner. |
  | Conflict, cancel, override | With A editing, B tries to save. B can cancel, keep working, or explicitly override; an override transfers the authoritative lease and A receives displacement information. |
  | Clean close | A closes the coordinated stage/editor cleanly; B sees A's presence and unreserved editing lease removed. |
  | Abrupt termination and 120-second expiry | A acquires an editing lease, then force-terminates Unity or disconnects its process. B observes the remaining claim, then observes server expiry no earlier than 120 seconds after last heartbeat and no later than 150 seconds. Record elapsed seconds. |
  | Network loss and outage fallback | Disconnect A's network or make its configured endpoint unreachable. A can keep local work, a guarded save offers the documented uncoordinated path with second confirmation, and the UI/manual announcement fallback remains available. Reconnect and confirm a new session and current authoritative snapshot. |
  | 24-hour session recreation | Keep the developer credential but expire or wait out the opaque 24-hour session. The next connection obtains a new session and reconnects without re-entering the developer token. Record the two session times; never record token values. |
  | Hibernation | Leave both sockets idle long enough for the Durable Object to hibernate, then request a snapshot or make one valid mutation. Both clients remain usable and receive server-derived state; record dashboard/tail evidence if Cloudflare exposes a hibernation event. |
  | Revocation | Revoke A's developer record with the admin route. A's session/socket is rejected or closes with the documented revocation state, B sees A's presence, editing lease, and reservation removed, and A cannot create a new session. |

  The hibernation timing and a real service outage depend on Cloudflare and the
  two external networks. They are manual acceptance observations, not local
  automation targets. If any row cannot be executed, state its unavailable
  dependency and leave PP-7 open.

- [ ] **Step 5: Monitor and record post-deploy signals.**

  Run `npx wrangler tail` during at least one connection, lease, forced-close,
  and revocation scenario. Record only timestamps, event categories, request
  IDs, status codes, and error codes. Exclude authorization headers, developer
  tokens, opaque sessions, secret values, and Credential Manager contents.

- [ ] **Step 6: Close PP-7 only on complete external evidence.**

  Update PP-7's Definition of Done only after all local gates, independent
  review, deployment health, token provisioning, and every acceptance-matrix
  row has dated evidence. Archive the program and slice pages only after PP-7
  is complete. Until then, keep PP-7 and the release-handoff plan active and
  preserve the exact outstanding external blocker.

## Final Coverage Check

| Required outcome | Task |
| --- | --- |
| 16 KiB envelopes, 256 KiB assembly, out-of-order/incomplete/duplicate/inconsistent chunks, atomic apply, one completion | Task 1 |
| Socket-close exact-once request drain, 256 UTF-16-unit bounds, credential-save reconnect, ASCII-only folding and shared Unicode vectors | Task 2 |
| Declarative Wrangler exports, required secrets, `workers_dev`, disabled preview URLs, observability, local secret template, GitHub verification-only | Task 3 |
| Secure setup, deploy, token, local development, monitoring, outage, and rotation guidance | Task 4 |
| Backend/root/docs/Unity verification, Play Mode smoke, independent review, PP-7 local evidence | Task 5 |
| Authenticated deployment and two-machine different-network acceptance including expiry, session recreation, hibernation, outage, and revocation | Task 6 |

## Current External Blockers

1. This checkout has no authenticated Wrangler session.
2. No Cloudflare Worker has been deployed, so no actual `workers.dev` URL is
   available for `coordination.json`.
3. Production `TOKEN_HMAC_KEY` and `ADMIN_TOKEN` have not been provisioned.
4. Developer tokens have not been issued to two real Windows machines on
   different networks.
5. Cloudflare-dependent hibernation and outage observations cannot be completed
   by local unit, integration, or dry-run commands.
