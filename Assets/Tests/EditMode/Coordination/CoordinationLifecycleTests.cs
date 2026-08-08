using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PotionPanic.Editor.Coordination;

#pragma warning disable 0067

namespace PotionPanic.Tests.EditMode.Coordination
{
  public sealed class CoordinationLifecycleTests
  {
    [Test]
    public void NormalShutdownQueuesPresenceAndOwnedEditingLeaseRelease()
    {
      var source = new FakeStageSource
      {
        LoadedScenes = new[]
        {
          new CoordinationLifecycleStageCandidate(
            CoordinationStageKind.Scene,
            1,
            "Assets/Scenes/Laboratory.unity",
            true)
        }
      };
      var service = new FakeAssetService();
      var state = new CoordinationStateStore();
      using var lifecycle = new CoordinationStageLifecycleAdapter(source);
      using var tracker = new CoordinationAssetTracker(
        lifecycle,
        service,
        ExclusiveSceneRules(),
        state);
      tracker.Enable();
      service.RaiseSessionReady("dev-local", "connection-local");
      service.RaiseLease(EditingLease("dev-local", "connection-local"));
      service.Requests.Clear();

      tracker.ReleaseOwnedCoordination();

      Assert.That(service.Requests, Is.EqualTo(new[]
      {
        "presence.close:Assets/Scenes/Laboratory.unity",
        "lease.release:Assets/Scenes/Laboratory.unity"
      }));
    }

    [Test]
    public void WarningClearsOnAssetCloseOrAuthoritativeLocalOwnership()
    {
      var source = new FakeStageSource();
      var service = new FakeWarningService();
      var state = new CoordinationStateStore();
      var warnings = new CoordinationUncoordinatedSaveState();
      using var lifecycle = new CoordinationStageLifecycleAdapter(source);
      using var controller = new CoordinationUncoordinatedWarningController(
        lifecycle, service, state, warnings);
      lifecycle.Enable();
      controller.Enable();
      AddWarning(warnings, new[]
      {
        new CoordinationSavePathInfo("Assets/Scenes/First.unity", "Sol"),
        new CoordinationSavePathInfo("Assets/Scenes/Second.unity", "Sol")
      });

      source.RaiseSceneOpened(1, "Assets/Scenes/First.unity");
      source.RaiseSceneClosed(1, "Assets/Scenes/First.unity");

      Assert.That(warnings.Warnings[0].AffectedPaths,
        Is.EqualTo(new[] { "Assets/Scenes/Second.unity" }));

      service.RaiseSessionReady("dev-local", "connection-local");
      state.ApplyLeaseUpdate(new CoordinationServerEnvelope
      {
        protocolVersion = 1,
        type = "lease.updated",
        stateVersion = 2,
        path = "assets/scenes/second.unity",
        lease = new CoordinationLeaseRecord
        {
          path = "assets/scenes/second.unity",
          displayPath = "Assets/Scenes/Second.unity",
          mode = "editing",
          developerId = "dev-local",
          connectionId = "connection-local"
        }
      });

      Assert.That(warnings.Warnings, Is.Empty);
    }

    [Test]
    public async Task BootstrapPreventsDuplicatesAndShutsDownBeforeDomainReload()
    {
      var hooks = new FakeLifecycleHooks();
      var runtimes = new List<FakeRuntime>();
      var bootstrap = new CoordinationBootstrapController(hooks, () =>
      {
        var runtime = new FakeRuntime();
        runtimes.Add(runtime);
        return runtime;
      });

      bootstrap.Enable();
      bootstrap.Enable();
      await bootstrap.StartAsync();
      await bootstrap.StartAsync();

      Assert.That(hooks.ReloadSubscriberCount, Is.EqualTo(1));
      Assert.That(hooks.RestartSubscriberCount, Is.EqualTo(1));
      Assert.That(hooks.ShutdownSubscriberCount, Is.EqualTo(1));
      Assert.That(runtimes, Has.Count.EqualTo(1));
      Assert.That(runtimes[0].StartCount, Is.EqualTo(1));

      hooks.RaiseReload();
      await bootstrap.ShutdownAsync();
      Assert.That(runtimes[0].ShutdownCount, Is.EqualTo(1));
    }

    [Test]
    public async Task BootstrapRestartsAfterCompilationWithoutDomainReload()
    {
      var hooks = new FakeLifecycleHooks();
      var runtimes = new List<FakeRuntime>();
      var bootstrap = new CoordinationBootstrapController(hooks, () =>
      {
        var runtime = new FakeRuntime();
        runtimes.Add(runtime);
        return runtime;
      });

      bootstrap.Enable();
      await bootstrap.StartAsync();
      hooks.RaiseReload();
      await bootstrap.ShutdownAsync();

      hooks.RaiseRestart();
      Assert.That(runtimes, Has.Count.EqualTo(2));
      Assert.That(runtimes[1].StartCount, Is.EqualTo(1));
    }

    [Test]
    public async Task BootstrapStartsRuntimeShutdownBeforeWaitingForStartup()
    {
      var hooks = new FakeLifecycleHooks();
      var runtime = new BlockingStartRuntime();
      var bootstrap = new CoordinationBootstrapController(hooks, () => runtime);
      bootstrap.Enable();

      var startTask = bootstrap.StartAsync();
      var shutdownTask = bootstrap.ShutdownAsync();
      await Task.Yield();
      var shutdownCountBeforeStartupCompletes = runtime.ShutdownCount;

      runtime.CompleteStart();
      await startTask;
      await shutdownTask;

      Assert.That(shutdownCountBeforeStartupCompletes, Is.EqualTo(1));
      Assert.That(runtime.ShutdownCount, Is.EqualTo(1));
    }

    [Test]
    public void ReloadShutdownDoesNotRequireTheUnitySynchronizationContextToPump()
    {
      var hooks = new FakeLifecycleHooks();
      var runtime = new ContextCapturingRuntime();
      var bootstrap = new CoordinationBootstrapController(hooks, () => runtime);
      bootstrap.Enable();
      var context = new QueuedSynchronizationContext();
      var thread = new Thread(() =>
      {
        SynchronizationContext.SetSynchronizationContext(context);
        _ = bootstrap.StartAsync();
        hooks.RaiseReload();
      })
      {
        IsBackground = true
      };

      thread.Start();
      var completedWithoutPump = thread.Join(TimeSpan.FromSeconds(1));
      context.Drain();
      thread.Join(TimeSpan.FromSeconds(1));

      Assert.That(completedWithoutPump, Is.True);
      Assert.That(runtime.ShutdownCount, Is.EqualTo(1));
    }

    private static CoordinatedPathRule[] ExclusiveSceneRules()
    {
      return new[]
      {
        new CoordinatedPathRule
        {
          pattern = "Assets/Scenes/**/*.unity",
          enabled = true,
          exclusive = true
        }
      };
    }

    private static void AddWarning(
      CoordinationUncoordinatedSaveState warnings,
      IReadOnlyList<CoordinationSavePathInfo> paths)
    {
      var method = typeof(CoordinationUncoordinatedSaveState).GetMethod(
        "Add",
        BindingFlags.Instance | BindingFlags.NonPublic);
      Assert.That(method, Is.Not.Null);
      method.Invoke(warnings, new object[] { paths });
    }

    private static CoordinationLeaseRecord EditingLease(
      string developerId,
      string connectionId)
    {
      return new CoordinationLeaseRecord
      {
        leaseId = Guid.NewGuid().ToString(),
        path = "assets/scenes/laboratory.unity",
        displayPath = "Assets/Scenes/Laboratory.unity",
        mode = "editing",
        developerId = developerId,
        displayName = "Rin",
        connectionId = connectionId,
        expiresAt = "2026-08-08T12:00:00Z"
      };
    }

    private sealed class FakeRuntime : ICoordinationEditorRuntime
    {
      public int StartCount { get; private set; }
      public int ShutdownCount { get; private set; }
      public CoordinationWindowViewModel ViewModel => null;

      public Task StartAsync()
      {
        StartCount += 1;
        return Task.CompletedTask;
      }

      public Task ShutdownAsync()
      {
        ShutdownCount += 1;
        return Task.CompletedTask;
      }

      public void FlushPendingNotifications()
      {
      }
    }

    private sealed class BlockingStartRuntime : ICoordinationEditorRuntime
    {
      private readonly TaskCompletionSource<bool> started
        = new TaskCompletionSource<bool>();

      public int ShutdownCount { get; private set; }
      public CoordinationWindowViewModel ViewModel => null;

      public Task StartAsync() => started.Task;

      public Task ShutdownAsync()
      {
        ShutdownCount += 1;
        started.TrySetResult(true);
        return Task.CompletedTask;
      }

      public void CompleteStart() => started.TrySetResult(true);

      public void FlushPendingNotifications()
      {
      }
    }

    private sealed class ContextCapturingRuntime : ICoordinationEditorRuntime
    {
      private readonly TaskCompletionSource<bool> startupGate
        = new TaskCompletionSource<bool>();

      public int ShutdownCount { get; private set; }
      public CoordinationWindowViewModel ViewModel => null;

      public async Task StartAsync()
      {
        await startupGate.Task;
      }

      public async Task ShutdownAsync()
      {
        startupGate.TrySetResult(true);
        await Task.Delay(10);
        ShutdownCount += 1;
      }

      public void FlushPendingNotifications()
      {
      }
    }

    private sealed class QueuedSynchronizationContext : SynchronizationContext
    {
      private readonly Queue<(SendOrPostCallback Callback, object State)> callbacks
        = new Queue<(SendOrPostCallback Callback, object State)>();

      public override void Post(SendOrPostCallback callback, object state)
      {
        lock (callbacks)
        {
          callbacks.Enqueue((callback, state));
        }
      }

      public void Drain()
      {
        while (true)
        {
          (SendOrPostCallback Callback, object State) work;
          lock (callbacks)
          {
            if (callbacks.Count == 0)
            {
              return;
            }
            work = callbacks.Dequeue();
          }
          work.Callback(work.State);
        }
      }
    }

    private sealed class FakeLifecycleHooks : ICoordinationEditorLifecycleHooks
    {
      private Action reload;
      private Action restart;
      private Action shutdown;
      public int ReloadSubscriberCount => reload?.GetInvocationList().Length ?? 0;
      public int RestartSubscriberCount => restart?.GetInvocationList().Length ?? 0;
      public int ShutdownSubscriberCount => shutdown?.GetInvocationList().Length ?? 0;

      public event Action Reloading
      {
        add => reload += value;
        remove => reload -= value;
      }

      public event Action Restarting
      {
        add => restart += value;
        remove => restart -= value;
      }

      public event Action ShuttingDown
      {
        add => shutdown += value;
        remove => shutdown -= value;
      }

      public void RaiseReload() => reload?.Invoke();
      public void RaiseRestart() => restart?.Invoke();
    }

    private sealed class FakeWarningService : ICoordinationWarningService
    {
      public CoordinationConnectionState State { get; private set; }
        = CoordinationConnectionState.Connected;
      public event Action<CoordinationConnectionState> StateChanged;
      public event Action<CoordinationServerEnvelope> SessionReady;

      public void RaiseSessionReady(string developerId, string connectionId)
      {
        SessionReady?.Invoke(new CoordinationServerEnvelope
        {
          protocolVersion = 1,
          type = "session.ready",
          stateVersion = 1,
          developerId = developerId,
          displayName = "Rin",
          connectionId = connectionId,
          leaseTtlSeconds = 120,
          reservationTtlSeconds = 1800,
          serverTime = "2026-08-08T10:00:00Z"
        });
      }
    }

    private sealed class FakeAssetService : ICoordinationAssetService
    {
      public List<string> Requests { get; } = new List<string>();
      public event Action<CoordinationServerEnvelope> SessionReady;
      public event Action<CoordinationServerEnvelope> SnapshotReceived;
      public event Action<CoordinationServerEnvelope> PresenceReceived;
      public event Action<CoordinationServerEnvelope> PresenceRemoved;
      public event Action<CoordinationServerEnvelope> LeaseResultReceived;
      public event Action<CoordinationRequestCompletion> RequestCompleted;
      public event Action<CoordinationRequestSendFailure> RequestSendFailed;

      public bool TryOpenPresence(string path, out CoordinationRequestHandle request)
        => Record("presence.open", path, out request);
      public bool TryClosePresence(string path, out CoordinationRequestHandle request)
        => Record("presence.close", path, out request);
      public bool TryAcquireLease(string path, out CoordinationRequestHandle request)
        => Record("lease.acquire", path, out request);
      public bool TryReleaseLease(string path, out CoordinationRequestHandle request)
        => Record("lease.release", path, out request);

      public void RaiseSessionReady(string developerId, string connectionId)
      {
        SessionReady?.Invoke(new CoordinationServerEnvelope
        {
          protocolVersion = 1,
          type = "session.ready",
          stateVersion = 1,
          developerId = developerId,
          displayName = "Rin",
          connectionId = connectionId,
          leaseTtlSeconds = 120,
          reservationTtlSeconds = 1800,
          serverTime = "2026-08-08T10:00:00Z"
        });
      }

      public void RaiseLease(CoordinationLeaseRecord lease)
      {
        LeaseResultReceived?.Invoke(new CoordinationServerEnvelope
        {
          protocolVersion = 1,
          type = "lease.updated",
          stateVersion = 2,
          path = lease.path,
          lease = lease
        });
      }

      private bool Record(string type, string path, out CoordinationRequestHandle request)
      {
        Requests.Add(type + ":" + path);
        request = null;
        return true;
      }
    }

    private sealed class FakeStageSource : ICoordinationStageLifecycleSource
    {
      public CoordinationLifecycleStageCandidate[] LoadedScenes { get; set; }
        = Array.Empty<CoordinationLifecycleStageCandidate>();
      public event Action<CoordinationLifecycleStageCandidate> SceneOpened;
      public event Action<CoordinationLifecycleStageCandidate> SceneDirtied;
      public event Action<CoordinationLifecycleStageCandidate> SceneSaved;
      public event Action<CoordinationLifecycleStageCandidate> SceneClosed;
      public event Action<CoordinationLifecycleStageCandidate> PrefabOpened;
      public event Action<CoordinationLifecycleStageCandidate> PrefabDirtied;
      public event Action<CoordinationLifecycleStageCandidate> PrefabSaved;
      public event Action<CoordinationLifecycleStageCandidate> PrefabClosed;

      public IEnumerable<CoordinationLifecycleStageCandidate> GetLoadedScenes()
        => LoadedScenes;
      public CoordinationLifecycleStageCandidate GetOpenPrefabStage() => null;

      public void RaiseSceneOpened(long id, string path)
        => SceneOpened?.Invoke(Scene(id, path));
      public void RaiseSceneClosed(long id, string path)
        => SceneClosed?.Invoke(Scene(id, path));

      private static CoordinationLifecycleStageCandidate Scene(long id, string path)
      {
        return new CoordinationLifecycleStageCandidate(
          CoordinationStageKind.Scene, (ulong)id, path, false);
      }
    }
  }
}
