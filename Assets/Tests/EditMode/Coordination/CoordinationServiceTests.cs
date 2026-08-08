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
    public async Task MissingCredentialPromptsOnceWithoutCallingTheServer()
    {
      var prompts = 0;
      var http = new FakeHttpClient();
      var service = CreateService(new MemoryCredentialStore(), http,
        requestCredentials: () => prompts++);

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

      Assert.That(CoordinationCredentialWindow.TrySubmitToken(store, target, "developer-token"), Is.True);
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
    public async Task CredentialReadFailureIsReportedWithoutPromptingForCredentials()
    {
      var prompts = 0;
      var errors = new List<CoordinationServerEnvelope>();
      var service = CreateService(new ThrowingCredentialStore(), new FakeHttpClient(),
        requestCredentials: () => prompts++);
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

    private static MemoryCredentialStore Credentials()
    {
      var credentials = new MemoryCredentialStore();
      credentials.Write(CoordinationCredentialStore.GetDeveloperTokenTarget("potion-panic"),
        "developer-token");
      return credentials;
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
      FakeHttpClient http,
      FakeWebSocketClient socket = null,
      Action requestCredentials = null,
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

      public Task<CoordinationHttpResponse> CreateSessionAsync(Uri uri, string developerToken)
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

    private sealed class FakeWebSocketClient : ICoordinationWebSocketClient
    {
      public event Action<string> MessageReceived;
      public event Action<int, string> Closed;
      public List<string> SentMessages { get; } = new List<string>();
      public int ConnectCalls { get; private set; }
      public int CloseCalls { get; private set; }
      public Exception SendException;

      public Task ConnectAsync(Uri uri, string sessionToken, CancellationToken cancellationToken)
      {
        ConnectCalls++;
        return Task.CompletedTask;
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
        CloseCalls++;
        return Task.CompletedTask;
      }

      public void RaiseMessage(string message) => MessageReceived?.Invoke(message);
      public void RaiseClosed(int statusCode, string reason) => Closed?.Invoke(statusCode, reason);
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
