using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PotionPanic.Editor.Coordination;

namespace PotionPanic.Tests.EditMode.Coordination
{
  public sealed class CoordinationAssetTrackerIntegrationTests
  {
    [Test]
    public void ExposesTheLifecycleServiceOrchestrationContract()
    {
      var trackerType = Type.GetType(
        "PotionPanic.Editor.Coordination.CoordinationAssetTracker, PotionPanic.Editor");

      Assert.That(trackerType, Is.Not.Null);
    }

    [Test]
    public void PublishesAnEnabledSceneAndAcquiresItsExclusiveLeaseWhenDirty()
    {
      var source = new FakeLifecycleSource
      {
        LoadedScenes = new[] { Scene(1, "Assets/Scenes/Laboratory.unity", false) }
      };
      var service = new FakeAssetService();
      using var lifecycle = new CoordinationStageLifecycleAdapter(source);
      using var tracker = CreateTracker(lifecycle, service);

      tracker.Enable();
      source.RaiseSceneDirtied(Scene(1, "Assets/Scenes/Laboratory.unity", true));

      Assert.That(service.Requests, Is.EqualTo(new[]
      {
        "presence.open:Assets/Scenes/Laboratory.unity",
        "lease.acquire:Assets/Scenes/Laboratory.unity"
      }));
    }

    [Test]
    public void ObservesAnExcludedPrefabWithoutPublishingOrLeasingIt()
    {
      var source = new FakeLifecycleSource
      {
        OpenPrefabStage = Prefab(1, "Assets/Prefabs/Potion.prefab", true)
      };
      var service = new FakeAssetService();
      using var lifecycle = new CoordinationStageLifecycleAdapter(source);
      using var tracker = CreateTracker(lifecycle, service);

      tracker.Enable();
      source.RaisePrefabClosed(Prefab(1, "Assets/Prefabs/Potion.prefab", false));

      Assert.That(service.Requests, Is.Empty);
    }

    [Test]
    public void PublishesANonExclusiveDirtyStageWithoutAcquiringALease()
    {
      var source = new FakeLifecycleSource
      {
        LoadedScenes = new[] { Scene(1, "Assets/Scenes/Laboratory.unity", true) }
      };
      var service = new FakeAssetService();
      using var lifecycle = new CoordinationStageLifecycleAdapter(source);
      using var tracker = new CoordinationAssetTracker(lifecycle, service, new[]
      {
        new CoordinatedPathRule
        {
          pattern = "Assets/Scenes/**/*.unity",
          enabled = true,
          exclusive = false
        }
      });

      tracker.Enable();

      Assert.That(service.Requests, Is.EqualTo(new[]
      {
        "presence.open:Assets/Scenes/Laboratory.unity"
      }));
    }

    [Test]
    public void ReconnectRepublishesAllPresenceAndReacquiresOnlyDirtyExclusiveStages()
    {
      var source = new FakeLifecycleSource
      {
        LoadedScenes = new[]
        {
          Scene(1, "Assets/Scenes/Laboratory.unity", false),
          Scene(2, "Assets/Scenes/Arena.unity", true)
        }
      };
      var service = new FakeAssetService();
      using var lifecycle = new CoordinationStageLifecycleAdapter(source);
      using var tracker = CreateTracker(lifecycle, service);
      tracker.Enable();
      service.Requests.Clear();

      service.RaiseSessionReady(4, "dev-local", "connection-new");

      Assert.That(service.Requests, Is.EqualTo(new[]
      {
        "presence.open:Assets/Scenes/Laboratory.unity",
        "presence.open:Assets/Scenes/Arena.unity",
        "lease.acquire:Assets/Scenes/Arena.unity"
      }));
      Assert.That(tracker.StateStore.NewestStateVersion, Is.EqualTo(4));
    }

    [Test]
    public void CloseReleasesPresenceAndOwnedEditingLeaseThenAcceptsResurfacedReservation()
    {
      var source = new FakeLifecycleSource
      {
        LoadedScenes = new[] { Scene(1, "Assets/Scenes/Laboratory.unity", true) }
      };
      var service = new FakeAssetService();
      using var lifecycle = new CoordinationStageLifecycleAdapter(source);
      using var tracker = CreateTracker(lifecycle, service);
      tracker.Enable();
      service.RaiseSessionReady(1, "dev-local", "connection-local");
      service.RaiseLeaseResult(LeaseEnvelope(2,
        Lease("editing", "dev-local", "connection-local")));
      service.Requests.Clear();

      source.RaiseSceneClosed(Scene(1, "Assets/Scenes/Laboratory.unity", false));
      service.RaiseLeaseResult(new CoordinationServerEnvelope
      {
        type = "lease.released",
        stateVersion = 3,
        path = "Assets/Scenes/Laboratory.unity",
        leaseId = "editing-lease"
      });
      service.RaiseLeaseResult(LeaseEnvelope(3, Lease("reserved", "dev-local", null)));

      Assert.That(service.Requests, Is.EqualTo(new[]
      {
        "presence.close:Assets/Scenes/Laboratory.unity",
        "lease.release:Assets/Scenes/Laboratory.unity"
      }));
      Assert.That(tracker.StateStore.TryGetLease("Assets/Scenes/Laboratory.unity",
        out var resurfaced), Is.True);
      Assert.That(resurfaced.mode, Is.EqualTo("reserved"));
    }

    [Test]
    public void CloseDoesNotReleaseAnEditingLeaseOwnedByAnotherConnection()
    {
      var source = new FakeLifecycleSource
      {
        LoadedScenes = new[] { Scene(1, "Assets/Scenes/Laboratory.unity", true) }
      };
      var service = new FakeAssetService();
      using var lifecycle = new CoordinationStageLifecycleAdapter(source);
      using var tracker = CreateTracker(lifecycle, service);
      tracker.Enable();
      service.RaiseSessionReady(1, "dev-local", "connection-local");
      service.RaiseLeaseResult(LeaseEnvelope(2,
        Lease("editing", "dev-local", "connection-other")));
      service.Requests.Clear();

      source.RaiseSceneClosed(Scene(1, "Assets/Scenes/Laboratory.unity", false));

      Assert.That(service.Requests, Is.EqualTo(new[]
      {
        "presence.close:Assets/Scenes/Laboratory.unity"
      }));
    }

    [Test]
    public void GrantThatCompletesAfterStageCloseIsReleasedImmediately()
    {
      var source = new FakeLifecycleSource
      {
        LoadedScenes = new[] { Scene(1, "Assets/Scenes/Laboratory.unity", true) }
      };
      var service = new FakeAssetService();
      using var lifecycle = new CoordinationStageLifecycleAdapter(source);
      using var tracker = CreateTracker(lifecycle, service);
      tracker.Enable();
      service.RaiseSessionReady(1, "dev-local", "connection-local");
      service.Requests.Clear();

      source.RaiseSceneClosed(Scene(1, "Assets/Scenes/Laboratory.unity", false));
      service.RaiseRequestCompleted(CreateCompletion(new CoordinationServerEnvelope
      {
        type = "lease.granted",
        stateVersion = 2,
        path = "Assets/Scenes/Laboratory.unity",
        lease = Lease("editing", "dev-local", "connection-local")
      }, false, service.LastAcquireRequest));

      Assert.That(service.Requests, Is.EqualTo(new[]
      {
        "presence.close:Assets/Scenes/Laboratory.unity",
        "lease.release:Assets/Scenes/Laboratory.unity"
      }));
    }

    [Test]
    public void OldGrantIsReleasedAfterTheSamePathReopensClean()
    {
      var source = new FakeLifecycleSource();
      var service = new FakeAssetService();
      using var lifecycle = new CoordinationStageLifecycleAdapter(source);
      using var tracker = CreateTracker(lifecycle, service);
      tracker.Enable();
      service.RaiseSessionReady(1, "dev-local", "connection-local");
      source.RaiseSceneOpened(Scene(1, "Assets/Scenes/Laboratory.unity", true));
      var oldAcquire = service.LastAcquireRequest;
      source.RaiseSceneClosed(Scene(1, "Assets/Scenes/Laboratory.unity", false));
      source.RaiseSceneOpened(Scene(2, "Assets/Scenes/Laboratory.unity", false));
      service.Requests.Clear();

      service.RaiseRequestCompleted(CreateCompletion(new CoordinationServerEnvelope
      {
        type = "lease.granted",
        stateVersion = 2,
        path = "Assets/Scenes/Laboratory.unity",
        lease = Lease("editing", "dev-local", "connection-local")
      }, false, oldAcquire));

      Assert.That(service.Requests, Is.EqualTo(new[]
      {
        "lease.release:Assets/Scenes/Laboratory.unity"
      }));
    }

    [Test]
    public void StaleReplayCompletionCannotReplaceCurrentAuthoritativeLeaseState()
    {
      var source = new FakeLifecycleSource();
      var service = new FakeAssetService();
      using var lifecycle = new CoordinationStageLifecycleAdapter(source);
      using var tracker = CreateTracker(lifecycle, service);
      tracker.Enable();
      service.RaiseSessionReady(10, "dev-local", "connection-local");
      service.RaiseLeaseResult(LeaseEnvelope(10,
        Lease("editing", "dev-current", "connection-current")));

      service.RaiseRequestCompleted(CreateCompletion(LeaseEnvelope(9,
        Lease("editing", "dev-stale", "connection-stale")), true));

      Assert.That(tracker.StateStore.TryGetLease("Assets/Scenes/Laboratory.unity",
        out var lease), Is.True);
      Assert.That(lease.developerId, Is.EqualTo("dev-current"));
      Assert.That(tracker.StateStore.NewestStateVersion, Is.EqualTo(10));
    }

    [Test]
    public void CurrentSnapshotPresenceAndLeaseEventsPopulateTheAuthoritativeStore()
    {
      var source = new FakeLifecycleSource();
      var service = new FakeAssetService();
      using var lifecycle = new CoordinationStageLifecycleAdapter(source);
      using var tracker = CreateTracker(lifecycle, service);
      tracker.Enable();
      service.RaiseSessionReady(1, "dev-local", "connection-local");
      service.RaiseSnapshot(new CoordinationServerEnvelope
      {
        type = "snapshot",
        stateVersion = 1,
        presence = Array.Empty<CoordinationPresenceRecord>(),
        leases = Array.Empty<CoordinationLeaseRecord>()
      });
      service.RaisePresence(new CoordinationServerEnvelope
      {
        type = "presence.updated",
        stateVersion = 2,
        presence = new[]
        {
          new CoordinationPresenceRecord
          {
            path = "Assets/Scenes/Laboratory.unity",
            connectionId = "connection-remote"
          }
        }
      });
      Assert.That(tracker.StateStore.GetPresence("Assets/Scenes/Laboratory.unity"),
        Has.Count.EqualTo(1));
      service.RaisePresenceRemoved(new CoordinationServerEnvelope
      {
        type = "presence.removed",
        stateVersion = 3,
        path = "Assets/Scenes/Laboratory.unity",
        connectionId = "connection-remote"
      });
      service.RaiseLeaseResult(LeaseEnvelope(4,
        Lease("editing", "dev-remote", "connection-remote")));

      Assert.That(tracker.StateStore.GetPresence("Assets/Scenes/Laboratory.unity"),
        Is.Empty);
      Assert.That(tracker.StateStore.TryGetLease("Assets/Scenes/Laboratory.unity",
        out var lease), Is.True);
      Assert.That(lease.developerId, Is.EqualTo("dev-remote"));
      Assert.That(tracker.StateStore.NewestStateVersion, Is.EqualTo(4));
    }

    [Test]
    public void FirstSaveOfAnUntitledScenePublishesPresenceForTheNewPath()
    {
      var source = new FakeLifecycleSource();
      var service = new FakeAssetService();
      using var lifecycle = new CoordinationStageLifecycleAdapter(source);
      using var tracker = CreateTracker(lifecycle, service);
      tracker.Enable();

      source.RaiseSceneOpened(Scene(7, string.Empty, false));
      source.RaiseSceneSaved(Scene(7, "Assets/Scenes/FirstSave.unity", false));

      Assert.That(service.Requests, Is.EqualTo(new[]
      {
        "presence.open:Assets/Scenes/FirstSave.unity"
      }));
    }

    [Test]
    public void SaveAsClosesTheOldCoordinationAndOpensTheNewPath()
    {
      var source = new FakeLifecycleSource();
      var service = new FakeAssetService();
      using var lifecycle = new CoordinationStageLifecycleAdapter(source);
      using var tracker = CreateTracker(lifecycle, service);
      tracker.Enable();
      service.RaiseSessionReady(1, "dev-local", "connection-local");
      source.RaiseSceneOpened(Scene(7, "Assets/Scenes/Original.unity", true));
      service.RaiseLeaseResult(LeaseEnvelope(2,
        Lease("editing", "dev-local", "connection-local", "Assets/Scenes/Original.unity")));
      service.Requests.Clear();

      source.RaiseSceneSaved(Scene(7, "Assets/Scenes/Copy.unity", false));

      Assert.That(service.Requests, Is.EqualTo(new[]
      {
        "presence.close:Assets/Scenes/Original.unity",
        "lease.release:Assets/Scenes/Original.unity",
        "presence.open:Assets/Scenes/Copy.unity"
      }));
    }

    [Test]
    public void FailedAcquireCannotCompleteAfterItsCorrelationIsRemoved()
    {
      var source = new FakeLifecycleSource();
      var service = new FakeAssetService();
      using var lifecycle = new CoordinationStageLifecycleAdapter(source);
      using var tracker = CreateTracker(lifecycle, service);
      tracker.Enable();
      service.RaiseSessionReady(1, "dev-local", "connection-local");
      source.RaiseSceneOpened(Scene(7, "Assets/Scenes/Laboratory.unity", true));
      var failedAcquire = service.LastAcquireRequest;

      service.RaiseRequestSendFailed(CreateSendFailure(failedAcquire, "socket closed"));
      source.RaiseSceneClosed(Scene(7, "Assets/Scenes/Laboratory.unity", false));
      service.Requests.Clear();
      service.RaiseRequestCompleted(CreateCompletion(new CoordinationServerEnvelope
      {
        type = "lease.granted",
        stateVersion = 2,
        path = "Assets/Scenes/Laboratory.unity",
        lease = Lease("editing", "dev-local", "connection-local")
      }, false, failedAcquire));

      Assert.That(service.Requests, Is.Empty);
    }

    private static CoordinationAssetTracker CreateTracker(
      CoordinationStageLifecycleAdapter lifecycle,
      ICoordinationAssetService service)
    {
      return new CoordinationAssetTracker(lifecycle, service, new[]
      {
        new CoordinatedPathRule
        {
          pattern = "Assets/Scenes/**/*.unity",
          enabled = true,
          exclusive = true
        }
      });
    }

    private static CoordinationLifecycleStageCandidate Scene(
      ulong instanceId,
      string path,
      bool isDirty)
    {
      return new CoordinationLifecycleStageCandidate(
        CoordinationStageKind.Scene, instanceId, path, isDirty);
    }

    private static CoordinationLifecycleStageCandidate Prefab(
      ulong instanceId,
      string path,
      bool isDirty)
    {
      return new CoordinationLifecycleStageCandidate(
        CoordinationStageKind.Prefab, instanceId, path, isDirty);
    }

    private static CoordinationLeaseRecord Lease(
      string mode,
      string developerId,
      string connectionId,
      string path = "Assets/Scenes/Laboratory.unity")
    {
      return new CoordinationLeaseRecord
      {
        leaseId = mode == "editing" ? "editing-lease" : "reservation",
        path = path,
        displayPath = path,
        mode = mode,
        developerId = developerId,
        displayName = developerId,
        branch = "feature/coordination",
        task = "PP-7",
        expiresAt = "2026-08-08T12:00:00Z",
        connectionId = connectionId
      };
    }

    private static CoordinationServerEnvelope LeaseEnvelope(
      long stateVersion,
      CoordinationLeaseRecord lease)
    {
      return new CoordinationServerEnvelope
      {
        type = "lease.updated",
        stateVersion = stateVersion,
        lease = lease
      };
    }

    private static CoordinationRequestCompletion CreateCompletion(
      CoordinationServerEnvelope response,
      bool isStaleReplay,
      CoordinationRequestHandle handle = null)
    {
      handle = handle ?? CreateHandle("request-1", "lease.acquire",
        response.path ?? response.lease.path);
      return (CoordinationRequestCompletion)Activator.CreateInstance(
        typeof(CoordinationRequestCompletion),
        BindingFlags.Instance | BindingFlags.NonPublic,
        null,
        new object[] { handle, response, isStaleReplay },
        null);
    }

    private static CoordinationRequestHandle CreateHandle(
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

    private static CoordinationRequestSendFailure CreateSendFailure(
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

    private sealed class FakeAssetService : ICoordinationAssetService
    {
      public event Action<CoordinationServerEnvelope> SessionReady;
      public event Action<CoordinationServerEnvelope> SnapshotReceived;
      public event Action<CoordinationServerEnvelope> PresenceReceived;
      public event Action<CoordinationServerEnvelope> PresenceRemoved;
      public event Action<CoordinationServerEnvelope> LeaseResultReceived;
      public event Action<CoordinationRequestCompletion> RequestCompleted;
      public event Action<CoordinationRequestSendFailure> RequestSendFailed;

      public List<string> Requests { get; } = new List<string>();
      public CoordinationRequestHandle LastAcquireRequest { get; private set; }
      private int requestNumber;

      public bool TryOpenPresence(string path, out CoordinationRequestHandle request)
        => Record("presence.open", path, out request);
      public bool TryClosePresence(string path, out CoordinationRequestHandle request)
        => Record("presence.close", path, out request);
      public bool TryAcquireLease(string path, out CoordinationRequestHandle request)
        => Record("lease.acquire", path, out request);
      public bool TryReleaseLease(string path, out CoordinationRequestHandle request)
        => Record("lease.release", path, out request);

      public void RaiseSessionReady(long stateVersion, string developerId, string connectionId)
      {
        SessionReady?.Invoke(new CoordinationServerEnvelope
        {
          type = "session.ready",
          stateVersion = stateVersion,
          developerId = developerId,
          connectionId = connectionId
        });
      }

      public void RaiseLeaseResult(CoordinationServerEnvelope envelope)
        => LeaseResultReceived?.Invoke(envelope);
      public void RaiseSnapshot(CoordinationServerEnvelope envelope)
        => SnapshotReceived?.Invoke(envelope);
      public void RaisePresence(CoordinationServerEnvelope envelope)
        => PresenceReceived?.Invoke(envelope);
      public void RaisePresenceRemoved(CoordinationServerEnvelope envelope)
        => PresenceRemoved?.Invoke(envelope);
      public void RaiseRequestCompleted(CoordinationRequestCompletion completion)
        => RequestCompleted?.Invoke(completion);
      public void RaiseRequestSendFailed(CoordinationRequestSendFailure failure)
        => RequestSendFailed?.Invoke(failure);

      private bool Record(string type, string path, out CoordinationRequestHandle request)
      {
        Requests.Add(type + ":" + path);
        requestNumber += 1;
        request = CreateHandle("request-" + requestNumber, type, path);
        if (type == "lease.acquire")
        {
          LastAcquireRequest = request;
        }
        return true;
      }
    }

    private sealed class FakeLifecycleSource : ICoordinationStageLifecycleSource
    {
      public event Action<CoordinationLifecycleStageCandidate> SceneOpened;
      public event Action<CoordinationLifecycleStageCandidate> SceneDirtied;
      public event Action<CoordinationLifecycleStageCandidate> SceneSaved;
      public event Action<CoordinationLifecycleStageCandidate> SceneClosed;
      public event Action<CoordinationLifecycleStageCandidate> PrefabOpened;
      public event Action<CoordinationLifecycleStageCandidate> PrefabDirtied;
      public event Action<CoordinationLifecycleStageCandidate> PrefabSaved;
      public event Action<CoordinationLifecycleStageCandidate> PrefabClosed;

      public IEnumerable<CoordinationLifecycleStageCandidate> LoadedScenes { get; set; }
        = Array.Empty<CoordinationLifecycleStageCandidate>();
      public CoordinationLifecycleStageCandidate OpenPrefabStage { get; set; }

      public IEnumerable<CoordinationLifecycleStageCandidate> GetLoadedScenes() => LoadedScenes;
      public CoordinationLifecycleStageCandidate GetOpenPrefabStage() => OpenPrefabStage;
      public void RaiseSceneOpened(CoordinationLifecycleStageCandidate candidate)
        => SceneOpened?.Invoke(candidate);
      public void RaiseSceneDirtied(CoordinationLifecycleStageCandidate candidate)
        => SceneDirtied?.Invoke(candidate);
      public void RaiseSceneSaved(CoordinationLifecycleStageCandidate candidate)
        => SceneSaved?.Invoke(candidate);
      public void RaiseSceneClosed(CoordinationLifecycleStageCandidate candidate)
        => SceneClosed?.Invoke(candidate);
      public void RaisePrefabOpened(CoordinationLifecycleStageCandidate candidate)
        => PrefabOpened?.Invoke(candidate);
      public void RaisePrefabDirtied(CoordinationLifecycleStageCandidate candidate)
        => PrefabDirtied?.Invoke(candidate);
      public void RaisePrefabSaved(CoordinationLifecycleStageCandidate candidate)
        => PrefabSaved?.Invoke(candidate);
      public void RaisePrefabClosed(CoordinationLifecycleStageCandidate candidate)
        => PrefabClosed?.Invoke(candidate);
    }
  }
}
