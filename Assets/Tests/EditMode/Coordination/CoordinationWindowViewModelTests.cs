using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using PotionPanic.Editor.Coordination;

namespace PotionPanic.Tests.EditMode.Coordination
{
  public sealed class CoordinationWindowViewModelTests
  {
    [Test]
    public void ExposesIdentityBranchStateAndAuthoritativeRows()
    {
      var fixture = new ViewModelFixture();
      fixture.Service.SetIdentity("dev-local", "Rin", "connection-local");
      fixture.Service.SetState(CoordinationConnectionState.Connected);
      fixture.State.ApplySnapshot(Snapshot(
        Presence("Assets/Scenes/Laboratory.unity", "dev-remote", "Sol"),
        EditingLease("Assets/Scenes/Laboratory.unity", "dev-remote", "Sol"),
        Reservation("Assets/Scenes/Queued.unity", "dev-local", "Rin")));

      fixture.ViewModel.Enable();

      Assert.That(fixture.ViewModel.Identity, Is.EqualTo("Rin (dev-local)"));
      Assert.That(fixture.ViewModel.Branch, Is.EqualTo("coordination-slice-08"));
      Assert.That(fixture.ViewModel.ConnectionState,
        Is.EqualTo(CoordinationConnectionState.Connected));
      Assert.That(fixture.ViewModel.Presence.Count, Is.EqualTo(1));
      Assert.That(fixture.ViewModel.EditingLeases.Count, Is.EqualTo(1));
      Assert.That(fixture.ViewModel.Reservations.Count, Is.EqualTo(1));
      Assert.That(fixture.ViewModel.EditingLeases[0].Owner, Is.EqualTo("Sol"));
      Assert.That(fixture.ViewModel.Reservations[0].IsLocal, Is.True);
      Assert.That(fixture.ViewModel.Presence[0].Kind,
        Is.EqualTo(CoordinationWindowRowKind.Presence));
      Assert.That(fixture.ViewModel.EditingLeases[0].Kind,
        Is.EqualTo(CoordinationWindowRowKind.EditingLease));
      Assert.That(fixture.ViewModel.Reservations[0].Kind,
        Is.EqualTo(CoordinationWindowRowKind.Reservation));
    }

    [Test]
    public void EnablesNetworkActionsOnlyWhenConnectedAndApplicable()
    {
      var fixture = new ViewModelFixture();
      fixture.Service.SetIdentity("dev-local", "Rin", "connection-local");
      fixture.Service.SetState(CoordinationConnectionState.Offline);
      fixture.ViewModel.Enable();
      fixture.ViewModel.SelectedPath = "Assets\\Scenes\\Laboratory.unity";

      Assert.That(fixture.ViewModel.CanReconnect, Is.True);
      Assert.That(fixture.ViewModel.CanReserve, Is.False);
      Assert.That(fixture.ViewModel.CanRelease, Is.False);
      Assert.That(fixture.ViewModel.CanCancelReservation, Is.False);
      Assert.That(fixture.ViewModel.CanOverride, Is.False);
      Assert.That(fixture.ViewModel.CanCopyCanonicalPath, Is.True);

      fixture.Service.SetState(CoordinationConnectionState.Connected);
      Assert.That(fixture.ViewModel.Freshness,
        Is.EqualTo(CoordinationDataFreshness.WaitingForSnapshot));
      Assert.That(fixture.ViewModel.CanReserve, Is.False);
      fixture.State.ApplySnapshot(Snapshot(null, null, null));
      Assert.That(fixture.ViewModel.Freshness, Is.EqualTo(CoordinationDataFreshness.Live));
      Assert.That(fixture.ViewModel.CanReserve, Is.True);
      fixture.State.ApplySnapshot(Snapshot(null,
        EditingLease("Assets/Scenes/Laboratory.unity", "dev-remote", "Sol"), null));

      Assert.That(fixture.ViewModel.CanReconnect, Is.False);
      Assert.That(fixture.ViewModel.CanReserve, Is.False);
      Assert.That(fixture.ViewModel.CanRelease, Is.False);
      Assert.That(fixture.ViewModel.CanCancelReservation, Is.False);
      Assert.That(fixture.ViewModel.CanOverride, Is.True);
      Assert.That(fixture.ViewModel.Override(), Is.True);
      Assert.That(fixture.Service.Requests,
        Is.EqualTo(new[] { "lease.override:Assets/Scenes/Laboratory.unity" }));

      fixture.State.ApplySnapshot(Snapshot(null,
        EditingLease(
          "Assets/Scenes/Laboratory.unity",
          "dev-local",
          "Rin",
          "connection-local"),
        null));

      Assert.That(fixture.ViewModel.CanRelease, Is.True);
      Assert.That(fixture.ViewModel.CanCancelReservation, Is.False);
      Assert.That(fixture.ViewModel.CanOverride, Is.False);
      Assert.That(fixture.ViewModel.Release(), Is.True);
      Assert.That(fixture.Service.Requests[1],
        Is.EqualTo("lease.release:Assets/Scenes/Laboratory.unity"));
    }

    [Test]
    public void PersistsTaskContextAndManualModeWithoutWritingCredentials()
    {
      var fixture = new ViewModelFixture();
      fixture.ViewModel.Enable();

      fixture.ViewModel.TaskContext = "PP-7 Slice 08";
      Assert.That(fixture.ViewModel.SetMode(CoordinationMode.Manual), Is.True);

      Assert.That(fixture.Settings.taskContext, Is.EqualTo("PP-7 Slice 08"));
      Assert.That(fixture.Settings.disabled, Is.True);
      Assert.That(fixture.Store.SavedJson, Does.Contain("PP-7 Slice 08"));
      Assert.That(fixture.Store.SavedJson, Does.Contain("\"disabled\": true"));
      Assert.That(fixture.Store.SavedJson, Does.Not.Contain("developerToken"));
      Assert.That(fixture.Store.SavedJson, Does.Not.Contain("sessionToken"));
      Assert.That(fixture.Service.DisabledValues, Is.EqualTo(new[] { true }));
    }

    [Test]
    public void ClampsTaskContextWithoutSplittingSurrogatePairs()
    {
      var fixture = new ViewModelFixture();
      var value = new string('x', 254) + "\U0001F9EA" + "z";

      fixture.ViewModel.TaskContext = value;

      Assert.That(fixture.Settings.taskContext.Length,
        Is.EqualTo(CoordinationProtocol.MaximumContextLength));
      Assert.That(fixture.Settings.taskContext,
        Is.EqualTo(new string('x', 254) + "\U0001F9EA"));
      Assert.That(fixture.Store.SavedJson, Does.Not.Contain("z"));
    }

    [Test]
    public void ClampsGitBranchContext()
    {
      var branch = new string('b', CoordinationProtocol.MaximumContextLength + 1);
      var fixture = new ViewModelFixture(gitBranch: branch);

      Assert.That(fixture.ViewModel.Branch.Length,
        Is.EqualTo(CoordinationProtocol.MaximumContextLength));
    }

    [Test]
    public void UnsupportedPlatformForcesDisabledAndPreventsEnabling()
    {
      var fixture = new ViewModelFixture(isSupportedPlatform: false);
      fixture.ViewModel.Enable();

      Assert.That(fixture.ViewModel.IsDisabled, Is.True);
      Assert.That(fixture.ViewModel.CanEditDisabled, Is.False);
      Assert.That(fixture.ViewModel.CanReconnect, Is.False);

      fixture.ViewModel.SetDisabled(false);

      Assert.That(fixture.Settings.disabled, Is.False);
      Assert.That(fixture.Service.DisabledValues, Is.Empty);
    }

    [Test]
    public void CopyCanonicalPathNormalizesTheSelectedPath()
    {
      var fixture = new ViewModelFixture();
      fixture.ViewModel.Enable();
      fixture.ViewModel.SelectedPath = "Assets\\Scenes\\Laboratory.unity";

      Assert.That(fixture.ViewModel.CopyCanonicalPath(), Is.True);
      Assert.That(fixture.Clipboard.Value, Is.EqualTo("Assets/Scenes/Laboratory.unity"));
    }

    [Test]
    public void ModeMapsTheExistingDisabledSettingAndManualRequiresConfirmation()
    {
      var fixture = new ViewModelFixture();
      fixture.Settings.disabled = true;

      Assert.That(fixture.ViewModel.Mode, Is.EqualTo(CoordinationMode.Manual));

      fixture.Settings.disabled = false;
      fixture.Confirmations.ManualResult = false;
      fixture.Service.SetState(CoordinationConnectionState.Connected);

      Assert.That(fixture.ViewModel.SetMode(CoordinationMode.Manual), Is.False);
      Assert.That(fixture.ViewModel.Mode, Is.EqualTo(CoordinationMode.Coordinated));
      Assert.That(fixture.Service.DisabledValues, Is.Empty);
      Assert.That(fixture.Confirmations.ManualMessage,
        Does.Contain("Reservations may remain"));

      fixture.Confirmations.ManualResult = true;
      Assert.That(fixture.ViewModel.SetMode(CoordinationMode.Manual), Is.True);
      Assert.That(fixture.Settings.disabled, Is.True);
      Assert.That(fixture.Service.DisabledValues, Is.EqualTo(new[] { true }));
    }

    [Test]
    public void CoordinatedModeStartsTheExistingConnectionPath()
    {
      var fixture = new ViewModelFixture();
      fixture.Settings.disabled = true;

      Assert.That(fixture.ViewModel.SetMode(CoordinationMode.Coordinated), Is.True);
      Assert.That(fixture.Settings.disabled, Is.False);
      Assert.That(fixture.Service.DisabledValues, Is.EqualTo(new[] { false }));
    }

    [Test]
    public void ActiveStageFollowsUntilProjectOrManualTargetIsChosen()
    {
      var fixture = new ViewModelFixture();
      fixture.Paths.ActiveStagePath = "Assets/Scenes/Laboratory.unity";
      fixture.ViewModel.Enable();

      Assert.That(fixture.ViewModel.SelectedPath,
        Is.EqualTo("Assets/Scenes/Laboratory.unity"));
      Assert.That(fixture.ViewModel.TargetSource,
        Is.EqualTo(CoordinationTargetSource.ActiveStage));

      fixture.Paths.ActiveStagePath = "Assets/Scenes/Arena.unity";
      fixture.ViewModel.RefreshActiveStage();
      Assert.That(fixture.ViewModel.SelectedPath, Is.EqualTo("Assets/Scenes/Arena.unity"));

      fixture.Paths.ProjectSelectionPath = "Assets/Scenes/Queued.unity";
      Assert.That(fixture.ViewModel.UseProjectSelection(), Is.True);
      fixture.Paths.ActiveStagePath = "Assets/Scenes/Laboratory.unity";
      fixture.ViewModel.RefreshActiveStage();
      Assert.That(fixture.ViewModel.SelectedPath, Is.EqualTo("Assets/Scenes/Queued.unity"));

      fixture.ViewModel.SelectedPath = "Assets/Scenes/Manual.unity";
      Assert.That(fixture.ViewModel.TargetSource,
        Is.EqualTo(CoordinationTargetSource.ManualPath));
      Assert.That(fixture.ViewModel.FollowActiveStage(), Is.True);
      Assert.That(fixture.ViewModel.SelectedPath,
        Is.EqualTo("Assets/Scenes/Laboratory.unity"));
    }

    [Test]
    public void SelectingARowOnlyExpandsItAndNeverChangesTheActionTarget()
    {
      var fixture = new ViewModelFixture();
      fixture.Paths.ActiveStagePath = "Assets/Scenes/Laboratory.unity";
      fixture.ViewModel.Enable();
      fixture.State.ApplySnapshot(Snapshot(
        Presence("Assets/Scenes/Queued.unity", "dev-remote", "Sol"), null, null));

      fixture.ViewModel.SelectRow(fixture.ViewModel.Presence[0]);

      Assert.That(fixture.ViewModel.SelectedPath,
        Is.EqualTo("Assets/Scenes/Laboratory.unity"));
      Assert.That(fixture.ViewModel.IsExpanded(fixture.ViewModel.Presence[0]), Is.True);
    }

    [Test]
    public void StaleDataIsReadOnlyAndPrimaryActionUsesAuthoritativeOwnership()
    {
      var fixture = new ViewModelFixture();
      fixture.Service.SetIdentity("dev-local", "Rin", "connection-local");
      fixture.Service.SetState(CoordinationConnectionState.Connected);
      fixture.State.ApplySnapshot(Snapshot(null,
        EditingLease("Assets/Scenes/Laboratory.unity", "dev-local", "Rin",
          "connection-local"), null));
      fixture.ViewModel.SelectedPath = "Assets/Scenes/Laboratory.unity";

      Assert.That(fixture.ViewModel.PrimaryAction,
        Is.EqualTo(CoordinationPrimaryAction.ReleaseEditingLease));
      fixture.Service.SetState(CoordinationConnectionState.Offline);

      Assert.That(fixture.ViewModel.Freshness, Is.EqualTo(CoordinationDataFreshness.Stale));
      Assert.That(fixture.ViewModel.PrimaryAction, Is.EqualTo(CoordinationPrimaryAction.None));
      Assert.That(fixture.ViewModel.CanOverride, Is.False);
      Assert.That(fixture.ViewModel.Release(), Is.False);
    }

    [Test]
    public void ReconciliationRequiresConfirmationAndKeepsTheWarningOnWriteFailure()
    {
      var store = new FailingWarningStore();
      var warnings = new CoordinationUncoordinatedSaveState(
        new CoordinationUncoordinatedSaveLedger(store, new FixedClock()));
      warnings.RecordSave("Assets/Scenes/Laboratory.unity",
        CoordinationUncoordinatedSaveReason.Manual, "Sol", "feature/test", "PP-9");
      var fixture = new ViewModelFixture(warnings);
      var warning = fixture.ViewModel.OutstandingWarnings[0];

      fixture.Confirmations.ReconcileResult = false;
      Assert.That(fixture.ViewModel.MarkReconciled(warning), Is.False);
      Assert.That(fixture.ViewModel.OutstandingWarnings.Count, Is.EqualTo(1));
      Assert.That(fixture.Confirmations.ReconcileMessage,
        Does.Contain("does not merge files or update server history"));

      fixture.Confirmations.ReconcileResult = true;
      store.FailWrites = true;
      Assert.That(fixture.ViewModel.MarkReconciled(warning), Is.False);
      Assert.That(fixture.ViewModel.OutstandingWarnings.Count, Is.EqualTo(1));
    }

    [TestCase("Could not read the warning ledger.", null)]
    [TestCase("Malformed warning ledger was quarantined.", "UserSettings/quarantine.json")]
    public void ExposesWarningStoreErrorsWhenThereAreNoOutstandingRecords(
      string error,
      string quarantinePath)
    {
      var warnings = new CoordinationUncoordinatedSaveState(
        new CoordinationUncoordinatedSaveLedger(
          new LoadErrorWarningStore(error, quarantinePath), new FixedClock()));
      var fixture = new ViewModelFixture(warnings);

      Assert.That(fixture.ViewModel.OutstandingWarnings, Is.Empty);
      Assert.That(fixture.ViewModel.WarningStoreError, Is.EqualTo(error));
    }

    [Test]
    public void DisablesLeaseActionsForPathsOutsideConfiguredRules()
    {
      var fixture = new ViewModelFixture();
      fixture.Service.SetIdentity("dev-local", "Rin", "connection-local");
      fixture.Service.SetState(CoordinationConnectionState.Connected);
      fixture.ViewModel.Enable();
      fixture.ViewModel.SelectedPath = "Assets/Scripts/Runtime/Player.cs";

      Assert.That(fixture.ViewModel.CanReserve, Is.False);
      Assert.That(fixture.ViewModel.CanRelease, Is.False);
      Assert.That(fixture.ViewModel.CanOverride, Is.False);
      Assert.That(fixture.ViewModel.Reserve(), Is.False);
      Assert.That(fixture.Service.Requests, Is.Empty);
      Assert.That(fixture.ViewModel.CanCopyCanonicalPath, Is.True);
    }

    [Test]
    public void SelectingARowExpandsItWithoutRetargetingTheCurrentAsset()
    {
      var fixture = new ViewModelFixture();
      fixture.Service.SetIdentity("dev-local", "Rin", "connection-local");
      fixture.State.ApplySnapshot(Snapshot(
        Presence("Assets/Scenes/Laboratory.unity", "dev-remote", "Sol"),
        EditingLease("Assets/Scenes/Laboratory.unity", "dev-remote", "Sol"),
        null));

      fixture.ViewModel.SelectRow(fixture.ViewModel.EditingLeases[0]);

      Assert.That(fixture.ViewModel.SelectedPath, Is.Empty);
      Assert.That(fixture.ViewModel.IsExpanded(fixture.ViewModel.EditingLeases[0]), Is.True);
      Assert.That(fixture.ViewModel.IsExpanded(fixture.ViewModel.Presence[0]), Is.False);
    }

    [Test]
    public void PathSourceButtonsNormalizePathsAndReportUnavailableSources()
    {
      var fixture = new ViewModelFixture();
      fixture.Paths.ActiveStagePath = "Assets\\Scenes\\Laboratory.unity";
      fixture.Paths.ProjectSelectionPath = "Assets/Scenes/Queued.unity";

      Assert.That(fixture.ViewModel.UseActiveStage(), Is.True);
      Assert.That(fixture.ViewModel.SelectedPath,
        Is.EqualTo("Assets/Scenes/Laboratory.unity"));
      Assert.That(fixture.ViewModel.UseProjectSelection(), Is.True);
      Assert.That(fixture.ViewModel.SelectedPath, Is.EqualTo("Assets/Scenes/Queued.unity"));

      fixture.Paths.ActiveStagePath = null;
      Assert.That(fixture.ViewModel.UseActiveStage(), Is.False);
      Assert.That(fixture.ViewModel.TargetHelpText,
        Is.EqualTo("The active stage is not a saved asset under Assets/."));
      Assert.That(fixture.ViewModel.SelectedPath, Is.Empty);
    }

    [Test]
    public void FailedFollowActiveStageClearsTheOldTargetAndCannotReserveIt()
    {
      var fixture = new ViewModelFixture();
      fixture.Service.SetState(CoordinationConnectionState.Connected);
      fixture.State.ApplySnapshot(Snapshot(null, null, null));
      fixture.ViewModel.SelectedPath = "Assets/Scenes/Queued.unity";
      fixture.Paths.ActiveStagePath = null;

      Assert.That(fixture.ViewModel.FollowActiveStage(), Is.False);
      Assert.That(fixture.ViewModel.SelectedPath, Is.Empty);
      Assert.That(fixture.ViewModel.PrimaryAction, Is.EqualTo(CoordinationPrimaryAction.None));
      Assert.That(fixture.ViewModel.Reserve(), Is.False);
      Assert.That(fixture.Service.Requests, Is.Empty);
    }

    [Test]
    public void InvalidActiveStagePathClearsTheOldTargetAndCannotReserveIt()
    {
      var fixture = new ViewModelFixture();
      fixture.Service.SetState(CoordinationConnectionState.Connected);
      fixture.State.ApplySnapshot(Snapshot(null, null, null));
      fixture.ViewModel.SelectedPath = "Assets/Scenes/Queued.unity";
      fixture.Paths.ActiveStagePath = "Packages/Example.asset";

      Assert.That(fixture.ViewModel.FollowActiveStage(), Is.False);
      Assert.That(fixture.ViewModel.SelectedPath, Is.Empty);
      Assert.That(fixture.ViewModel.PrimaryAction, Is.EqualTo(CoordinationPrimaryAction.None));
      Assert.That(fixture.ViewModel.Reserve(), Is.False);
      Assert.That(fixture.Service.Requests, Is.Empty);
    }

    [Test]
    public void LocalReservationCanBeCancelledFromTheTargetOrItsRow()
    {
      var fixture = new ViewModelFixture();
      fixture.Service.SetIdentity("dev-local", "Rin", "connection-local");
      fixture.Service.SetState(CoordinationConnectionState.Connected);
      fixture.State.ApplySnapshot(Snapshot(null, null,
        Reservation("Assets/Scenes/Queued.unity", "dev-local", "Rin")));
      var row = fixture.ViewModel.Reservations[0];

      fixture.ViewModel.SelectedPath = row.Path;

      Assert.That(fixture.ViewModel.CanCancelReservation, Is.True);
      Assert.That(fixture.ViewModel.CanCancelReservationRow(row), Is.True);
      Assert.That(fixture.ViewModel.TargetHelpText,
        Is.EqualTo("You own this reservation. Cancel reservation is available."));
      Assert.That(fixture.ViewModel.CancelReservation(row), Is.True);
      Assert.That(fixture.Service.Requests,
        Is.EqualTo(new[] { "reservation.cancel:Assets/Scenes/Queued.unity" }));
    }

    [Test]
    public void StaleLocalReservationRowCannotCancelANewRemoteReservation()
    {
      var fixture = new ViewModelFixture();
      fixture.Service.SetIdentity("dev-local", "Rin", "connection-local");
      fixture.Service.SetState(CoordinationConnectionState.Connected);
      fixture.State.ApplySnapshot(Snapshot(null, null,
        Reservation("Assets/Scenes/Queued.unity", "dev-local", "Rin")));
      var staleRow = fixture.ViewModel.Reservations[0];
      fixture.State.ApplySnapshot(Snapshot(null, null,
        Reservation("Assets/Scenes/Queued.unity", "dev-remote", "Sol")));

      Assert.That(fixture.ViewModel.CanCancelReservationRow(staleRow), Is.False);
      Assert.That(fixture.ViewModel.CancelReservation(staleRow), Is.False);
      Assert.That(fixture.Service.Requests, Is.Empty);
    }

    [Test]
    public void RemoteRowOverrideRequiresConfirmationAndUsesCurrentOwner()
    {
      var fixture = new ViewModelFixture();
      fixture.Service.SetIdentity("dev-local", "Rin", "connection-local");
      fixture.Service.SetState(CoordinationConnectionState.Connected);
      fixture.State.ApplySnapshot(Snapshot(null,
        EditingLease("Assets/Scenes/Laboratory.unity", "dev-remote", "Sol"), null));
      var row = fixture.ViewModel.EditingLeases[0];
      fixture.Confirmation.Result = false;

      Assert.That(fixture.ViewModel.Override(row), Is.False);
      Assert.That(fixture.Service.Requests, Is.Empty);
      Assert.That(fixture.Confirmation.Path,
        Is.EqualTo("Assets/Scenes/Laboratory.unity"));
      Assert.That(fixture.Confirmation.Owner, Is.EqualTo("Sol"));

      fixture.Confirmation.Result = true;
      Assert.That(fixture.ViewModel.Override(row), Is.True);
      Assert.That(fixture.Service.Requests,
        Is.EqualTo(new[] { "lease.override:Assets/Scenes/Laboratory.unity" }));
    }

    [Test]
    public void SelectedOverrideDoesNotSendWhenTheLeaseChangesDuringConfirmation()
    {
      var fixture = new ViewModelFixture();
      fixture.Service.SetIdentity("dev-local", "Rin", "connection-local");
      fixture.Service.SetState(CoordinationConnectionState.Connected);
      fixture.State.ApplySnapshot(Snapshot(null,
        EditingLease("Assets/Scenes/Laboratory.unity", "dev-remote", "Sol"), null));
      fixture.ViewModel.SelectedPath = "Assets/Scenes/Laboratory.unity";
      fixture.Confirmation.OnConfirm = (_, __) => fixture.State.ApplySnapshot(Snapshot(null,
        EditingLease("Assets/Scenes/Laboratory.unity", "dev-other", "Ari"), null));

      Assert.That(fixture.ViewModel.Override(), Is.False);
      Assert.That(fixture.Service.Requests, Is.Empty);
    }

    [Test]
    public void RowOverrideDoesNotSendWhenTheLeaseChangesDuringConfirmation()
    {
      var fixture = new ViewModelFixture();
      fixture.Service.SetIdentity("dev-local", "Rin", "connection-local");
      fixture.Service.SetState(CoordinationConnectionState.Connected);
      fixture.State.ApplySnapshot(Snapshot(null,
        EditingLease("Assets/Scenes/Laboratory.unity", "dev-remote", "Sol"), null));
      var row = fixture.ViewModel.EditingLeases[0];
      fixture.Confirmation.OnConfirm = (_, __) => fixture.State.ApplySnapshot(Snapshot(null,
        EditingLease("Assets/Scenes/Laboratory.unity", "dev-other", "Ari"), null));

      Assert.That(fixture.ViewModel.Override(row), Is.False);
      Assert.That(fixture.Service.Requests, Is.Empty);
    }

    [Test]
    public void ManualModeReportsRetainedRowsAsStaleEvenBeforeAsyncShutdownCompletes()
    {
      var fixture = new ViewModelFixture();
      fixture.Service.SetState(CoordinationConnectionState.Connected);
      fixture.State.ApplySnapshot(Snapshot(null,
        EditingLease("Assets/Scenes/Laboratory.unity", "dev-remote", "Sol"), null));
      fixture.Settings.disabled = true;

      Assert.That(fixture.ViewModel.Freshness, Is.EqualTo(CoordinationDataFreshness.Stale));
      Assert.That(fixture.ViewModel.CanOverride, Is.False);
    }

    [Test]
    public void ManualModeWithoutRowsReportsUnavailableBeforeAsyncShutdownCompletes()
    {
      var fixture = new ViewModelFixture();
      fixture.Service.SetState(CoordinationConnectionState.Connected);
      fixture.State.ApplySnapshot(Snapshot(null, null, null));
      fixture.Settings.disabled = true;

      Assert.That(fixture.ViewModel.Freshness,
        Is.EqualTo(CoordinationDataFreshness.Unavailable));
    }

    [TestCase(CoordinationMode.Manual)]
    [TestCase(CoordinationConnectionState.Reconnecting)]
    [TestCase(CoordinationConnectionState.AuthenticationFailed)]
    public void RetainedRowsAreStaleAndReadOnlyForManualOrDisconnectedStates(object state)
    {
      var fixture = new ViewModelFixture();
      fixture.Service.SetIdentity("dev-local", "Rin", "connection-local");
      fixture.Service.SetState(CoordinationConnectionState.Connected);
      fixture.State.ApplySnapshot(Snapshot(null,
        EditingLease("Assets/Scenes/Laboratory.unity", "dev-local", "Rin",
          "connection-local"), null));
      fixture.ViewModel.SelectedPath = "Assets/Scenes/Laboratory.unity";

      if (state is CoordinationMode)
      {
        fixture.Settings.disabled = true;
      }
      else
      {
        fixture.Service.SetState((CoordinationConnectionState)state);
      }

      Assert.That(fixture.ViewModel.Freshness, Is.EqualTo(CoordinationDataFreshness.Stale));
      Assert.That(fixture.ViewModel.PrimaryAction, Is.EqualTo(CoordinationPrimaryAction.None));
      Assert.That(fixture.ViewModel.Release(), Is.False);
      Assert.That(fixture.Service.Requests, Is.Empty);
    }

    [Test]
    public void ReconciliationRemovesOnlyTheConfirmedRecordAfterASuccessfulWrite()
    {
      var store = new FailingWarningStore();
      var warnings = new CoordinationUncoordinatedSaveState(
        new CoordinationUncoordinatedSaveLedger(store, new FixedClock()));
      warnings.RecordSave("Assets/Scenes/Laboratory.unity",
        CoordinationUncoordinatedSaveReason.Manual, "Sol", "feature/test", "PP-9");
      warnings.RecordSave("Assets/Scenes/Queued.unity",
        CoordinationUncoordinatedSaveReason.Offline, "Ari", "feature/test", "PP-9");
      var fixture = new ViewModelFixture(warnings);
      var first = fixture.ViewModel.OutstandingWarnings[0];

      Assert.That(fixture.ViewModel.MarkReconciled(first), Is.True);
      Assert.That(fixture.ViewModel.OutstandingWarnings.Count, Is.EqualTo(1));
      Assert.That(fixture.ViewModel.OutstandingWarnings[0].Path,
        Is.EqualTo("Assets/Scenes/Queued.unity"));
    }

    [Test]
    public void ForgetCredentialsRequiresConfirmationBeforeCallingTheService()
    {
      var fixture = new ViewModelFixture();
      fixture.Confirmations.ForgetCredentialsResult = false;

      Assert.That(fixture.ViewModel.ForgetCredentials(), Is.False);
      Assert.That(fixture.Service.Requests, Is.Empty);

      fixture.Confirmations.ForgetCredentialsResult = true;
      Assert.That(fixture.ViewModel.ForgetCredentials(), Is.True);
      Assert.That(fixture.Service.Requests, Is.EqualTo(new[] { "forget" }));
    }

    [Test]
    public void TargetHelpExplainsInvalidUncoordinatedDisconnectedAndFreePaths()
    {
      var fixture = new ViewModelFixture();

      Assert.That(fixture.ViewModel.TargetHelpText, Does.StartWith("Choose the active stage"));
      fixture.ViewModel.SelectedPath = "Packages/example.asset";
      Assert.That(fixture.ViewModel.TargetHelpText,
        Is.EqualTo("Choose an asset under Assets/."));
      fixture.ViewModel.SelectedPath = "Assets/Scripts/Runtime/Player.cs";
      Assert.That(fixture.ViewModel.TargetHelpText,
        Is.EqualTo("This path is not covered by a coordination rule."));
      fixture.ViewModel.SelectedPath = "Assets/Scenes/Laboratory.unity";
      Assert.That(fixture.ViewModel.TargetHelpText,
        Is.EqualTo("Reconnect to change claims. Copy path remains available."));
      fixture.Service.SetState(CoordinationConnectionState.Connected);
      Assert.That(fixture.ViewModel.TargetHelpText,
        Is.EqualTo("Waiting for team data. Claim changes remain unavailable."));
      fixture.State.ApplySnapshot(Snapshot(null, null, null));
      Assert.That(fixture.ViewModel.TargetHelpText,
        Is.EqualTo("No current claim. Reserve is available."));
    }

    private static CoordinationServerEnvelope Snapshot(
      CoordinationPresenceRecord presence,
      CoordinationLeaseRecord editing,
      CoordinationLeaseRecord reservation)
    {
      var presenceRecords = presence == null
        ? Array.Empty<CoordinationPresenceRecord>()
        : new[] { presence };
      var leases = new List<CoordinationLeaseRecord>();
      if (editing != null)
      {
        leases.Add(editing);
      }
      if (reservation != null)
      {
        leases.Add(reservation);
      }

      return new CoordinationServerEnvelope
      {
        protocolVersion = 1,
        type = "snapshot",
        stateVersion = 2,
        presence = presenceRecords,
        leases = leases.ToArray()
      };
    }

    private static CoordinationPresenceRecord Presence(
      string path,
      string developerId,
      string displayName)
    {
      return new CoordinationPresenceRecord
      {
        path = path.ToLowerInvariant(),
        displayPath = path,
        developerId = developerId,
        displayName = displayName,
        connectionId = "connection-remote",
        branch = "feature/other",
        task = "Other task",
        expiresAt = "2026-08-08T12:00:00Z"
      };
    }

    private static CoordinationLeaseRecord EditingLease(
      string path,
      string developerId,
      string displayName,
      string connectionId = "connection-remote")
    {
      return Lease(path, developerId, displayName, "editing", connectionId);
    }

    private static CoordinationLeaseRecord Reservation(
      string path,
      string developerId,
      string displayName)
    {
      return Lease(path, developerId, displayName, "reserved", string.Empty);
    }

    private static CoordinationLeaseRecord Lease(
      string path,
      string developerId,
      string displayName,
      string mode,
      string connectionId)
    {
      return new CoordinationLeaseRecord
      {
        leaseId = Guid.NewGuid().ToString(),
        path = path.ToLowerInvariant(),
        displayPath = path,
        mode = mode,
        developerId = developerId,
        displayName = displayName,
        connectionId = connectionId,
        branch = "feature/other",
        task = "Other task",
        expiresAt = "2026-08-08T12:00:00Z"
      };
    }

    private sealed class ViewModelFixture
    {
      public CoordinationUserSettings Settings { get; }
        = CoordinationUserSettings.CreateDefault();
      public FakeWindowService Service { get; }
      public CoordinationStateStore State { get; } = new CoordinationStateStore();
      public CoordinationUncoordinatedSaveState Warnings { get; private set; }
        = new CoordinationUncoordinatedSaveState();
      public FakeSettingsStore Store { get; } = new FakeSettingsStore();
      public FakeClipboard Clipboard { get; } = new FakeClipboard();
      public FakePathSource Paths { get; } = new FakePathSource();
      public FakeOverrideConfirmation Confirmation { get; }
        = new FakeOverrideConfirmation();
      public FakeWindowConfirmation Confirmations { get; }
        = new FakeWindowConfirmation();
      public CoordinationWindowViewModel ViewModel { get; }

      public ViewModelFixture(
        CoordinationUncoordinatedSaveState warnings = null,
        bool isSupportedPlatform = true,
        string gitBranch = null)
      {
        Service = new FakeWindowService(isSupportedPlatform);
        if (warnings != null)
        {
          Warnings = warnings;
        }
        ViewModel = new CoordinationWindowViewModel(
          Service,
          State,
          Warnings,
          Settings,
          Store,
          new[]
          {
            new CoordinatedPathRule
            {
              pattern = "Assets/Scenes/**/*.unity",
              enabled = true,
              exclusive = true
            }
          },
          new FakeGitContext(gitBranch),
          Clipboard,
          Paths,
          Confirmation,
          Confirmations);
      }
    }

    private sealed class FakeSettingsStore : ICoordinationUserSettingsStore
    {
      public string SavedJson { get; private set; } = string.Empty;

      public void Save(CoordinationUserSettings settings)
      {
        SavedJson = CoordinationUserSettings.ToJson(settings);
      }
    }

    private sealed class FakeClipboard : ICoordinationClipboard
    {
      public string Value { get; private set; } = string.Empty;
      public void SetText(string value) => Value = value;
    }

    private sealed class FakeGitContext : ICoordinationGitContext
    {
      private readonly string branch;

      public FakeGitContext(string branch = null)
      {
        this.branch = branch ?? "coordination-slice-08";
      }

      public string GetBranch() => branch;
    }

    private sealed class FakePathSource : ICoordinationWindowPathSource
    {
      public string ActiveStagePath { get; set; }
      public string ProjectSelectionPath { get; set; }

      public bool TryGetActiveStagePath(out string path)
      {
        path = ActiveStagePath;
        return path != null;
      }

      public bool TryGetProjectSelectionPath(out string path)
      {
        path = ProjectSelectionPath;
        return path != null;
      }
    }

    private sealed class FakeOverrideConfirmation : ICoordinationOverrideConfirmation
    {
      public bool Result { get; set; } = true;
      public string Path { get; private set; }
      public string Owner { get; private set; }
      public Action<string, string> OnConfirm { get; set; }

      public bool Confirm(string path, string owner)
      {
        Path = path;
        Owner = owner;
        OnConfirm?.Invoke(path, owner);
        return Result;
      }
    }

    private sealed class FakeWindowConfirmation : ICoordinationWindowConfirmation
    {
      public bool ManualResult { get; set; } = true;
      public bool ReconcileResult { get; set; } = true;
      public bool ForgetCredentialsResult { get; set; } = true;
      public string ManualMessage { get; private set; }
      public string ReconcileMessage { get; private set; }

      public bool ConfirmManualMode(string message)
      {
        ManualMessage = message;
        return ManualResult;
      }

      public bool ConfirmReconciliation(string path, string message)
      {
        ReconcileMessage = message;
        return ReconcileResult;
      }

      public bool ConfirmForgetCredentials(string message) => ForgetCredentialsResult;
    }

    private sealed class FixedClock : ICoordinationClock
    {
      public DateTimeOffset UtcNow => new DateTimeOffset(2026, 8, 10, 12, 0, 0,
        TimeSpan.Zero);
    }

    private sealed class FailingWarningStore : ICoordinationUncoordinatedSaveStore
    {
      public bool FailWrites { get; set; }

      public CoordinationUncoordinatedSaveLoadResult Load()
      {
        return new CoordinationUncoordinatedSaveLoadResult(
          Array.Empty<CoordinationUncoordinatedSaveRecord>(), null, null);
      }

      public CoordinationUncoordinatedSaveWriteResult Save(
        IReadOnlyList<CoordinationUncoordinatedSaveRecord> records)
      {
        return FailWrites
          ? CoordinationUncoordinatedSaveWriteResult.Failure("disk unavailable")
          : CoordinationUncoordinatedSaveWriteResult.Success();
      }
    }

    private sealed class LoadErrorWarningStore : ICoordinationUncoordinatedSaveStore
    {
      private readonly string error;
      private readonly string quarantinePath;

      public LoadErrorWarningStore(string error, string quarantinePath)
      {
        this.error = error;
        this.quarantinePath = quarantinePath;
      }

      public CoordinationUncoordinatedSaveLoadResult Load()
      {
        return new CoordinationUncoordinatedSaveLoadResult(
          Array.Empty<CoordinationUncoordinatedSaveRecord>(), quarantinePath, error);
      }

      public CoordinationUncoordinatedSaveWriteResult Save(
        IReadOnlyList<CoordinationUncoordinatedSaveRecord> records)
      {
        return CoordinationUncoordinatedSaveWriteResult.Success();
      }
    }

    private sealed class FakeWindowService : ICoordinationWindowService
    {
      public CoordinationConnectionState State { get; private set; }
        = CoordinationConnectionState.Offline;
      public string DeveloperId { get; private set; } = string.Empty;
      public string DisplayName { get; private set; } = string.Empty;
      public string ConnectionId { get; private set; } = string.Empty;
      public bool IsSupportedPlatform { get; }
      public List<string> Requests { get; } = new List<string>();
      public List<bool> DisabledValues { get; } = new List<bool>();

      public event Action<CoordinationConnectionState> StateChanged;

      public FakeWindowService(bool isSupportedPlatform)
      {
        IsSupportedPlatform = isSupportedPlatform;
      }

      public Task ConnectAsync()
      {
        Requests.Add("reconnect");
        return Task.CompletedTask;
      }

      public Task ForgetCredentialsAsync()
      {
        Requests.Add("forget");
        return Task.CompletedTask;
      }

      public Task SetDisabledAsync(bool disabled)
      {
        DisabledValues.Add(disabled);
        return Task.CompletedTask;
      }

      public bool TryReserveLease(string path, out CoordinationRequestHandle request)
        => Record("lease.reserve", path, out request);
      public bool TryReleaseLease(string path, out CoordinationRequestHandle request)
        => Record("lease.release", path, out request);
      public bool TryCancelReservation(string path, out CoordinationRequestHandle request)
        => Record("reservation.cancel", path, out request);
      public bool TryOverrideLease(string path, out CoordinationRequestHandle request)
        => Record("lease.override", path, out request);

      public void SetState(CoordinationConnectionState state)
      {
        State = state;
        StateChanged?.Invoke(state);
      }

      public void SetIdentity(string developerId, string displayName, string connectionId)
      {
        DeveloperId = developerId;
        DisplayName = displayName;
        ConnectionId = connectionId;
      }

      private bool Record(string type, string path, out CoordinationRequestHandle request)
      {
        Requests.Add(type + ":" + path);
        request = null;
        return true;
      }
    }
  }
}
