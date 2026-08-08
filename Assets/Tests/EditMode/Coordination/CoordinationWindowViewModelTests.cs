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
      Assert.That(fixture.ViewModel.CanOverride, Is.False);
      Assert.That(fixture.ViewModel.CanCopyCanonicalPath, Is.True);

      fixture.Service.SetState(CoordinationConnectionState.Connected);
      Assert.That(fixture.ViewModel.CanReserve, Is.True);
      fixture.State.ApplySnapshot(Snapshot(null,
        EditingLease("Assets/Scenes/Laboratory.unity", "dev-remote", "Sol"), null));

      Assert.That(fixture.ViewModel.CanReconnect, Is.False);
      Assert.That(fixture.ViewModel.CanReserve, Is.False);
      Assert.That(fixture.ViewModel.CanRelease, Is.False);
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
      Assert.That(fixture.ViewModel.CanOverride, Is.False);
      Assert.That(fixture.ViewModel.Release(), Is.True);
      Assert.That(fixture.Service.Requests[1],
        Is.EqualTo("lease.release:Assets/Scenes/Laboratory.unity"));
    }

    [Test]
    public void PersistsTaskContextAndDisabledWithoutWritingCredentials()
    {
      var fixture = new ViewModelFixture();
      fixture.ViewModel.Enable();

      fixture.ViewModel.TaskContext = "PP-7 Slice 08";
      fixture.ViewModel.SetDisabled(true);

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
      public CoordinationUncoordinatedSaveState Warnings { get; }
        = new CoordinationUncoordinatedSaveState();
      public FakeSettingsStore Store { get; } = new FakeSettingsStore();
      public FakeClipboard Clipboard { get; } = new FakeClipboard();
      public CoordinationWindowViewModel ViewModel { get; }

      public ViewModelFixture(bool isSupportedPlatform = true, string gitBranch = null)
      {
        Service = new FakeWindowService(isSupportedPlatform);
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
          Clipboard);
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
