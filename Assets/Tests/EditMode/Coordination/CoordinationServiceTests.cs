using System;
using System.Collections.Generic;
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
      var credentials = new MemoryCredentialStore();
      credentials.Write(CoordinationCredentialStore.GetDeveloperTokenTarget("potion-panic"), "bad-token");
      var service = CreateService(credentials, new FakeHttpClient { StatusCode = 401 });

      await service.ConnectAsync();

      Assert.That(service.State, Is.EqualTo(CoordinationConnectionState.AuthenticationFailed));
      Assert.That(service.HasSession, Is.False);
    }

    [Test]
    public async Task UnsupportedPlatformRemainsDisabledWithoutCallingTheServer()
    {
      var http = new FakeHttpClient();
      var service = CreateService(new MemoryCredentialStore(), http, isSupportedPlatform: false);

      await service.ConnectAsync();

      Assert.That(service.State, Is.EqualTo(CoordinationConnectionState.Disabled));
      Assert.That(http.Calls, Is.EqualTo(0));
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
    public async Task SocketMessagesAreDispatchedOnTheMainThreadAndReconnectUsesANewSession()
    {
      var credentials = new MemoryCredentialStore();
      credentials.Write(CoordinationCredentialStore.GetDeveloperTokenTarget("potion-panic"), "developer-token");
      var dispatcher = new QueuedMainThreadDispatcher();
      var http = new FakeHttpClient();
      var socket = new FakeWebSocketClient();
      var service = CreateService(credentials, http, socket, dispatcher);
      var readyCount = 0;
      service.SessionReady += _ => readyCount++;

      await service.ConnectAsync();
      socket.RaiseMessage("{\"protocolVersion\":1,\"type\":\"session.ready\",\"stateVersion\":1,"
        + "\"developerId\":\"dev-1\",\"displayName\":\"Rin\",\"serverTime\":\"2026-08-07T12:00:00Z\","
        + "\"connectionId\":\"connection-1\",\"leaseTtlSeconds\":120,\"reservationTtlSeconds\":1800}");

      Assert.That(readyCount, Is.EqualTo(0));
      dispatcher.ExecutePending();
      Assert.That(readyCount, Is.EqualTo(1));
      Assert.That(service.State, Is.EqualTo(CoordinationConnectionState.Connected));

      socket.RaiseClosed(1006, "network interrupted");
      dispatcher.ExecutePending();
      Assert.That(service.State, Is.EqualTo(CoordinationConnectionState.Reconnecting));

      await service.ReconnectAsync();
      Assert.That(http.Calls, Is.EqualTo(2));
    }

    [Test]
    public async Task OfflineServiceDoesNotQueueLeaseMutations()
    {
      var credentials = new MemoryCredentialStore();
      credentials.Write(CoordinationCredentialStore.GetDeveloperTokenTarget("potion-panic"), "developer-token");
      var socket = new FakeWebSocketClient();
      var service = CreateService(credentials, new FakeHttpClient { StatusCode = 503 }, socket);

      await service.ConnectAsync();

      Assert.That(service.TryAcquireLease("Assets/Scenes/SampleScene.unity"), Is.False);
      Assert.That(socket.SentMessages, Is.Empty);
    }

    [Test]
    public async Task PresenceRemovalIsRaisedOnTheMainThread()
    {
      var credentials = new MemoryCredentialStore();
      credentials.Write(CoordinationCredentialStore.GetDeveloperTokenTarget("potion-panic"), "developer-token");
      var dispatcher = new QueuedMainThreadDispatcher();
      var socket = new FakeWebSocketClient();
      var service = CreateService(credentials, new FakeHttpClient(), socket, dispatcher);
      CoordinationServerEnvelope removed = null;
      service.PresenceRemoved += envelope => removed = envelope;

      await service.ConnectAsync();
      socket.RaiseMessage("{\"protocolVersion\":1,\"type\":\"presence.removed\",\"stateVersion\":1,"
        + "\"path\":\"Assets/Scenes/SampleScene.unity\",\"connectionId\":\"connection-1\"}");

      Assert.That(removed, Is.Null);
      dispatcher.ExecutePending();
      Assert.That(removed.path, Is.EqualTo("Assets/Scenes/SampleScene.unity"));
    }

    private static CoordinationService CreateService(
      ICredentialStore credentials,
      FakeHttpClient http,
      FakeWebSocketClient socket = null,
      IMainThreadDispatcher dispatcher = null,
      bool isSupportedPlatform = true,
      Action requestCredentials = null)
    {
      return new CoordinationService(
        Configuration,
        CoordinationUserSettings.CreateDefault(),
        credentials,
        http,
        socket ?? new FakeWebSocketClient(),
        dispatcher ?? new ImmediateMainThreadDispatcher(),
        new FixedGitContext("feature/coordination-05"),
        isSupportedPlatform,
        requestCredentials);
    }

    private sealed class FakeHttpClient : ICoordinationHttpClient
    {
      public int Calls { get; private set; }
      public int StatusCode = 201;

      public Task<CoordinationHttpResponse> CreateSessionAsync(Uri uri, string developerToken)
      {
        Calls++;
        return Task.FromResult(new CoordinationHttpResponse(
          StatusCode,
          StatusCode == 201
            ? "{\"developerId\":\"dev-1\",\"displayName\":\"Rin\",\"sessionToken\":\"session-"
              + Calls + "\",\"serverTime\":\"2026-08-07T12:00:00Z\",\"leaseTtlSeconds\":120,"
              + "\"reservationTtlSeconds\":1800,\"stateVersion\":0}"
            : "Unauthorized"));
      }
    }

    private sealed class FakeWebSocketClient : ICoordinationWebSocketClient
    {
      public event Action<string> MessageReceived;
      public event Action<int, string> Closed;
      public List<string> SentMessages { get; } = new List<string>();

      public Task ConnectAsync(Uri uri, string sessionToken) => Task.CompletedTask;

      public Task SendAsync(string message)
      {
        SentMessages.Add(message);
        return Task.CompletedTask;
      }

      public Task CloseAsync() => Task.CompletedTask;
      public void RaiseMessage(string message) => MessageReceived?.Invoke(message);
      public void RaiseClosed(int statusCode, string reason) => Closed?.Invoke(statusCode, reason);
    }

    private sealed class FixedGitContext : ICoordinationGitContext
    {
      private readonly string branch;
      public FixedGitContext(string branch) => this.branch = branch;
      public string GetBranch() => branch;
    }
  }
}
