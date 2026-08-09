using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PotionPanic.Editor.Coordination;

namespace PotionPanic.Tests.EditMode.Coordination
{
  public sealed class CoordinationSaveGuardTests
  {
    private static readonly string Laboratory = "Assets/Scenes/Laboratory.unity";
    private static readonly string Arena = "Assets/Scenes/Arena.unity";

    [Test]
    public void ManualSaveRequiresBothConfirmationsBeforeSaving()
    {
      // Catches restoring the Disabled pass-through that bypasses guarded Manual saves.
      using var fixture = new SaveFixture(CoordinationConnectionState.Disabled);
      fixture.Prompt.ChooseResult = true;
      fixture.Prompt.ConfirmResult = true;

      var returned = fixture.AttemptSave(Laboratory);
      fixture.Scheduler.RunImmediate();

      Assert.That(returned, Is.Empty);
      Assert.That(fixture.Prompt.ChooseCount, Is.EqualTo(1));
      Assert.That(fixture.Prompt.ConfirmCount, Is.EqualTo(1));
      Assert.That(fixture.Saves.Paths, Is.EqualTo(new[] { Laboratory }));
      Assert.That(fixture.WarningState.Records.Single().reason, Is.EqualTo("Manual"));
      Assert.That(fixture.Saves.IsDirty(Laboratory), Is.False);
    }

    [Test]
    public void AuthenticationFailureRequiresBothConfirmationsBeforeSaving()
    {
      // Catches dropping AuthenticationFailed saves through the non-connected branch.
      using var fixture = new SaveFixture(
        CoordinationConnectionState.AuthenticationFailed);
      fixture.Service.RaiseError(
        "authentication_failed",
        "The saved developer credential was rejected.");
      fixture.Prompt.ChooseResult = true;
      fixture.Prompt.ConfirmResult = true;

      var returned = fixture.AttemptSave(Laboratory);
      fixture.Scheduler.RunImmediate();

      Assert.That(returned, Is.Empty);
      Assert.That(fixture.Prompt.ChooseCount, Is.EqualTo(1));
      Assert.That(fixture.Prompt.ConfirmCount, Is.EqualTo(1));
      Assert.That(fixture.Prompt.LastRequest.Reason,
        Is.EqualTo(CoordinationUncoordinatedSaveReason.AuthenticationFailed));
      Assert.That(fixture.Prompt.LastRequest.Detail,
        Is.EqualTo("The saved developer credential was rejected."));
      Assert.That(fixture.Saves.Paths, Is.EqualTo(new[] { Laboratory }));
      Assert.That(fixture.Saves.IsDirty(Laboratory), Is.False);
    }

    [Test]
    public void AuthenticationFailureDoesNotExposeAnArbitraryTransportMessage()
    {
      // Catches displaying credential material supplied by a transport error.
      using var fixture = new SaveFixture(
        CoordinationConnectionState.AuthenticationFailed);
      fixture.Service.RaiseError(
        "authentication_failed",
        "Bearer secret-developer-token");
      fixture.Prompt.ChooseResult = false;

      var returned = fixture.AttemptSave(Laboratory);
      fixture.Scheduler.RunImmediate();

      Assert.That(returned, Is.Empty);
      Assert.That(fixture.Prompt.LastRequest.Detail,
        Is.EqualTo("The saved developer credential was rejected."));
      Assert.That(fixture.Prompt.LastRequest.Detail,
        Does.Not.Contain("secret-developer-token"));
      Assert.That(fixture.Saves.IsDirty(Laboratory), Is.True);
    }

    [TestCase(
      CoordinationConnectionState.Offline,
      "Offline")]
    [TestCase(
      CoordinationConnectionState.Reconnecting,
      "Reconnecting")]
    [TestCase(
      CoordinationConnectionState.AuthenticationFailed,
      "AuthenticationFailed")]
    [TestCase(
      CoordinationConnectionState.Disabled,
      "Manual")]
    public void ConnectionFallbackRecordsItsExactStableReason(
      CoordinationConnectionState state,
      string expectedReason)
    {
      // Catches collapsing distinct connection states into a generic outage reason.
      using var fixture = new SaveFixture(state);
      fixture.Prompt.ChooseResult = true;
      fixture.Prompt.ConfirmResult = true;

      var returned = fixture.AttemptSave(Laboratory);
      fixture.Scheduler.RunImmediate();

      Assert.That(returned, Is.Empty);
      Assert.That(fixture.Prompt.ChooseCount, Is.EqualTo(1));
      Assert.That(fixture.Prompt.ConfirmCount, Is.EqualTo(1));
      Assert.That(fixture.WarningState.Records.Single().reason,
        Is.EqualTo(expectedReason));
      Assert.That(fixture.Saves.IsDirty(Laboratory), Is.False);
    }

    [TestCase(false, false, 0)]
    [TestCase(true, false, 1)]
    public void DecliningEitherFallbackConfirmationLeavesTheWholeBatchDirty(
      bool choose,
      bool confirm,
      int expectedConfirmCount)
    {
      // Catches clearing dirty work when either fallback confirmation is declined.
      using var fixture = new SaveFixture(CoordinationConnectionState.Offline);
      fixture.Prompt.ChooseResult = choose;
      fixture.Prompt.ConfirmResult = confirm;

      var returned = fixture.AttemptSave(Laboratory, Arena);
      fixture.Scheduler.RunImmediate();

      Assert.That(returned, Is.Empty);
      Assert.That(fixture.Prompt.ChooseCount, Is.EqualTo(1));
      Assert.That(fixture.Prompt.ConfirmCount, Is.EqualTo(expectedConfirmCount));
      Assert.That(fixture.Saves.IsDirty(Laboratory), Is.True);
      Assert.That(fixture.Saves.IsDirty(Arena), Is.True);
      Assert.That(fixture.WarningState.Records, Is.Empty);
    }

    [Test]
    public void FallbackAuthorizationIsExactAndCannotAuthorizeALaterSave()
    {
      // Catches retaining a fallback authorization after its one resumed save attempt.
      using var fixture = new SaveFixture(CoordinationConnectionState.Offline);
      fixture.Prompt.ChooseResult = true;
      fixture.Prompt.ConfirmResult = true;
      string[] firstCallback = null;
      string[] laterCallback = null;
      fixture.Saves.OnSave = paths =>
      {
        firstCallback = fixture.Filter.FilterPaths(new[] { paths[0] });
        fixture.Saves.AcceptImmediate(firstCallback);
      };

      var returned = fixture.AttemptSave(Laboratory);
      fixture.Scheduler.RunNextImmediate();
      fixture.Scheduler.RunNextImmediate();
      fixture.Saves.MarkDirty(Laboratory);
      laterCallback = fixture.Filter.FilterPaths(new[] { Laboratory });

      Assert.That(returned, Is.Empty);
      Assert.That(firstCallback, Is.EqualTo(new[] { Laboratory }));
      Assert.That(laterCallback, Is.Empty);
      Assert.That(fixture.Saves.Calls, Is.EqualTo(new[] { Laboratory }));
      Assert.That(fixture.Saves.IsDirty(Laboratory), Is.True);
    }

    [Test]
    public void MixedBatchCannotUseOnePathsAuthorizationForAnotherPath()
    {
      // Catches broadening one path's authorization to the rest of its save batch.
      using var fixture = new SaveFixture(CoordinationConnectionState.Offline);
      fixture.Prompt.ChooseResult = true;
      fixture.Prompt.ConfirmResult = true;
      string[] firstCallback = null;
      fixture.Saves.OnSave = paths =>
      {
        if (paths[0] == Laboratory)
        {
          firstCallback = fixture.Filter.FilterPaths(new[] { Laboratory, Arena });
          fixture.Saves.AcceptImmediate(firstCallback);
        }
      };

      var returned = fixture.AttemptSave(Laboratory, Arena);
      fixture.Scheduler.RunImmediate();

      Assert.That(returned, Is.Empty);
      Assert.That(firstCallback, Is.EqualTo(new[] { Laboratory }));
      Assert.That(fixture.Saves.Calls, Is.EqualTo(new[] { Laboratory, Arena }));
      Assert.That(fixture.Saves.IsDirty(Laboratory), Is.False);
      Assert.That(fixture.Saves.IsDirty(Arena), Is.False);
    }

    [Test]
    public void SuccessfulFallbackRecordsOwnerBranchAndTaskMetadata()
    {
      // Catches omitting available snapshot, Git, or local task metadata.
      using var fixture = new SaveFixture(CoordinationConnectionState.Offline);
      fixture.SetRemoteLease(Laboratory, "Morgan");
      fixture.GitContext.Branch = "feature/save-policy";
      fixture.TaskContext = "PP-9 Task 2";
      fixture.Prompt.ChooseResult = true;
      fixture.Prompt.ConfirmResult = true;

      var returned = fixture.AttemptSave(Laboratory);
      fixture.Scheduler.RunImmediate();

      var record = fixture.WarningState.Records.Single();
      Assert.That(returned, Is.Empty);
      Assert.That(record.lastKnownOwner, Is.EqualTo("Morgan"));
      Assert.That(record.branch, Is.EqualTo("feature/save-policy"));
      Assert.That(record.task, Is.EqualTo("PP-9 Task 2"));
      Assert.That(fixture.Saves.IsDirty(Laboratory), Is.False);
    }

    [Test]
    public void MissingFallbackMetadataIsStoredAsEmpty()
    {
      // Catches inventing placeholder values in the durable warning ledger.
      using var fixture = new SaveFixture(CoordinationConnectionState.Offline);
      fixture.GitContext.Branch = null;
      fixture.TaskContext = null;
      fixture.Prompt.ChooseResult = true;
      fixture.Prompt.ConfirmResult = true;

      var returned = fixture.AttemptSave(Laboratory);
      fixture.Scheduler.RunImmediate();

      var record = fixture.WarningState.Records.Single();
      Assert.That(returned, Is.Empty);
      Assert.That(record.lastKnownOwner, Is.Empty);
      Assert.That(record.branch, Is.Empty);
      Assert.That(record.task, Is.Empty);
      Assert.That(fixture.Saves.IsDirty(Laboratory), Is.False);
    }

    [Test]
    public void SuccessfulFallbackPersistsLastKnownOwnerMetadata()
    {
      // Catches replacing the durable warning ledger with memory-only warning state.
      var path = "Assets/Scenes/Task2Metadata-" + Guid.NewGuid().ToString("N")
        + ".unity";
      var store = new CoordinationUncoordinatedSaveStore();
      var clock = new SystemCoordinationClock();
      var cleanupLedger = new CoordinationUncoordinatedSaveLedger(store, clock);
      cleanupLedger.ReconcilePath(path);
      try
      {
        using var fixture = new SaveFixture(
          CoordinationConnectionState.Offline,
          new CoordinationUncoordinatedSaveState());
        fixture.SetRemoteLease(path, "Morgan");
        fixture.Prompt.ChooseResult = true;
        fixture.Prompt.ConfirmResult = true;

        var returned = fixture.Filter.FilterPaths(new[] { path });
        fixture.Scheduler.RunImmediate();

        var record = store.Load().Records.Single(value => value.path == path);
        Assert.That(returned, Is.Empty);
        Assert.That(fixture.Saves.Paths, Is.EqualTo(new[] { path }));
        Assert.That(record.reason, Is.EqualTo("Offline"));
        Assert.That(record.lastKnownOwner, Is.EqualTo("Morgan"));
      }
      finally
      {
        new CoordinationUncoordinatedSaveLedger(store, clock).ReconcilePath(path);
      }
    }

    [Test]
    public void RemoteConflictReturnsSafePathsBeforeSchedulingAnAcquire()
    {
      using var fixture = new SaveFixture();
      fixture.SetRemoteLease(Laboratory, "Morgan");

      var returned = fixture.Filter.FilterPaths(new[]
      {
        Laboratory,
        "Assets/Data.asset"
      });

      Assert.That(returned, Is.EqualTo(new[] { "Assets/Data.asset" }));
      Assert.That(fixture.Service.Requests, Is.Empty);
      Assert.That(fixture.Dialog.ShowCount, Is.Zero);
      Assert.That(fixture.Saves.Paths, Is.Empty);

      fixture.Scheduler.RunImmediate();

      Assert.That(fixture.Service.Requests, Is.EqualTo(new[]
      {
        "lease.acquire:" + Laboratory
      }));
    }

    [Test]
    public void InstalledUnityCallbackUsesTheSameDeferredFilteringContract()
    {
      using var fixture = new SaveFixture();
      CoordinationSaveGuard.Install(fixture.Filter);
      try
      {
        var returned = CoordinationSaveGuard.OnWillSaveAssets(new[]
        {
          Laboratory,
          "Assets/Data.asset"
        });

        Assert.That(returned, Is.EqualTo(new[] { "Assets/Data.asset" }));
        Assert.That(fixture.Service.Requests, Is.Empty);
      }
      finally
      {
        CoordinationSaveGuard.Uninstall(fixture.Filter);
      }
    }

    [Test]
    public void AuthoritativelyOwnedPathReturnsImmediatelyWithoutScheduling()
    {
      // Catches prompting or deferring a save already authorized by the local lease.
      using var fixture = new SaveFixture();
      fixture.StateStore.ApplyLeaseUpdate(Granted(
        2,
        Laboratory,
        "dev-local",
        "connection-local"));

      var returned = fixture.AttemptSave(Laboratory);

      Assert.That(returned, Is.EqualTo(new[] { Laboratory }));
      fixture.Scheduler.RunImmediate();
      Assert.That(fixture.Service.Requests, Is.Empty);
      Assert.That(fixture.Prompt.ChooseCount, Is.Zero);
      Assert.That(fixture.Saves.IsDirty(Laboratory), Is.False);
    }

    [Test]
    public void PendingClaimSuppressesASecondRequestForTheSameNormalizedPath()
    {
      using var fixture = new SaveFixture();

      var returned = fixture.Filter.FilterPaths(new[]
      {
        "Assets\\Scenes\\Laboratory.unity"
      });
      Assert.That(returned,
        Is.Empty);
      fixture.Scheduler.RunImmediate();
      Assert.That(fixture.Filter.FilterPaths(new[] { Laboratory }), Is.Empty);
      fixture.Scheduler.RunImmediate();

      Assert.That(fixture.Service.Requests, Is.EqualTo(new[]
      {
        "lease.acquire:" + Laboratory
      }));
    }

    [Test]
    public void MultiPathGrantResumesOnlyTheCorrelatedAuthorizedPath()
    {
      using var fixture = new SaveFixture();

      Assert.That(fixture.Filter.FilterPaths(new[] { Laboratory, Arena }), Is.Empty);
      fixture.Scheduler.RunImmediate();
      var laboratoryRequest = fixture.Service.RequestFor("lease.acquire", Laboratory);
      var arenaRequest = fixture.Service.RequestFor("lease.acquire", Arena);

      fixture.Service.RaiseCompletion(Completion(
        laboratoryRequest,
        Granted(2, Laboratory, "dev-local", "connection-local"),
        false));

      Assert.That(fixture.Saves.Paths, Is.EqualTo(new[] { Laboratory }));

      fixture.Service.RaiseCompletion(Completion(
        arenaRequest, Denied(3, Arena, RemoteLease(Arena, "Morgan")), false));
      fixture.Scheduler.RunImmediate();

      Assert.That(fixture.Saves.Paths, Is.EqualTo(new[] { Laboratory }));
    }

    [Test]
    public void StaleReplayGrantCannotResumeWithoutCurrentLocalOwnership()
    {
      using var fixture = new SaveFixture();
      fixture.SetRemoteLease(Laboratory, "Morgan", 10);
      fixture.Filter.FilterPaths(new[] { Laboratory });
      fixture.Scheduler.RunImmediate();
      var request = fixture.Service.RequestFor("lease.acquire", Laboratory);

      fixture.Service.RaiseCompletion(Completion(
        request, Granted(9, Laboratory, "dev-local", "connection-local"), true));

      Assert.That(fixture.Saves.Paths, Is.Empty);
    }

    [Test]
    public void AuthoritativeGrantResumesTheOmittedPath()
    {
      // Catches resuming a different or unnormalized path after lease acquisition.
      using var fixture = new SaveFixture();
      var returned = fixture.AttemptSave("Assets\\Scenes\\Laboratory.unity");
      Assert.That(returned, Is.Empty);
      Assert.That(fixture.Saves.IsDirty(Laboratory), Is.True);
      fixture.Scheduler.RunImmediate();
      var request = fixture.Service.RequestFor("lease.acquire", Laboratory);

      fixture.Service.RaiseCompletion(Completion(
        request, Granted(2, Laboratory, "dev-local", "connection-local"), false));

      Assert.That(fixture.Saves.Paths, Is.EqualTo(new[] { Laboratory }));
      Assert.That(fixture.Saves.IsDirty(Laboratory), Is.False);
    }

    [Test]
    public void ResumeAuthorizationAllowsOnlyTheExactPathAndPreventsRecursiveAcquisition()
    {
      using var fixture = new SaveFixture();
      fixture.Saves.OnSave = paths =>
      {
        var callbackPaths = fixture.Filter.FilterPaths(new[] { paths[0], Arena });
        Assert.That(callbackPaths, Is.EqualTo(new[] { Laboratory }));
      };
      fixture.Filter.FilterPaths(new[] { Laboratory });
      fixture.Scheduler.RunImmediate();
      var request = fixture.Service.RequestFor("lease.acquire", Laboratory);

      fixture.Service.RaiseCompletion(Completion(
        request, Granted(2, Laboratory, "dev-local", "connection-local"), false));

      var acquireCount = fixture.Service.Requests.Count(
        value => value == "lease.acquire:" + Laboratory);
      Assert.That(acquireCount,
        Is.EqualTo(1));
      Assert.That(fixture.Saves.Paths, Is.EqualTo(new[] { Laboratory }));
    }

    [Test]
    public void OfflineLocalSaveRequiresBothConfirmationsAndRecordsOnlyMemoryState()
    {
      using var fixture = new SaveFixture(CoordinationConnectionState.Offline);
      fixture.SetRemoteLease(Laboratory, "Morgan");
      fixture.Prompt.ChooseResult = true;
      fixture.Prompt.ConfirmResult = true;

      Assert.That(fixture.Filter.FilterPaths(new[] { Laboratory }), Is.Empty);
      fixture.Scheduler.RunImmediate();

      Assert.That(fixture.Prompt.ChooseCount, Is.EqualTo(1));
      Assert.That(fixture.Prompt.ConfirmCount, Is.EqualTo(1));
      Assert.That(fixture.Prompt.LastRequest.AssetPaths,
        Is.EqualTo(new[] { Laboratory }));
      Assert.That(fixture.Saves.Paths, Is.EqualTo(new[] { Laboratory }));
      Assert.That(fixture.WarningState.Warnings.Single().AffectedPaths,
        Is.EqualTo(new[] { Laboratory }));
      Assert.That(fixture.Logger.Messages.Single(), Does.Contain(Laboratory));
    }

    [Test]
    public void CachedLocalLeaseDoesNotCountAsOwnershipWhileOffline()
    {
      using var fixture = new SaveFixture(CoordinationConnectionState.Offline);
      fixture.StateStore.ApplyLeaseUpdate(Granted(
        2,
        Laboratory,
        "dev-local",
        "connection-local"));
      fixture.Prompt.ChooseResult = true;
      fixture.Prompt.ConfirmResult = false;

      var returned = fixture.Filter.FilterPaths(new[] { Laboratory });

      Assert.That(returned, Is.Empty);
      fixture.Scheduler.RunImmediate();
      Assert.That(fixture.Prompt.ChooseCount, Is.EqualTo(1));
      Assert.That(fixture.Saves.Paths, Is.Empty);
    }

    [Test]
    public void ReconnectCannotReuseThePriorConnectionIdentityBeforeSessionReady()
    {
      using var fixture = new SaveFixture();
      fixture.StateStore.ApplyLeaseUpdate(Granted(
        2,
        Laboratory,
        "dev-local",
        "connection-local"));
      fixture.Service.SetState(CoordinationConnectionState.Reconnecting);
      fixture.Service.SetState(CoordinationConnectionState.Connected);

      var returned = fixture.Filter.FilterPaths(new[] { Laboratory });

      Assert.That(returned, Is.Empty);
    }

    [Test]
    public void DecliningSecondConfirmationPreservesThePendingDirtyPath()
    {
      using var fixture = new SaveFixture(CoordinationConnectionState.Reconnecting);
      fixture.Prompt.ChooseResult = true;
      fixture.Prompt.ConfirmResult = false;

      fixture.Filter.FilterPaths(new[] { Laboratory });
      fixture.Scheduler.RunImmediate();

      Assert.That(fixture.Prompt.ChooseCount, Is.EqualTo(1));
      Assert.That(fixture.Prompt.ConfirmCount, Is.EqualTo(1));
      Assert.That(fixture.Saves.Paths, Is.Empty);
      Assert.That(fixture.WarningState.Warnings, Is.Empty);
      Assert.That(fixture.Filter.FilterPaths(new[] { Laboratory }), Is.Empty,
        "The path remains guarded and dirty work is not converted into ownership.");
    }

    [Test]
    public void ReconnectBeforeTheOutagePromptDisablesTheLocalSaveChoice()
    {
      // Catches showing stale fallback UI after a coordinated retry becomes possible.
      using var fixture = new SaveFixture(CoordinationConnectionState.Offline);
      fixture.Prompt.ChooseResult = true;
      fixture.Prompt.ConfirmResult = true;
      var returned = fixture.AttemptSave(Laboratory);
      fixture.Scheduler.RunNextImmediate();

      fixture.Service.SetState(CoordinationConnectionState.Connected);
      fixture.Scheduler.RunImmediate();

      Assert.That(fixture.Prompt.ChooseCount, Is.Zero);
      Assert.That(returned, Is.Empty);
      Assert.That(fixture.Service.Requests, Is.EqualTo(new[]
      {
        "lease.acquire:" + Laboratory
      }));
      Assert.That(fixture.Saves.Paths, Is.Empty);
      Assert.That(fixture.Saves.IsDirty(Laboratory), Is.True);
    }

    [Test]
    public void AcquireTimeoutOffersLocalSaveWithoutTreatingTimeoutAsOwnership()
    {
      // Catches recording a request timeout under the wrong durable reason.
      using var fixture = new SaveFixture();
      fixture.Prompt.ChooseResult = true;
      fixture.Prompt.ConfirmResult = true;
      var returned = fixture.AttemptSave(Laboratory);
      fixture.Scheduler.RunImmediate();

      fixture.Scheduler.RunDelayed();
      fixture.Scheduler.RunImmediate();

      Assert.That(fixture.Prompt.ChooseCount, Is.EqualTo(1));
      Assert.That(fixture.Prompt.ConfirmCount, Is.EqualTo(1));
      Assert.That(fixture.Saves.Paths, Is.EqualTo(new[] { Laboratory }));
      Assert.That(fixture.StateStore.TryGetLease(Laboratory, out _), Is.False);
      Assert.That(returned, Is.Empty);
      Assert.That(fixture.WarningState.Records.Single().reason,
        Is.EqualTo("RequestTimeout"));
      Assert.That(fixture.Saves.IsDirty(Laboratory), Is.False);
    }

    [Test]
    public void OverrideTransportFailureOffersTheTwoStepLocalSave()
    {
      // Catches recording a failed override transport under a generic outage reason.
      using var fixture = new SaveFixture();
      fixture.SetRemoteLease(Laboratory, "Morgan");
      fixture.Dialog.Result = SaveConflictAction.OverrideAndSave;
      fixture.Prompt.ChooseResult = true;
      fixture.Prompt.ConfirmResult = true;
      var returned = fixture.AttemptSave(Laboratory);
      fixture.Scheduler.RunImmediate();
      var acquire = fixture.Service.RequestFor("lease.acquire", Laboratory);
      fixture.Service.RaiseCompletion(Completion(
        acquire, Denied(2, Laboratory, RemoteLease(Laboratory, "Morgan")), false));
      fixture.Scheduler.RunImmediate();
      var overrideRequest = fixture.Service.RequestFor("lease.override", Laboratory);

      fixture.Service.RaiseSendFailure(SendFailure(overrideRequest, "socket closed"));
      fixture.Scheduler.RunImmediate();

      Assert.That(fixture.Prompt.ChooseCount, Is.EqualTo(1));
      Assert.That(fixture.Prompt.ConfirmCount, Is.EqualTo(1));
      Assert.That(fixture.Saves.Paths, Is.EqualTo(new[] { Laboratory }));
      Assert.That(returned, Is.Empty);
      Assert.That(fixture.WarningState.Records.Single().reason,
        Is.EqualTo("OverrideTransportFailure"));
      Assert.That(fixture.Saves.IsDirty(Laboratory), Is.False);
    }

    [Test]
    public void AcquireSendFailureDoesNotEnableLocalSaveWhileStillConnected()
    {
      // Catches treating a connected acquire send failure as fallback authorization.
      using var fixture = new SaveFixture();
      fixture.Prompt.ChooseResult = true;
      fixture.Prompt.ConfirmResult = true;
      var returned = fixture.AttemptSave(Laboratory);
      fixture.Scheduler.RunImmediate();
      var acquire = fixture.Service.RequestFor("lease.acquire", Laboratory);

      fixture.Service.RaiseSendFailure(SendFailure(acquire, "rejected before send"));
      fixture.Scheduler.RunImmediate();

      Assert.That(fixture.Prompt.ChooseCount, Is.Zero);
      Assert.That(fixture.Saves.Paths, Is.Empty);
      Assert.That(returned, Is.Empty);
      Assert.That(fixture.Saves.IsDirty(Laboratory), Is.True);
    }

    [Test]
    public void AuthenticationFailureDoesNotEnableTheUncoordinatedOverrideFallback()
    {
      using var fixture = new SaveFixture();
      fixture.SetRemoteLease(Laboratory, "Morgan");
      fixture.Dialog.Result = SaveConflictAction.OverrideAndSave;
      fixture.Prompt.ChooseResult = true;
      fixture.Prompt.ConfirmResult = true;
      fixture.Filter.FilterPaths(new[] { Laboratory });
      fixture.Scheduler.RunImmediate();
      var acquire = fixture.Service.RequestFor("lease.acquire", Laboratory);
      fixture.Service.RaiseCompletion(Completion(
        acquire,
        Denied(2, Laboratory, RemoteLease(Laboratory, "Morgan")),
        false));
      fixture.Service.SetState(CoordinationConnectionState.AuthenticationFailed);

      fixture.Scheduler.RunImmediate();

      Assert.That(fixture.Dialog.ShowCount, Is.EqualTo(1));
      Assert.That(fixture.Prompt.ChooseCount, Is.Zero);
      Assert.That(fixture.Saves.Paths, Is.Empty);
    }

    [TestCase(SaveConflictAction.OverrideAndSave, 1)]
    [TestCase(SaveConflictAction.CancelSave, 0)]
    [TestCase(SaveConflictAction.KeepWorking, 0)]
    public void ConflictDialogActionsSendOverrideOnlyForOverrideAndSave(
      SaveConflictAction action,
      int expectedOverrideCount)
    {
      // Catches routing a connected remote-owner decision through fallback prompts.
      using var fixture = new SaveFixture();
      fixture.SetRemoteLease(Laboratory, "Morgan");
      fixture.Dialog.Result = action;
      var returned = fixture.AttemptSave(Laboratory);
      fixture.Scheduler.RunImmediate();
      var acquire = fixture.Service.RequestFor("lease.acquire", Laboratory);

      fixture.Service.RaiseCompletion(Completion(
        acquire, Denied(2, Laboratory, RemoteLease(Laboratory, "Morgan")), false));

      Assert.That(fixture.Dialog.ShowCount, Is.Zero,
        "The conflict dialog must be queued after the request callback returns.");
      fixture.Scheduler.RunImmediate();

      Assert.That(fixture.Dialog.ShowCount, Is.EqualTo(1));
      var overrideCount = fixture.Service.Requests.Count(
        value => value.StartsWith("lease.override:"));
      Assert.That(overrideCount,
        Is.EqualTo(expectedOverrideCount));
      Assert.That(fixture.Prompt.ChooseCount, Is.Zero);
      Assert.That(returned, Is.Empty);
      Assert.That(fixture.Saves.IsDirty(Laboratory), Is.True);
      if (action != SaveConflictAction.OverrideAndSave)
      {
        Assert.That(fixture.Saves.Paths, Is.Empty);
      }
    }

    [Test]
    public void FailedResumeDoesNotRecordAnUncoordinatedSaveOrRetryRecursively()
    {
      // Catches recording a warning before Unity accepts the resumed save.
      using var fixture = new SaveFixture(CoordinationConnectionState.Offline);
      fixture.Prompt.ChooseResult = true;
      fixture.Prompt.ConfirmResult = true;
      fixture.Saves.Result = false;

      var returned = fixture.AttemptSave(Laboratory);
      fixture.Scheduler.RunImmediate();

      Assert.That(returned, Is.Empty);
      Assert.That(fixture.Saves.Paths, Is.EqualTo(new[] { Laboratory }));
      Assert.That(fixture.WarningState.Warnings, Is.Empty);
      Assert.That(fixture.WarningState.Records, Is.Empty);
      Assert.That(fixture.Logger.Messages, Is.Empty);
      Assert.That(fixture.Saves.IsDirty(Laboratory), Is.True);
    }

    [Test]
    public void PartialOfflineBatchWarnsOnlyForThePathThatActuallySaved()
    {
      // Catches recording failed paths from a partially accepted fallback batch.
      using var fixture = new SaveFixture(CoordinationConnectionState.Offline);
      fixture.Prompt.ChooseResult = true;
      fixture.Prompt.ConfirmResult = true;
      fixture.Saves.Results.Enqueue(true);
      fixture.Saves.Results.Enqueue(false);

      var returned = fixture.AttemptSave(Laboratory, Arena);
      fixture.Scheduler.RunImmediate();

      Assert.That(returned, Is.Empty);
      Assert.That(fixture.Saves.Calls, Is.EqualTo(new[]
      {
        Laboratory,
        Arena
      }));
      Assert.That(fixture.WarningState.Warnings.Single().AffectedPaths,
        Is.EqualTo(new[] { Laboratory }));
      Assert.That(fixture.Logger.Messages.Single(), Does.Contain(Laboratory));
      Assert.That(fixture.Logger.Messages.Single(), Does.Not.Contain(Arena));
      Assert.That(fixture.Saves.IsDirty(Laboratory), Is.False);
      Assert.That(fixture.Saves.IsDirty(Arena), Is.True);
    }

    [Test]
    public void AuthoritativeOwnershipDoesNotClearUncoordinatedSaveWarnings()
    {
      using var fixture = new SaveFixture(CoordinationConnectionState.Offline);
      fixture.Prompt.ChooseResult = true;
      fixture.Prompt.ConfirmResult = true;
      fixture.Filter.FilterPaths(new[] { Laboratory });
      fixture.Scheduler.RunImmediate();
      Assert.That(fixture.WarningState.Warnings.Count, Is.EqualTo(1));
      fixture.Service.SetState(CoordinationConnectionState.Connected);
      fixture.Filter.FilterPaths(new[] { Laboratory });
      fixture.Scheduler.RunImmediate();
      var acquire = fixture.Service.RequestFor("lease.acquire", Laboratory);

      fixture.Service.RaiseCompletion(Completion(
        acquire,
        Granted(2, Laboratory, "dev-local", "connection-local"),
        false));

      Assert.That(fixture.WarningState.Warnings.Single().AffectedPaths,
        Is.EqualTo(new[] { Laboratory }));
    }

    [Test]
    public void DisposalDropsPendingWorkWithoutSavingOrOpeningUi()
    {
      var fixture = new SaveFixture();
      fixture.Filter.FilterPaths(new[] { Laboratory });
      fixture.Scheduler.RunImmediate();

      fixture.Dispose();
      fixture.Scheduler.RunDelayed();

      Assert.That(fixture.Saves.Paths, Is.Empty);
      Assert.That(fixture.Dialog.ShowCount, Is.Zero);
      Assert.That(fixture.Prompt.ChooseCount, Is.Zero);
    }

    private sealed class SaveFixture : IDisposable
    {
      public FakeSaveService Service { get; }
      public CoordinationStateStore StateStore { get; } = new CoordinationStateStore();
      public CoordinationUncoordinatedSaveState WarningState { get; }
      public ManualScheduler Scheduler { get; } = new ManualScheduler();
      public FakeConflictDialog Dialog { get; } = new FakeConflictDialog();
      public FakeLocalSavePrompt Prompt { get; } = new FakeLocalSavePrompt();
      public FakeSaveInvoker Saves { get; } = new FakeSaveInvoker();
      public FakeWarningLogger Logger { get; } = new FakeWarningLogger();
      public FakeGitContext GitContext { get; } = new FakeGitContext();
      public string TaskContext { get; set; } = "PP-9";
      public CoordinationSaveResumeCoordinator Coordinator { get; }
      public CoordinationSavePathFilter Filter { get; }

      public SaveFixture(
        CoordinationConnectionState state = CoordinationConnectionState.Connected,
        CoordinationUncoordinatedSaveState warningState = null)
      {
        Service = new FakeSaveService(state);
        WarningState = warningState ?? new CoordinationUncoordinatedSaveState(
          new CoordinationUncoordinatedSaveLedger(
            new FakeUncoordinatedSaveStore(),
            new SystemCoordinationClock()));
        Coordinator = new CoordinationSaveResumeCoordinator(
          Service,
          StateStore,
          WarningState,
          Scheduler,
          Dialog,
          Prompt,
          Saves,
          Logger,
          GitContext,
          () => TaskContext,
          TimeSpan.FromSeconds(5));
        Coordinator.Enable();
        Service.RaiseSessionReady(1, "dev-local", "connection-local");
        Filter = new CoordinationSavePathFilter(Coordinator, new[]
        {
          new CoordinatedPathRule
          {
            pattern = "Assets/Scenes/**/*.unity",
            enabled = true,
            exclusive = true
          }
        }, Scheduler);
      }

      public void SetRemoteLease(string path, string owner, long stateVersion = 1)
      {
        StateStore.ApplyLeaseUpdate(new CoordinationServerEnvelope
        {
          type = "lease.updated",
          stateVersion = stateVersion,
          lease = RemoteLease(path, owner)
        });
      }

      public string[] AttemptSave(params string[] paths)
      {
        Saves.MarkDirty(paths);
        var immediate = Filter.FilterPaths(paths);
        Saves.AcceptImmediate(immediate);
        return immediate;
      }

      public void Dispose() => Coordinator.Dispose();
    }

    private sealed class FakeSaveService
      : ICoordinationSaveService,
        ICoordinationNotificationSource
    {
      private readonly List<CoordinationRequestHandle> requests
        = new List<CoordinationRequestHandle>();
      private int nextRequestId;

      public event Action<CoordinationConnectionState> StateChanged;
      public event Action<CoordinationServerEnvelope> SessionReady;
      public event Action<CoordinationRequestCompletion> RequestCompleted;
      public event Action<CoordinationRequestSendFailure> RequestSendFailed;
      public event Action<CoordinationServerEnvelope> ErrorReceived;
      event Action<CoordinationServerEnvelope>
        ICoordinationNotificationSource.LeaseResultReceived
      {
        add { }
        remove { }
      }

      public CoordinationConnectionState State { get; private set; }
      public List<string> Requests { get; } = new List<string>();

      public FakeSaveService(CoordinationConnectionState state)
      {
        State = state;
      }

      public bool TryAcquireLease(string path, out CoordinationRequestHandle request)
        => Record("lease.acquire", path, out request);

      public bool TryOverrideLease(string path, out CoordinationRequestHandle request)
        => Record("lease.override", path, out request);

      public CoordinationRequestHandle RequestFor(string type, string path)
      {
        return requests.Single(value => value.Type == type
          && value.NormalizedPath == path);
      }

      public void SetState(CoordinationConnectionState state)
      {
        State = state;
        StateChanged?.Invoke(state);
      }

      public void RaiseSessionReady(long version, string developerId, string connectionId)
      {
        SessionReady?.Invoke(new CoordinationServerEnvelope
        {
          type = "session.ready",
          stateVersion = version,
          developerId = developerId,
          connectionId = connectionId
        });
      }

      public void RaiseCompletion(CoordinationRequestCompletion completion)
        => RequestCompleted?.Invoke(completion);
      public void RaiseSendFailure(CoordinationRequestSendFailure failure)
        => RequestSendFailed?.Invoke(failure);
      public void RaiseError(string code, string message)
      {
        ErrorReceived?.Invoke(new CoordinationServerEnvelope
        {
          type = "error",
          code = code,
          message = message
        });
      }

      private bool Record(
        string type,
        string path,
        out CoordinationRequestHandle request)
      {
        if (State != CoordinationConnectionState.Connected)
        {
          request = null;
          return false;
        }

        nextRequestId += 1;
        request = Handle("request-" + nextRequestId, type, path);
        requests.Add(request);
        Requests.Add(type + ":" + path);
        return true;
      }
    }

    private sealed class ManualScheduler : ICoordinationSaveScheduler
    {
      private readonly Queue<Action> immediate = new Queue<Action>();
      private readonly Queue<Action> delayed = new Queue<Action>();

      public void Post(Action action) => immediate.Enqueue(action);
      public void PostAfter(TimeSpan delay, Action action) => delayed.Enqueue(action);

      public void RunImmediate()
      {
        while (immediate.Count > 0)
        {
          immediate.Dequeue().Invoke();
        }
      }

      public void RunNextImmediate()
      {
        immediate.Dequeue().Invoke();
      }

      public void RunDelayed()
      {
        var count = delayed.Count;
        for (var index = 0; index < count; index += 1)
        {
          delayed.Dequeue().Invoke();
        }
      }
    }

    private sealed class FakeConflictDialog : ISaveConflictDialog
    {
      public SaveConflictAction Result { get; set; } = SaveConflictAction.CancelSave;
      public int ShowCount { get; private set; }

      public SaveConflictAction Show(IReadOnlyList<CoordinationSavePathInfo> paths)
      {
        ShowCount += 1;
        return Result;
      }
    }

    private sealed class FakeLocalSavePrompt : IUncoordinatedSavePrompt
    {
      public bool ChooseResult { get; set; }
      public bool ConfirmResult { get; set; }
      public int ChooseCount { get; private set; }
      public int ConfirmCount { get; private set; }
      public CoordinationUncoordinatedSaveRequest LastRequest { get; private set; }

      public bool ChooseLocalSave(CoordinationUncoordinatedSaveRequest request)
      {
        ChooseCount += 1;
        LastRequest = request;
        return ChooseResult;
      }

      public bool ConfirmLocalSave(CoordinationUncoordinatedSaveRequest request)
      {
        ConfirmCount += 1;
        LastRequest = request;
        return ConfirmResult;
      }
    }

    private sealed class FakeGitContext : ICoordinationGitContext
    {
      public string Branch { get; set; } = "feature/task-2";
      public string GetBranch() => Branch;
    }

    private sealed class FakeUncoordinatedSaveStore
      : ICoordinationUncoordinatedSaveStore
    {
      private List<CoordinationUncoordinatedSaveRecord> records
        = new List<CoordinationUncoordinatedSaveRecord>();

      public CoordinationUncoordinatedSaveLoadResult Load()
      {
        return new CoordinationUncoordinatedSaveLoadResult(
          records.Select(record => record.Copy()).ToArray(),
          null,
          null);
      }

      public CoordinationUncoordinatedSaveWriteResult Save(
        IReadOnlyList<CoordinationUncoordinatedSaveRecord> next)
      {
        records = next.Select(record => record.Copy()).ToList();
        return CoordinationUncoordinatedSaveWriteResult.Success();
      }
    }

    private sealed class FakeSaveInvoker : ICoordinationSaveInvoker
    {
      private readonly HashSet<string> dirtyPaths = new HashSet<string>();
      public List<string> Paths { get; } = new List<string>();
      public List<string> Calls { get; } = new List<string>();
      public Queue<bool> Results { get; } = new Queue<bool>();
      public Action<IReadOnlyList<string>> OnSave { get; set; }
      public bool Result { get; set; } = true;

      public bool Save(IReadOnlyList<string> paths)
      {
        Paths.AddRange(paths);
        Calls.Add(string.Join("|", paths));
        OnSave?.Invoke(paths);
        var result = Results.Count > 0 ? Results.Dequeue() : Result;
        if (result)
        {
          AcceptImmediate(paths);
        }
        return result;
      }

      public void MarkDirty(params string[] paths)
      {
        foreach (var path in paths)
        {
          dirtyPaths.Add(Canonical(path));
        }
      }

      public void AcceptImmediate(IEnumerable<string> paths)
      {
        foreach (var path in paths)
        {
          dirtyPaths.Remove(Canonical(path));
        }
      }

      public bool IsDirty(string path) => dirtyPaths.Contains(Canonical(path));

      private static string Canonical(string path)
      {
        CoordinationPathMatcher.TryNormalize(path, out var normalized);
        return CoordinationPathMatcher.ToCanonicalKey(normalized);
      }
    }

    private sealed class FakeWarningLogger : ICoordinationSaveWarningLogger
    {
      public List<string> Messages { get; } = new List<string>();
      public void LogWarning(string message) => Messages.Add(message);
    }

    private static CoordinationServerEnvelope Granted(
      long stateVersion,
      string path,
      string developerId,
      string connectionId)
    {
      return new CoordinationServerEnvelope
      {
        type = "lease.granted",
        stateVersion = stateVersion,
        path = path,
        lease = Lease(path, developerId, developerId, connectionId)
      };
    }

    private static CoordinationServerEnvelope Denied(
      long stateVersion,
      string path,
      CoordinationLeaseRecord lease)
    {
      return new CoordinationServerEnvelope
      {
        type = "lease.denied",
        stateVersion = stateVersion,
        path = path,
        code = "owned_by_other",
        currentLease = lease
      };
    }

    private static CoordinationLeaseRecord RemoteLease(string path, string owner)
      => Lease(path, "dev-remote", owner, "connection-remote");

    private static CoordinationLeaseRecord Lease(
      string path,
      string developerId,
      string displayName,
      string connectionId)
    {
      return new CoordinationLeaseRecord
      {
        leaseId = "editing-lease",
        path = path,
        displayPath = path,
        mode = "editing",
        developerId = developerId,
        displayName = displayName,
        branch = "feature/coordination",
        task = "PP-7",
        expiresAt = "2026-08-08T12:00:00Z",
        connectionId = connectionId
      };
    }

    private static CoordinationRequestHandle Handle(
      string requestId,
      string type,
      string path)
    {
      return (CoordinationRequestHandle)Activator.CreateInstance(
        typeof(CoordinationRequestHandle),
        BindingFlags.Instance | BindingFlags.NonPublic,
        null,
        new object[] { requestId, type, path },
        null);
    }

    private static CoordinationRequestCompletion Completion(
      CoordinationRequestHandle request,
      CoordinationServerEnvelope response,
      bool stale)
    {
      response.requestId = request.RequestId;
      return (CoordinationRequestCompletion)Activator.CreateInstance(
        typeof(CoordinationRequestCompletion),
        BindingFlags.Instance | BindingFlags.NonPublic,
        null,
        new object[] { request, response, stale },
        null);
    }

    private static CoordinationRequestSendFailure SendFailure(
      CoordinationRequestHandle request,
      string message)
    {
      return (CoordinationRequestSendFailure)Activator.CreateInstance(
        typeof(CoordinationRequestSendFailure),
        BindingFlags.Instance | BindingFlags.NonPublic,
        null,
        new object[] { request, message },
        null);
    }
  }
}
