using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PotionPanic.Editor.Coordination;

namespace PotionPanic.Tests.EditMode.Coordination
{
  public sealed class CoordinationServiceTests
  {
    private const string SnapshotId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
    private const string OtherSnapshotId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
    private static readonly CoordinationConfig Configuration = new CoordinationConfig
    {
      schemaVersion = 1,
      projectId = "potion-panic",
      serverBaseUrl = "https://coordination.example.test",
      heartbeatSeconds = 30,
      rules = Array.Empty<CoordinatedPathRule>()
    };

    [Test]
    public void CredentialStoreKeepsTokenAtTheProjectCredentialTargetAndForgetsIt()
    {
      var store = new MemoryCredentialStore();
      var target = CoordinationCredentialStore.GetDeveloperTokenTarget("potion-panic");

      store.Write(target, "developer-token");

      Assert.That(store.TryRead(target, out var token), Is.True);
      Assert.That(token, Is.EqualTo("developer-token"));
      store.Delete(target);
      Assert.That(store.TryRead(target, out _), Is.False);
    }

    [Test]
    public void ParsesServerIdentityAndTtlsWithoutPersistingTheSessionToken()
    {
      const string json = "{\"developerId\":\"dev-1\",\"displayName\":\"Rin\","
        + "\"sessionToken\":\"session-secret\",\"serverTime\":\"2026-08-07T12:00:00Z\","
        + "\"leaseTtlSeconds\":120,\"reservationTtlSeconds\":1800,\"stateVersion\":4}";

      Assert.That(CoordinationSessionResponse.TryParse(json, out var response, out _), Is.True);
      Assert.That(response.DeveloperId, Is.EqualTo("dev-1"));
      Assert.That(response.LeaseTtlSeconds, Is.EqualTo(120));
      Assert.That(response.ReservationTtlSeconds, Is.EqualTo(1800));
      Assert.That(CoordinationUserSettings.ToJson(CoordinationUserSettings.CreateDefault()),
        Does.Not.Contain("session-secret"));
    }

    [Test]
    public async Task InvalidDeveloperTokenChangesTheServiceToAuthenticationFailed()
    {
      var service = CreateService(Credentials(), new FakeHttpClient(401));

      await service.ConnectAsync();

      Assert.That(service.State, Is.EqualTo(CoordinationConnectionState.AuthenticationFailed));
      Assert.That(service.HasSession, Is.False);
      await service.ShutdownAsync();
    }

    [Test]
    public async Task UnsupportedPlatformRemainsDisabledWithoutCallingTheServer()
    {
      var http = new FakeHttpClient();
      var service = CreateService(new MemoryCredentialStore(), http, isSupportedPlatform: false);

      await service.ConnectAsync();

      Assert.That(service.State, Is.EqualTo(CoordinationConnectionState.Disabled));
      Assert.That(http.Calls, Is.EqualTo(0));
      await service.ShutdownAsync();
    }

    [Test]
    public async Task ShutdownCancelsAnInFlightSessionBeforeClosingTheSocket()
    {
      var http = new BlockingHttpClient();
      var socket = new FakeWebSocketClient();
      var service = CreateService(Credentials(), http, socket);

      var connection = service.ConnectAsync();
      await http.Started.Task;
      await service.ShutdownAsync();

      Assert.That(http.WasCancelled, Is.True);
      Assert.That(socket.CloseCalls, Is.EqualTo(1));
      Assert.That(connection.IsCompleted, Is.True);
    }

    [Test]
    public async Task MissingCredentialPromptsOnceWithoutCallingTheServer()
    {
      var prompts = 0;
      var http = new FakeHttpClient();
      var service = CreateService(new MemoryCredentialStore(), http,
        requestCredentials: _ => prompts++);

      await service.ConnectAsync();
      await service.ConnectAsync();

      Assert.That(service.State, Is.EqualTo(CoordinationConnectionState.Offline));
      Assert.That(prompts, Is.EqualTo(1));
      Assert.That(http.Calls, Is.EqualTo(0));
      await service.ShutdownAsync();
    }

    [Test]
    public void CredentialWindowSubmissionWritesOnlyTheCredentialStore()
    {
      var store = new MemoryCredentialStore();
      var target = CoordinationCredentialStore.GetDeveloperTokenTarget("potion-panic");
      var callbacks = 0;

      Assert.That(CoordinationCredentialWindow.TrySubmitToken(store, target, "developer-token",
        () => callbacks++), Is.True);
      Assert.That(callbacks, Is.EqualTo(1));
      Assert.That(store.TryRead(target, out var token), Is.True);
      Assert.That(token, Is.EqualTo("developer-token"));
      Assert.That(CoordinationUserSettings.ToJson(CoordinationUserSettings.CreateDefault()),
        Does.Not.Contain("developer-token"));
    }

    [Test]
    public async Task SocketMessagesAreDispatchedOnTheMainThread()
    {
      var dispatcher = new QueuedMainThreadDispatcher();
      var socket = new FakeWebSocketClient();
      var service = CreateService(Credentials(), new FakeHttpClient(), socket,
        dispatcher: dispatcher, delay: new ControlledDelay());
      var readyCount = 0;
      service.SessionReady += _ => readyCount++;

      await service.ConnectAsync();
      RaiseReady(socket, 1);

      Assert.That(readyCount, Is.EqualTo(0));
      dispatcher.ExecutePending();
      Assert.That(readyCount, Is.EqualTo(1));
      Assert.That(service.State, Is.EqualTo(CoordinationConnectionState.Connected));
      await service.ShutdownAsync();
    }

    [Test]
    public async Task OfflineServiceDoesNotQueueLeaseMutations()
    {
      var socket = new FakeWebSocketClient();
      var service = CreateService(Credentials(), new FakeHttpClient(503), socket,
        delay: new ControlledDelay());

      await service.ConnectAsync();

      Assert.That(service.TryAcquireLease("Assets/Scenes/SampleScene.unity", out _), Is.False);
      Assert.That(socket.SentMessages, Is.Empty);
      await service.ShutdownAsync();
    }

    [Test]
    public async Task PresenceRemovalIsRaisedOnTheMainThread()
    {
      var dispatcher = new QueuedMainThreadDispatcher();
      var socket = new FakeWebSocketClient();
      var service = CreateService(Credentials(), new FakeHttpClient(), socket,
        dispatcher: dispatcher);
      CoordinationServerEnvelope removed = null;
      service.PresenceRemoved += envelope => removed = envelope;

      await service.ConnectAsync();
      socket.RaiseMessage("{\"protocolVersion\":1,\"type\":\"presence.removed\",\"stateVersion\":1,"
        + "\"path\":\"Assets/Scenes/SampleScene.unity\",\"connectionId\":\"connection-1\"}");

      Assert.That(removed, Is.Null);
      dispatcher.ExecutePending();
      Assert.That(removed.path, Is.EqualTo("Assets/Scenes/SampleScene.unity"));
      await service.ShutdownAsync();
    }

    [Test]
    public async Task PresenceUpdatePublishesItsAuthoritativeEnvelope()
    {
      var socket = new FakeWebSocketClient();
      var service = CreateService(Credentials(), new FakeHttpClient(), socket);
      object received = null;
      service.PresenceReceived += value => received = value;

      await service.ConnectAsync();
      RaiseReady(socket, 1);
      socket.RaiseMessage("{\"protocolVersion\":1,\"type\":\"presence.updated\","
        + "\"stateVersion\":2,\"presence\":[]}");

      Assert.That(received, Is.TypeOf<CoordinationServerEnvelope>());
      Assert.That(((CoordinationServerEnvelope)received).stateVersion, Is.EqualTo(2));
      await service.ShutdownAsync();
    }

    [Test]
    public async Task SessionReadyStartsHeartbeatsWithoutRequestingAnotherSnapshot()
    {
      var credentials = Credentials();
      var delay = new ControlledDelay();
      var socket = new FakeWebSocketClient();
      var service = CreateService(credentials, new FakeHttpClient(), socket, delay: delay);

      await service.ConnectAsync();
      RaiseReady(socket, 1);

      Assert.That(socket.SentMessages, Is.Empty);
      Assert.That(delay.Delays, Is.EqualTo(new[] { TimeSpan.FromSeconds(30) }));
      delay.CompleteNext();
      await Task.Yield();
      Assert.That(socket.SentMessages[0], Does.Contain("\"type\":\"heartbeat\""));
      await service.ShutdownAsync();
    }

    [Test]
    public async Task SessionExpiryImmediatelyCreatesANewSession()
    {
      var socket = new FakeWebSocketClient();
      var service = CreateService(Credentials(), new FakeHttpClient(), socket,
        delay: new ImmediateDelay());

      await service.ConnectAsync();
      socket.RaiseClosed(4001, "Session expired.");

      Assert.That(socket.ConnectCalls, Is.EqualTo(2));
      await service.ShutdownAsync();
    }

    [Test]
    public async Task NetworkReconnectUsesBoundedBackoffAndOneAttemptAtATime()
    {
      var delay = new ControlledDelay();
      var http = new FakeHttpClient(503, 503, 201);
      var socket = new FakeWebSocketClient();
      var service = CreateService(Credentials(), http, socket, delay: delay);

      await service.ConnectAsync();
      Assert.That(delay.Delays, Is.EqualTo(new[] { TimeSpan.FromSeconds(1) }));
      delay.CompleteNext();
      await Task.Yield();
      Assert.That(delay.Delays, Is.EqualTo(new[] {
        TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)
      }));
      delay.CompleteNext();
      await Task.Yield();

      Assert.That(http.Calls, Is.EqualTo(3));
      Assert.That(socket.ConnectCalls, Is.EqualTo(1));
      await service.ShutdownAsync();
    }

    [Test]
    public async Task RevocationDoesNotRetryAndExplicitShutdownCancelsTheHeartbeat()
    {
      var delay = new ControlledDelay();
      var socket = new FakeWebSocketClient();
      var service = CreateService(Credentials(), new FakeHttpClient(), socket, delay: delay);
      var revoked = 0;
      service.Revoked += () => revoked++;

      await service.ConnectAsync();
      RaiseReady(socket, 1);
      socket.RaiseClosed(4003, "Developer access revoked.");

      Assert.That(service.State, Is.EqualTo(CoordinationConnectionState.AuthenticationFailed));
      Assert.That(revoked, Is.EqualTo(1));
      Assert.That(socket.ConnectCalls, Is.EqualTo(1));
      await service.ShutdownAsync();
      Assert.That(socket.CloseCalls, Is.EqualTo(1));
      Assert.That(delay.Cancellations, Is.GreaterThan(0));
    }

    [Test]
    public async Task MutationExposesNormalizedRequestAndReportsAStaleReplayWithoutApplyingIt()
    {
      var socket = new FakeWebSocketClient();
      var service = CreateService(Credentials(), new FakeHttpClient(), socket);
      CoordinationRequestCompletion completion = null;
      var leaseResults = 0;
      service.RequestCompleted += value => completion = value;
      service.LeaseResultReceived += _ => leaseResults++;

      await service.ConnectAsync();
      RaiseReady(socket, 5);
      Assert.That(service.TryAcquireLease("Assets\\Scenes\\SampleScene.unity", out var request), Is.True);
      Assert.That(request.NormalizedPath, Is.EqualTo("Assets/Scenes/SampleScene.unity"));

      socket.RaiseMessage("{\"protocolVersion\":1,\"type\":\"lease.denied\",\"stateVersion\":4,"
        + "\"requestId\":\"" + request.RequestId + "\",\"path\":\"Assets/Scenes/SampleScene.unity\","
        + "\"code\":\"lease_unavailable\",\"currentLease\":null}");

      Assert.That(completion.Request, Is.SameAs(request));
      Assert.That(completion.IsStaleReplay, Is.True);
      Assert.That(leaseResults, Is.EqualTo(0));
      await service.ShutdownAsync();
    }

    [Test]
    public async Task SendFailureRaisesTheMatchingRequestFailure()
    {
      var socket = new FakeWebSocketClient { SendException = new InvalidOperationException("write failed") };
      var service = CreateService(Credentials(), new FakeHttpClient(), socket,
        delay: new ControlledDelay());
      CoordinationRequestSendFailure failure = null;
      service.RequestSendFailed += value => failure = value;

      await service.ConnectAsync();
      RaiseReady(socket, 1);
      Assert.That(service.TryOpenPresence("Assets/Scenes/SampleScene.unity", out var request), Is.True);

      Assert.That(failure.Request, Is.SameAs(request));
      Assert.That(failure.Message, Is.EqualTo("write failed"));
      await service.ShutdownAsync();
    }

    [Test]
    public async Task SocketCloseDrainsPendingRequestsExactlyOnce()
    {
      var dispatcher = new QueuedMainThreadDispatcher();
      var socket = new FakeWebSocketClient();
      var service = CreateService(Credentials(), new FakeHttpClient(), socket,
        dispatcher: dispatcher);
      var failures = new List<CoordinationRequestSendFailure>();
      var completions = 0;
      service.RequestSendFailed += failures.Add;
      service.RequestCompleted += _ => completions++;

      await service.ConnectAsync();
      RaiseReady(socket, 1);
      dispatcher.ExecutePending();
      Assert.That(service.TryOpenPresence("Assets/Scenes/One.unity", out _), Is.True);
      Assert.That(service.TryOpenPresence("Assets/Scenes/Two.unity", out _), Is.True);

      socket.RaiseClosed(1006, "closed");
      socket.RaiseClosed(1006, "closed");
      dispatcher.ExecutePending();

      Assert.That(failures, Has.Count.EqualTo(2));
      Assert.That(failures, Has.All.Matches<CoordinationRequestSendFailure>(
        value => value.Message == "The coordination socket closed."));
      Assert.That(completions, Is.Zero);
      await service.ShutdownAsync();
    }

    [Test]
    public async Task SavedCredentialStartsOneConnectionAttempt()
    {
      var credentials = new MemoryCredentialStore();
      var http = new FakeHttpClient();
      var socket = new FakeWebSocketClient();
      Action saved = null;
      var service = CreateService(credentials, http, socket,
        requestCredentials: callback => saved = callback);

      await service.ConnectAsync();
      Assert.That(saved, Is.Not.Null);
      credentials.Write(CoordinationCredentialStore.GetDeveloperTokenTarget("potion-panic"),
        "developer-token");
      saved();
      await Task.Yield();

      Assert.That(http.Calls, Is.EqualTo(1));
      Assert.That(socket.ConnectCalls, Is.EqualTo(1));
      await service.ShutdownAsync();
    }

    [Test]
    public async Task CredentialReadFailureIsReportedWithoutPromptingForCredentials()
    {
      var prompts = 0;
      var errors = new List<CoordinationServerEnvelope>();
      var service = CreateService(new ThrowingCredentialStore(), new FakeHttpClient(),
        requestCredentials: _ => prompts++);
      service.ErrorReceived += errors.Add;

      await service.ConnectAsync();

      Assert.That(prompts, Is.EqualTo(0));
      Assert.That(errors, Has.Exactly(1).Matches<CoordinationServerEnvelope>(
        value => value.code == "credential_read_failed"));
      await service.ShutdownAsync();
    }

    [Test]
    public async Task ForgettingCredentialsCancelsReconnectAndClosesTheSocket()
    {
      var socket = new FakeWebSocketClient();
      var service = CreateService(Credentials(), new FakeHttpClient(), socket,
        delay: new ControlledDelay());

      await service.ConnectAsync();
      await service.ForgetCredentialsAsync();
      socket.RaiseClosed(1006, "network");

      Assert.That(socket.CloseCalls, Is.EqualTo(1));
      Assert.That(socket.ConnectCalls, Is.EqualTo(1));
      await service.ShutdownAsync();
    }

    [Test]
    public async Task CorrelatedSnapshotAppliesAtomicallyAndCompletesOnlyOnce()
    {
      var socket = new FakeWebSocketClient();
      var service = CreateService(Credentials(), new FakeHttpClient(), socket);
      var snapshots = 0;
      var completions = 0;
      var presenceUpdates = 0;
      CoordinationServerEnvelope received = null;
      service.SnapshotReceived += envelope =>
      {
        snapshots++;
        received = envelope;
      };
      service.PresenceReceived += _ => presenceUpdates++;
      service.RequestCompleted += _ => completions++;

      await service.ConnectAsync();
      RaiseReady(socket, 5);
      Assert.That(service.TryRequestSnapshot(out var request), Is.True);

      var second = SnapshotChunkJson(SnapshotId, 1, 2, 8, request.RequestId);
      var first = SnapshotChunkJson(SnapshotId, 0, 2, 8, request.RequestId);
      socket.RaiseMessage(second);

      Assert.That(snapshots, Is.Zero);
      Assert.That(completions, Is.Zero);
      socket.RaiseMessage(PresenceUpdateJson(6));
      Assert.That(presenceUpdates, Is.EqualTo(1));

      socket.RaiseMessage(first);

      Assert.That(snapshots, Is.EqualTo(1));
      Assert.That(completions, Is.EqualTo(1));
      Assert.That(received.presence, Has.Length.EqualTo(2));
      Assert.That(received.presence[0].path, Is.EqualTo("assets/chunk-0.asset"));
      Assert.That(received.presence[1].path, Is.EqualTo("assets/chunk-1.asset"));
      socket.RaiseMessage(PresenceUpdateJson(7));
      Assert.That(presenceUpdates, Is.EqualTo(1));

      socket.RaiseMessage(second);
      socket.RaiseMessage(first);

      Assert.That(completions, Is.EqualTo(1));
      await service.ShutdownAsync();
    }

    [Test]
    public async Task IncompleteSnapshotDoesNotPublishApplyOrComplete()
    {
      var socket = new FakeWebSocketClient();
      var service = CreateService(Credentials(), new FakeHttpClient(), socket);
      var snapshots = 0;
      var completions = 0;
      var presenceUpdates = 0;
      service.SnapshotReceived += _ => snapshots++;
      service.RequestCompleted += _ => completions++;
      service.PresenceReceived += _ => presenceUpdates++;

      await service.ConnectAsync();
      RaiseReady(socket, 5);
      Assert.That(service.TryRequestSnapshot(out var request), Is.True);

      socket.RaiseMessage(SnapshotChunkJson(SnapshotId, 0, 2, 8, request.RequestId));

      Assert.That(snapshots, Is.Zero);
      Assert.That(completions, Is.Zero);
      socket.RaiseMessage(PresenceUpdateJson(6));
      Assert.That(presenceUpdates, Is.EqualTo(1));
      await service.ShutdownAsync();
    }

    [Test]
    public async Task StaleCompletedSnapshotCompletesOnceWithoutPublishingState()
    {
      var socket = new FakeWebSocketClient();
      var service = CreateService(Credentials(), new FakeHttpClient(), socket);
      var snapshots = 0;
      var presenceUpdates = 0;
      var completions = new List<CoordinationRequestCompletion>();
      service.SnapshotReceived += _ => snapshots++;
      service.PresenceReceived += _ => presenceUpdates++;
      service.RequestCompleted += completions.Add;

      await service.ConnectAsync();
      RaiseReady(socket, 8);
      Assert.That(service.TryRequestSnapshot(out var request), Is.True);
      var first = SnapshotChunkJson(SnapshotId, 0, 2, 7, request.RequestId);
      var second = SnapshotChunkJson(SnapshotId, 1, 2, 7, request.RequestId);

      socket.RaiseMessage(second);
      socket.RaiseMessage(first);

      Assert.That(snapshots, Is.Zero);
      Assert.That(completions, Has.Count.EqualTo(1));
      Assert.That(completions[0].IsStaleReplay, Is.True);
      socket.RaiseMessage(PresenceUpdateJson(7));
      Assert.That(presenceUpdates, Is.Zero);

      socket.RaiseMessage(second);
      socket.RaiseMessage(first);

      Assert.That(completions, Has.Count.EqualTo(1));
      await service.ShutdownAsync();
    }

    [Test]
    public async Task UnrelatedStateMessageDoesNotDiscardAPartialSnapshot()
    {
      var socket = new FakeWebSocketClient();
      var service = CreateService(Credentials(), new FakeHttpClient(), socket);
      var snapshots = 0;
      var presenceUpdates = 0;
      service.SnapshotReceived += _ => snapshots++;
      service.PresenceReceived += _ => presenceUpdates++;

      await service.ConnectAsync();
      RaiseReady(socket, 5);
      socket.RaiseMessage(SnapshotChunkJson(SnapshotId, 0, 2, 8, null));
      socket.RaiseMessage(PresenceUpdateJson(6));
      socket.RaiseMessage(SnapshotChunkJson(SnapshotId, 1, 2, 8, null));

      Assert.That(snapshots, Is.EqualTo(1));
      Assert.That(presenceUpdates, Is.EqualTo(1));
      socket.RaiseMessage(PresenceUpdateJson(7));
      Assert.That(presenceUpdates, Is.EqualTo(1));
      await service.ShutdownAsync();
    }

    [Test]
    public async Task RejectedSnapshotPublishesTheAssemblerErrorWithoutApplyingState()
    {
      var socket = new FakeWebSocketClient();
      var service = CreateService(Credentials(), new FakeHttpClient(), socket);
      var errors = new List<CoordinationServerEnvelope>();
      var snapshots = 0;
      var presenceUpdates = 0;
      service.ErrorReceived += errors.Add;
      service.SnapshotReceived += _ => snapshots++;
      service.PresenceReceived += _ => presenceUpdates++;

      await service.ConnectAsync();
      RaiseReady(socket, 5);
      socket.RaiseMessage(SnapshotChunkJson(SnapshotId, 0, 2, 8, null, "first"));
      socket.RaiseMessage(SnapshotChunkJson(SnapshotId, 0, 2, 8, null, "conflict"));

      Assert.That(errors, Has.Exactly(1).Matches<CoordinationServerEnvelope>(
        envelope => envelope.code == "snapshot_duplicate_inconsistent"));
      Assert.That(snapshots, Is.Zero);
      socket.RaiseMessage(PresenceUpdateJson(6));
      Assert.That(presenceUpdates, Is.EqualTo(1));
      await service.ShutdownAsync();
    }

    [Test]
    public async Task SocketCloseDiscardsAPartialSnapshot()
    {
      var socket = new FakeWebSocketClient();
      var service = CreateService(Credentials(), new FakeHttpClient(), socket);
      var snapshots = 0;
      service.SnapshotReceived += _ => snapshots++;

      await service.ConnectAsync();
      RaiseReady(socket, 5);
      socket.RaiseMessage(SnapshotChunkJson(SnapshotId, 0, 2, 8, null));
      socket.RaiseClosed(4003, "revoked");
      socket.RaiseMessage(SnapshotChunkJson(SnapshotId, 1, 2, 8, null));

      Assert.That(snapshots, Is.Zero);
      await service.ShutdownAsync();
    }

    [Test]
    public async Task CredentialRemovalDiscardsAPartialSnapshot()
    {
      var socket = new FakeWebSocketClient();
      var service = CreateService(Credentials(), new FakeHttpClient(), socket);
      var snapshots = 0;
      service.SnapshotReceived += _ => snapshots++;

      await service.ConnectAsync();
      RaiseReady(socket, 5);
      socket.RaiseMessage(SnapshotChunkJson(SnapshotId, 0, 2, 8, null));
      await service.ForgetCredentialsAsync();
      socket.RaiseMessage(SnapshotChunkJson(SnapshotId, 1, 2, 8, null));

      Assert.That(snapshots, Is.Zero);
      await service.ShutdownAsync();
    }

    [Test]
    public async Task DisablingTheServiceDiscardsAPartialSnapshot()
    {
      var socket = new FakeWebSocketClient();
      var service = CreateService(Credentials(), new FakeHttpClient(), socket);
      var snapshots = 0;
      service.SnapshotReceived += _ => snapshots++;

      await service.ConnectAsync();
      RaiseReady(socket, 5);
      socket.RaiseMessage(SnapshotChunkJson(SnapshotId, 0, 2, 8, null));
      await service.SetDisabledAsync(true);
      socket.RaiseMessage(SnapshotChunkJson(SnapshotId, 1, 2, 8, null));

      Assert.That(snapshots, Is.Zero);
      await service.ShutdownAsync();
    }

    [Test]
    public async Task ShutdownDiscardsAPartialSnapshotBeforeQueuedMessagesApply()
    {
      var dispatcher = new QueuedMainThreadDispatcher();
      var socket = new FakeWebSocketClient();
      var service = CreateService(Credentials(), new FakeHttpClient(), socket,
        dispatcher: dispatcher);
      var snapshots = 0;
      service.SnapshotReceived += _ => snapshots++;

      await service.ConnectAsync();
      RaiseReady(socket, 5);
      dispatcher.ExecutePending();
      socket.RaiseMessage(SnapshotChunkJson(SnapshotId, 0, 2, 8, null));
      dispatcher.ExecutePending();
      socket.RaiseMessage(SnapshotChunkJson(SnapshotId, 1, 2, 8, null));

      await service.ShutdownAsync();
      dispatcher.ExecutePending();

      Assert.That(snapshots, Is.Zero);
    }

    [TestCase("socket-close")]
    [TestCase("credential-removal")]
    [TestCase("disable")]
    [TestCase("shutdown")]
    public async Task LifecycleInvalidationDropsAllQueuedSnapshotChunks(string transition)
    {
      var dispatcher = new QueuedMainThreadDispatcher();
      var socket = new FakeWebSocketClient();
      var service = CreateService(Credentials(), new FakeHttpClient(), socket,
        dispatcher: dispatcher);
      var snapshots = 0;
      var completions = 0;
      service.SnapshotReceived += _ => snapshots++;
      service.RequestCompleted += _ => completions++;

      await service.ConnectAsync();
      RaiseReady(socket, 5);
      dispatcher.ExecutePending();
      Assert.That(service.TryRequestSnapshot(out var request), Is.True);
      socket.RaiseMessage(SnapshotChunkJson(SnapshotId, 0, 2, 8, request.RequestId));
      socket.RaiseMessage(SnapshotChunkJson(SnapshotId, 1, 2, 8, request.RequestId));

      switch (transition)
      {
        case "socket-close":
          socket.RaiseClosed(4003, "revoked");
          break;
        case "credential-removal":
          await service.ForgetCredentialsAsync();
          break;
        case "disable":
          await service.SetDisabledAsync(true);
          break;
        case "shutdown":
          await service.ShutdownAsync();
          break;
      }
      dispatcher.ExecutePending();

      Assert.That(snapshots, Is.Zero, transition);
      Assert.That(completions, Is.Zero, transition);
      await service.ShutdownAsync();
    }

    [Test]
    public async Task CurrentGenerationProcessesSnapshotsAfterReconnect()
    {
      var dispatcher = new QueuedMainThreadDispatcher();
      var socket = new FakeWebSocketClient();
      var service = CreateService(Credentials(), new FakeHttpClient(), socket,
        dispatcher: dispatcher);
      var snapshots = 0;
      var completions = new List<CoordinationRequestCompletion>();
      service.SnapshotReceived += _ => snapshots++;
      service.RequestCompleted += completions.Add;

      await service.ConnectAsync();
      RaiseReady(socket, 5);
      dispatcher.ExecutePending();
      Assert.That(service.TryRequestSnapshot(out var staleRequest), Is.True);
      socket.RaiseMessage(SnapshotChunkJson(
        SnapshotId, 0, 2, 8, staleRequest.RequestId));
      socket.RaiseMessage(SnapshotChunkJson(
        SnapshotId, 1, 2, 8, staleRequest.RequestId));
      socket.RaiseClosed(4001, "expired");
      dispatcher.ExecutePending();
      await Task.Yield();

      Assert.That(snapshots, Is.Zero);
      Assert.That(completions, Is.Empty);
      RaiseReady(socket, 9);
      dispatcher.ExecutePending();
      Assert.That(service.TryRequestSnapshot(out var currentRequest), Is.True);
      socket.RaiseMessage(SnapshotChunkJson(
        OtherSnapshotId, 1, 2, 10, currentRequest.RequestId));
      socket.RaiseMessage(SnapshotChunkJson(
        OtherSnapshotId, 0, 2, 10, currentRequest.RequestId));
      dispatcher.ExecutePending();

      Assert.That(snapshots, Is.EqualTo(1));
      Assert.That(completions, Has.Count.EqualTo(1));
      Assert.That(completions[0].Request, Is.SameAs(currentRequest));
      await service.ShutdownAsync();
    }

    [TestCase("credential-removal")]
    [TestCase("disable")]
    [TestCase("shutdown")]
    public async Task MessagesReceivedWhileLifecycleCloseIsPendingAreDropped(
      string transition)
    {
      var dispatcher = new QueuedMainThreadDispatcher();
      var socket = new FakeWebSocketClient { HoldClose = true };
      var service = CreateService(Credentials(), new FakeHttpClient(), socket,
        dispatcher: dispatcher);
      var snapshots = 0;
      var completions = 0;
      service.SnapshotReceived += _ => snapshots++;
      service.RequestCompleted += _ => completions++;

      await service.ConnectAsync();
      RaiseReady(socket, 5);
      dispatcher.ExecutePending();
      Assert.That(service.TryRequestSnapshot(out var request), Is.True);
      QueueSnapshot(socket, SnapshotId, 8, request.RequestId);

      Task transitionTask;
      switch (transition)
      {
        case "credential-removal":
          transitionTask = service.ForgetCredentialsAsync();
          break;
        case "disable":
          transitionTask = service.SetDisabledAsync(true);
          break;
        default:
          transitionTask = service.ShutdownAsync();
          break;
      }
      await socket.CloseStarted.Task;

      QueueSnapshot(socket, OtherSnapshotId, 9, request.RequestId);
      dispatcher.ExecutePending();

      Assert.That(snapshots, Is.Zero, transition);
      Assert.That(completions, Is.Zero, transition);
      socket.ReleaseClose();
      await transitionTask;
      await service.ShutdownAsync();
    }

    [Test]
    public async Task MessagesReceivedAfterSpontaneousCloseAreDroppedBeforeReconnect()
    {
      var dispatcher = new QueuedMainThreadDispatcher();
      var socket = new FakeWebSocketClient();
      var service = CreateService(Credentials(), new FakeHttpClient(), socket,
        dispatcher: dispatcher);
      var snapshots = 0;
      var completions = 0;
      service.SnapshotReceived += _ => snapshots++;
      service.RequestCompleted += _ => completions++;

      await service.ConnectAsync();
      RaiseReady(socket, 5);
      dispatcher.ExecutePending();
      Assert.That(service.TryRequestSnapshot(out var request), Is.True);
      QueueSnapshot(socket, SnapshotId, 8, request.RequestId);
      socket.RaiseClosed(4003, "revoked");
      QueueSnapshot(socket, OtherSnapshotId, 9, request.RequestId);

      dispatcher.ExecutePending();

      Assert.That(snapshots, Is.Zero);
      Assert.That(completions, Is.Zero);
      await service.ShutdownAsync();
    }

    [Test]
    public async Task FailedConnectionDisablesSocketMessageAcceptance()
    {
      var dispatcher = new QueuedMainThreadDispatcher();
      var socket = new FakeWebSocketClient
      {
        ConnectException = new InvalidOperationException("connect failed")
      };
      var service = CreateService(Credentials(), new FakeHttpClient(), socket,
        dispatcher: dispatcher, delay: new ControlledDelay());
      var snapshots = 0;
      service.SnapshotReceived += _ => snapshots++;

      await service.ConnectAsync();
      socket.RaiseMessage(SnapshotChunkJson(SnapshotId, 0, 1, 8, null));
      dispatcher.ExecutePending();

      Assert.That(snapshots, Is.Zero);
      await service.ShutdownAsync();
    }

    [Test]
    public async Task ReplacementConnectionRejectsMessagesUntilPriorCloseCompletes()
    {
      var dispatcher = new QueuedMainThreadDispatcher();
      var socket = new FakeWebSocketClient();
      var service = CreateService(Credentials(), new FakeHttpClient(), socket,
        dispatcher: dispatcher);
      var snapshots = 0;
      var completions = new List<CoordinationRequestCompletion>();
      service.SnapshotReceived += _ => snapshots++;
      service.RequestCompleted += completions.Add;

      await service.ConnectAsync();
      RaiseReady(socket, 5);
      dispatcher.ExecutePending();
      Assert.That(service.TryRequestSnapshot(out var oldRequest), Is.True);
      QueueSnapshot(socket, SnapshotId, 8, oldRequest.RequestId);
      socket.HoldClose = true;

      var reconnect = service.ConnectAsync();
      await socket.CloseStarted.Task;
      QueueSnapshot(socket, OtherSnapshotId, 9, oldRequest.RequestId);
      dispatcher.ExecutePending();

      Assert.That(snapshots, Is.Zero);
      Assert.That(completions, Is.Empty);
      socket.ReleaseClose();
      await reconnect;
      RaiseReady(socket, 9);
      dispatcher.ExecutePending();
      Assert.That(service.TryRequestSnapshot(out var currentRequest), Is.True);
      QueueSnapshot(socket, OtherSnapshotId, 10, currentRequest.RequestId);
      dispatcher.ExecutePending();

      Assert.That(snapshots, Is.EqualTo(1));
      Assert.That(completions, Has.Count.EqualTo(1));
      Assert.That(completions[0].Request, Is.SameAs(currentRequest));
      await service.ShutdownAsync();
    }

    private static MemoryCredentialStore Credentials()
    {
      var credentials = new MemoryCredentialStore();
      credentials.Write(CoordinationCredentialStore.GetDeveloperTokenTarget("potion-panic"),
        "developer-token");
      return credentials;
    }

    private static string SnapshotChunkJson(
      string snapshotId,
      int chunkIndex,
      int chunkCount,
      long stateVersion,
      string requestId,
      string marker = null)
    {
      var suffix = marker ?? chunkIndex.ToString();
      var requestField = requestId == null
        ? string.Empty
        : ",\"requestId\":\"" + requestId + "\"";
      return "{\"protocolVersion\":1,\"type\":\"snapshot\",\"snapshotId\":\""
        + snapshotId + "\",\"chunkIndex\":" + chunkIndex + ",\"chunkCount\":"
        + chunkCount + ",\"stateVersion\":" + stateVersion + requestField
        + ",\"serverTime\":\"2026-08-08T00:00:00Z\",\"presence\":[{"
        + "\"path\":\"assets/chunk-"
        + suffix + ".asset\",\"displayPath\":\"Assets/Chunk-" + suffix
        + ".asset\",\"developerId\":\"dev-1\",\"displayName\":\"Rin\","
        + "\"connectionId\":\"connection-1\",\"branch\":\"feature/chunks\","
        + "\"task\":\"PP-7\",\"expiresAt\":\"2026-08-08T00:02:00Z\"}],\"leases\":[]}";
    }

    private static string PresenceUpdateJson(long stateVersion)
    {
      return "{\"protocolVersion\":1,\"type\":\"presence.updated\",\"stateVersion\":"
        + stateVersion + ",\"presence\":[]}";
    }

    private static void QueueSnapshot(
      FakeWebSocketClient socket,
      string snapshotId,
      long stateVersion,
      string requestId)
    {
      socket.RaiseMessage(SnapshotChunkJson(snapshotId, 0, 2, stateVersion, requestId));
      socket.RaiseMessage(SnapshotChunkJson(snapshotId, 1, 2, stateVersion, requestId));
    }

    private static void RaiseReady(FakeWebSocketClient socket, long stateVersion)
    {
      socket.RaiseMessage("{\"protocolVersion\":1,\"type\":\"session.ready\",\"stateVersion\":"
        + stateVersion + ",\"developerId\":\"dev-1\",\"displayName\":\"Rin\","
        + "\"serverTime\":\"2026-08-07T12:00:00Z\",\"connectionId\":\"connection-1\","
        + "\"leaseTtlSeconds\":120,\"reservationTtlSeconds\":1800}");
    }

    private static CoordinationService CreateService(
      ICredentialStore credentials,
      ICoordinationHttpClient http,
      FakeWebSocketClient socket = null,
      Action<Action> requestCredentials = null,
      ICoordinationDelay delay = null,
      IMainThreadDispatcher dispatcher = null,
      bool isSupportedPlatform = true)
    {
      return new CoordinationService(Configuration, CoordinationUserSettings.CreateDefault(),
        credentials, http, socket ?? new FakeWebSocketClient(),
        dispatcher ?? new ImmediateMainThreadDispatcher(), new FixedGitContext("feature/coordination-05"),
        isSupportedPlatform, requestCredentials, delay,
        value => value);
    }

    private sealed class FakeHttpClient : ICoordinationHttpClient
    {
      private readonly Queue<int> statuses = new Queue<int>();
      public int Calls { get; private set; }

      public FakeHttpClient(params int[] statuses)
      {
        foreach (var status in statuses)
        {
          this.statuses.Enqueue(status);
        }
      }

      public Task<CoordinationHttpResponse> CreateSessionAsync(
        Uri uri,
        string developerToken,
        CancellationToken cancellationToken)
      {
        Calls++;
        var status = statuses.Count == 0 ? 201 : statuses.Dequeue();
        var body = status == 201
          ? "{\"developerId\":\"dev-1\",\"displayName\":\"Rin\",\"sessionToken\":\"session-"
            + Calls + "\",\"serverTime\":\"2026-08-07T12:00:00Z\",\"leaseTtlSeconds\":120,"
            + "\"reservationTtlSeconds\":1800,\"stateVersion\":0}"
          : "Unavailable";
        return Task.FromResult(new CoordinationHttpResponse(status, body));
      }
    }

    private sealed class BlockingHttpClient : ICoordinationHttpClient
    {
      public TaskCompletionSource<bool> Started { get; }
        = new TaskCompletionSource<bool>();
      public bool WasCancelled { get; private set; }

      public async Task<CoordinationHttpResponse> CreateSessionAsync(
        Uri uri,
        string developerToken,
        CancellationToken cancellationToken)
      {
        Started.TrySetResult(true);
        try
        {
          await Task.Delay(Timeout.Infinite, cancellationToken);
          throw new InvalidOperationException("The blocking request unexpectedly completed.");
        }
        catch (OperationCanceledException)
        {
          WasCancelled = true;
          throw;
        }
      }
    }

    private sealed class FakeWebSocketClient : ICoordinationWebSocketClient
    {
      public event Action<string> MessageReceived;
      public event Action<int, string> Closed;
      public List<string> SentMessages { get; } = new List<string>();
      public int ConnectCalls { get; private set; }
      public int CloseCalls { get; private set; }
      public Exception SendException;
      public Exception ConnectException;
      public bool HoldClose;
      public TaskCompletionSource<bool> CloseStarted { get; }
        = new TaskCompletionSource<bool>();
      private readonly TaskCompletionSource<bool> closeCompletion
        = new TaskCompletionSource<bool>();

      public async Task ConnectAsync(
        Uri uri,
        string sessionToken,
        CancellationToken cancellationToken)
      {
        ConnectCalls++;
        if (IsConnected)
        {
          await CloseAsync(cancellationToken);
        }

        if (ConnectException != null)
        {
          throw ConnectException;
        }

        IsConnected = true;
      }

      public Task SendAsync(string message, CancellationToken cancellationToken)
      {
        if (SendException != null)
        {
          return Task.FromException(SendException);
        }
        SentMessages.Add(message);
        return Task.CompletedTask;
      }

      public Task CloseAsync(CancellationToken cancellationToken)
      {
        IsConnected = false;
        CloseCalls++;
        CloseStarted.TrySetResult(true);
        return HoldClose ? closeCompletion.Task : Task.CompletedTask;
      }

      public void ReleaseClose()
      {
        HoldClose = false;
        closeCompletion.TrySetResult(true);
      }

      public void RaiseMessage(string message) => MessageReceived?.Invoke(message);
      public void RaiseClosed(int statusCode, string reason)
      {
        IsConnected = false;
        Closed?.Invoke(statusCode, reason);
      }

      private bool IsConnected { get; set; }
    }

    private sealed class ControlledDelay : ICoordinationDelay
    {
      private readonly Queue<TaskCompletionSource<bool>> pending = new Queue<TaskCompletionSource<bool>>();
      public List<TimeSpan> Delays { get; } = new List<TimeSpan>();
      public int Cancellations { get; private set; }

      public Task DelayAsync(TimeSpan value, CancellationToken cancellationToken)
      {
        Delays.Add(value);
        var completion = new TaskCompletionSource<bool>();
        cancellationToken.Register(() =>
        {
          Cancellations++;
          completion.TrySetCanceled();
        });
        pending.Enqueue(completion);
        return completion.Task;
      }

      public void CompleteNext() => pending.Dequeue().TrySetResult(true);
    }

    private sealed class ImmediateDelay : ICoordinationDelay
    {
      public Task DelayAsync(TimeSpan value, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class ThrowingCredentialStore : ICredentialStore
    {
      public bool TryRead(string target, out string value)
      {
        value = null;
        throw new InvalidOperationException("Credential Manager is unavailable.");
      }

      public void Write(string target, string value) { }
      public void Delete(string target) { }
    }

    private sealed class FixedGitContext : ICoordinationGitContext
    {
      private readonly string branch;
      public FixedGitContext(string branch) => this.branch = branch;
      public string GetBranch() => branch;
    }
  }
}
