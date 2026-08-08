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
      using var fixture = new SaveFixture();
      fixture.StateStore.ApplyLeaseUpdate(Granted(
        2,
        Laboratory,
        "dev-local",
        "connection-local"));

      var returned = fixture.Filter.FilterPaths(new[] { Laboratory });

      Assert.That(returned, Is.EqualTo(new[] { Laboratory }));
      fixture.Scheduler.RunImmediate();
      Assert.That(fixture.Service.Requests, Is.Empty);
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
      using var fixture = new SaveFixture();
      Assert.That(fixture.Filter.FilterPaths(new[] { Laboratory }), Is.Empty);
      fixture.Scheduler.RunImmediate();
      var request = fixture.Service.RequestFor("lease.acquire", Laboratory);

      fixture.Service.RaiseCompletion(Completion(
        request, Granted(2, Laboratory, "dev-local", "connection-local"), false));

      Assert.That(fixture.Saves.Paths, Is.EqualTo(new[] { Laboratory }));
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
      Assert.That(fixture.Prompt.LastPaths.Select(value => value.Path),
        Is.EqualTo(new[] { Laboratory }));
      Assert.That(fixture.Prompt.LastPaths.Single().LastKnownOwner, Is.EqualTo("Morgan"));
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
      using var fixture = new SaveFixture(CoordinationConnectionState.Offline);
      fixture.Prompt.ChooseResult = true;
      fixture.Prompt.ConfirmResult = true;
      fixture.Filter.FilterPaths(new[] { Laboratory });
      fixture.Scheduler.RunNextImmediate();

      fixture.Service.SetState(CoordinationConnectionState.Connected);
      fixture.Scheduler.RunImmediate();

      Assert.That(fixture.Prompt.ChooseCount, Is.Zero);
      Assert.That(fixture.Service.Requests, Is.EqualTo(new[]
      {
        "lease.acquire:" + Laboratory
      }));
      Assert.That(fixture.Saves.Paths, Is.Empty);
    }

    [Test]
    public void AcquireTimeoutOffersLocalSaveWithoutTreatingTimeoutAsOwnership()
    {
      using var fixture = new SaveFixture();
      fixture.Prompt.ChooseResult = true;
      fixture.Prompt.ConfirmResult = true;
      fixture.Filter.FilterPaths(new[] { Laboratory });
      fixture.Scheduler.RunImmediate();

      fixture.Scheduler.RunDelayed();
      fixture.Scheduler.RunImmediate();

      Assert.That(fixture.Prompt.ChooseCount, Is.EqualTo(1));
      Assert.That(fixture.Saves.Paths, Is.EqualTo(new[] { Laboratory }));
      Assert.That(fixture.StateStore.TryGetLease(Laboratory, out _), Is.False);
    }

    [Test]
    public void OverrideTransportFailureOffersTheTwoStepLocalSave()
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
        acquire, Denied(2, Laboratory, RemoteLease(Laboratory, "Morgan")), false));
      fixture.Scheduler.RunImmediate();
      var overrideRequest = fixture.Service.RequestFor("lease.override", Laboratory);

      fixture.Service.RaiseSendFailure(SendFailure(overrideRequest, "socket closed"));
      fixture.Scheduler.RunImmediate();

      Assert.That(fixture.Prompt.ChooseCount, Is.EqualTo(1));
      Assert.That(fixture.Prompt.ConfirmCount, Is.EqualTo(1));
      Assert.That(fixture.Saves.Paths, Is.EqualTo(new[] { Laboratory }));
    }

    [Test]
    public void AcquireSendFailureDoesNotEnableLocalSaveWhileStillConnected()
    {
      using var fixture = new SaveFixture();
      fixture.Prompt.ChooseResult = true;
      fixture.Prompt.ConfirmResult = true;
      fixture.Filter.FilterPaths(new[] { Laboratory });
      fixture.Scheduler.RunImmediate();
      var acquire = fixture.Service.RequestFor("lease.acquire", Laboratory);

      fixture.Service.RaiseSendFailure(SendFailure(acquire, "rejected before send"));
      fixture.Scheduler.RunImmediate();

      Assert.That(fixture.Prompt.ChooseCount, Is.Zero);
      Assert.That(fixture.Saves.Paths, Is.Empty);
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
      using var fixture = new SaveFixture();
      fixture.SetRemoteLease(Laboratory, "Morgan");
      fixture.Dialog.Result = action;
      fixture.Filter.FilterPaths(new[] { Laboratory });
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
      if (action != SaveConflictAction.OverrideAndSave)
      {
        Assert.That(fixture.Saves.Paths, Is.Empty);
      }
    }

    [Test]
    public void FailedResumeDoesNotRecordAnUncoordinatedSaveOrRetryRecursively()
    {
      using var fixture = new SaveFixture(CoordinationConnectionState.Offline);
      fixture.Prompt.ChooseResult = true;
      fixture.Prompt.ConfirmResult = true;
      fixture.Saves.Result = false;

      fixture.Filter.FilterPaths(new[] { Laboratory });
      fixture.Scheduler.RunImmediate();

      Assert.That(fixture.Saves.Paths, Is.EqualTo(new[] { Laboratory }));
      Assert.That(fixture.WarningState.Warnings, Is.Empty);
      Assert.That(fixture.Logger.Messages, Is.Empty);
    }

    [Test]
    public void PartialOfflineBatchWarnsOnlyForThePathThatActuallySaved()
    {
      using var fixture = new SaveFixture(CoordinationConnectionState.Offline);
      fixture.Prompt.ChooseResult = true;
      fixture.Prompt.ConfirmResult = true;
      fixture.Saves.Results.Enqueue(true);
      fixture.Saves.Results.Enqueue(false);

      fixture.Filter.FilterPaths(new[] { Laboratory, Arena });
      fixture.Scheduler.RunImmediate();

      Assert.That(fixture.Saves.Calls, Is.EqualTo(new[]
      {
        Laboratory,
        Arena
      }));
      Assert.That(fixture.WarningState.Warnings.Single().AffectedPaths,
        Is.EqualTo(new[] { Laboratory }));
      Assert.That(fixture.Logger.Messages.Single(), Does.Contain(Laboratory));
      Assert.That(fixture.Logger.Messages.Single(), Does.Not.Contain(Arena));
    }

    [Test]
    public void AuthoritativeOwnershipClearsThePathFromUncoordinatedSaveWarnings()
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

      Assert.That(fixture.WarningState.Warnings, Is.Empty);
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
        = new CoordinationUncoordinatedSaveState();
      public ManualScheduler Scheduler { get; } = new ManualScheduler();
      public FakeConflictDialog Dialog { get; } = new FakeConflictDialog();
      public FakeLocalSavePrompt Prompt { get; } = new FakeLocalSavePrompt();
      public FakeSaveInvoker Saves { get; } = new FakeSaveInvoker();
      public FakeWarningLogger Logger { get; } = new FakeWarningLogger();
      public CoordinationSaveResumeCoordinator Coordinator { get; }
      public CoordinationSavePathFilter Filter { get; }

      public SaveFixture(
        CoordinationConnectionState state = CoordinationConnectionState.Connected)
      {
        Service = new FakeSaveService(state);
        Coordinator = new CoordinationSaveResumeCoordinator(
          Service,
          StateStore,
          WarningState,
          Scheduler,
          Dialog,
          Prompt,
          Saves,
          Logger,
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

      public void Dispose() => Coordinator.Dispose();
    }

    private sealed class FakeSaveService : ICoordinationSaveService
    {
      private readonly List<CoordinationRequestHandle> requests
        = new List<CoordinationRequestHandle>();
      private int nextRequestId;

      public event Action<CoordinationConnectionState> StateChanged;
      public event Action<CoordinationServerEnvelope> SessionReady;
      public event Action<CoordinationRequestCompletion> RequestCompleted;
      public event Action<CoordinationRequestSendFailure> RequestSendFailed;

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
      public IReadOnlyList<CoordinationSavePathInfo> LastPaths { get; private set; }

      public bool ChooseLocalSave(IReadOnlyList<CoordinationSavePathInfo> paths)
      {
        ChooseCount += 1;
        LastPaths = paths;
        return ChooseResult;
      }

      public bool ConfirmLocalSave(IReadOnlyList<CoordinationSavePathInfo> paths)
      {
        ConfirmCount += 1;
        LastPaths = paths;
        return ConfirmResult;
      }
    }

    private sealed class FakeSaveInvoker : ICoordinationSaveInvoker
    {
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
        return Results.Count > 0 ? Results.Dequeue() : Result;
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
