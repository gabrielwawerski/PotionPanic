using System;
using System.Threading.Tasks;
using UnityEngine;

namespace PotionPanic.Editor.Coordination
{
  public enum CoordinationConnectionState
  {
    Connected,
    Reconnecting,
    Offline,
    AuthenticationFailed,
    Disabled
  }

  public sealed class CoordinationService
  {
    private readonly CoordinationConfig configuration;
    private readonly CoordinationUserSettings settings;
    private readonly ICredentialStore credentialStore;
    private readonly ICoordinationHttpClient httpClient;
    private readonly ICoordinationWebSocketClient webSocketClient;
    private readonly IMainThreadDispatcher dispatcher;
    private readonly ICoordinationGitContext gitContext;
    private readonly bool isSupportedPlatform;
    private readonly Action requestCredentials;
    private readonly CoordinationProtocolState protocolState = new CoordinationProtocolState();
    private CoordinationSessionResponse session;
    private bool hasPromptedForCredentials;

    public CoordinationConnectionState State { get; private set; }
    public bool HasSession => session != null;
    public event Action<CoordinationConnectionState> StateChanged;
    public event Action<CoordinationServerEnvelope> SessionReady;
    public event Action<CoordinationServerEnvelope> SnapshotReceived;
    public event Action<CoordinationPresenceRecord[]> PresenceReceived;
    public event Action<CoordinationServerEnvelope> PresenceRemoved;
    public event Action<CoordinationServerEnvelope> LeaseResultReceived;
    public event Action<CoordinationServerEnvelope> ErrorReceived;
    public event Action Revoked;

    public CoordinationService(
      CoordinationConfig configuration,
      CoordinationUserSettings settings,
      ICredentialStore credentialStore,
      ICoordinationHttpClient httpClient,
      ICoordinationWebSocketClient webSocketClient,
      IMainThreadDispatcher dispatcher,
      ICoordinationGitContext gitContext,
      bool isSupportedPlatform,
      Action requestCredentials = null)
    {
      this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
      this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
      this.credentialStore = credentialStore ?? throw new ArgumentNullException(nameof(credentialStore));
      this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
      this.webSocketClient = webSocketClient ?? throw new ArgumentNullException(nameof(webSocketClient));
      this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
      this.gitContext = gitContext ?? throw new ArgumentNullException(nameof(gitContext));
      this.isSupportedPlatform = isSupportedPlatform;
      this.requestCredentials = requestCredentials;
      State = IsDisabled ? CoordinationConnectionState.Disabled : CoordinationConnectionState.Offline;
      webSocketClient.MessageReceived += OnSocketMessage;
      webSocketClient.Closed += OnSocketClosed;
    }

    public static bool TryCreateDefault(out CoordinationService service, out string error)
    {
      service = null;
      if (!CoordinationConfig.TryLoad(out var configuration, out error)
        || !CoordinationUserSettings.TryLoad(out var settings, out error))
      {
        return false;
      }

      service = new CoordinationService(
        configuration,
        settings,
        new WindowsCredentialStore(),
        new UnityWebRequestCoordinationHttpClient(),
        new ClientWebSocketCoordinationClient(),
        new UnityMainThreadDispatcher(),
        new GitCoordinationContext(),
        Application.platform == RuntimePlatform.WindowsEditor,
        () => CoordinationCredentialWindow.ShowForProject(
          configuration.projectId, new WindowsCredentialStore()));
      return true;
    }

    public async Task ConnectAsync()
    {
      if (IsDisabled)
      {
        SetState(CoordinationConnectionState.Disabled);
        return;
      }

      var credentialTarget = CoordinationCredentialStore.GetDeveloperTokenTarget(configuration.projectId);
      if (!credentialStore.TryRead(credentialTarget, out var developerToken)
        || string.IsNullOrWhiteSpace(developerToken))
      {
        session = null;
        SetState(CoordinationConnectionState.Offline);
        PromptForCredentials();
        return;
      }

      SetState(CoordinationConnectionState.Reconnecting);
      CoordinationHttpResponse response;
      try
      {
        response = await httpClient.CreateSessionAsync(SessionUri(), developerToken);
      }
      catch (Exception exception)
      {
        session = null;
        SetState(CoordinationConnectionState.Offline);
        PublishTransportError("session_unavailable", exception.Message);
        return;
      }

      if (response.StatusCode == 401 || response.StatusCode == 403)
      {
        session = null;
        SetState(CoordinationConnectionState.AuthenticationFailed);
        PublishTransportError("authentication_failed", "The developer token was rejected.");
        return;
      }

      string error = null;
      var isSuccessfulResponse = response.StatusCode >= 200 && response.StatusCode < 300;
      if (!isSuccessfulResponse
        || !CoordinationSessionResponse.TryParse(response.Body, out session, out error))
      {
        session = null;
        SetState(CoordinationConnectionState.Offline);
        PublishTransportError("session_unavailable", error ?? "The session request failed.");
        return;
      }

      try
      {
        await webSocketClient.ConnectAsync(WebSocketUri(), session.SessionToken);
      }
      catch (Exception exception)
      {
        SetState(CoordinationConnectionState.Offline);
        PublishTransportError("connection_failed", exception.Message);
      }
    }

    public Task ReconnectAsync()
    {
      return ConnectAsync();
    }

    public void ForgetCredentials()
    {
      credentialStore.Delete(CoordinationCredentialStore.GetDeveloperTokenTarget(configuration.projectId));
      session = null;
      _ = webSocketClient.CloseAsync();
      SetState(IsDisabled ? CoordinationConnectionState.Disabled : CoordinationConnectionState.Offline);
    }

    public bool TryOpenPresence(string path) => TrySend("presence.open", path, true);
    public bool TryClosePresence(string path) => TrySend("presence.close", path, false);
    public bool TryAcquireLease(string path) => TrySend("lease.acquire", path, true);
    public bool TryReleaseLease(string path) => TrySend("lease.release", path, false);
    public bool TryReserveLease(string path) => TrySend("lease.reserve", path, true);
    public bool TryOverrideLease(string path) => TrySend("lease.override", path, true);
    public bool TrySendHeartbeat() => TrySend("heartbeat", null, false);
    public bool TryRequestSnapshot() => TrySend("snapshot.request", null, false);

    private bool IsDisabled => !isSupportedPlatform || settings.disabled;

    private bool TrySend(string type, string path, bool includeContext)
    {
      if (State != CoordinationConnectionState.Connected)
      {
        return false;
      }

      var envelope = new CoordinationClientEnvelope
      {
        protocolVersion = CoordinationProtocol.Version,
        type = type,
        requestId = Guid.NewGuid().ToString(),
        path = path,
        branch = includeContext ? gitContext.GetBranch() ?? string.Empty : null,
        task = includeContext ? settings.taskContext ?? string.Empty : null
      };
      var json = JsonUtility.ToJson(envelope);
      if (!CoordinationProtocol.TryParseClientEnvelope(json, out _, out _))
      {
        return false;
      }

      _ = SendAsync(json);
      return true;
    }

    private async Task SendAsync(string json)
    {
      try
      {
        await webSocketClient.SendAsync(json);
      }
      catch (Exception exception)
      {
        dispatcher.Post(() =>
        {
          SetState(CoordinationConnectionState.Reconnecting);
          PublishTransportError("connection_failed", exception.Message);
        });
      }
    }

    private void OnSocketMessage(string json)
    {
      dispatcher.Post(() => ApplySocketMessage(json));
    }

    private void ApplySocketMessage(string json)
    {
      if (!protocolState.TryApplyServerEnvelope(json, out var envelope, out var error))
      {
        PublishTransportError("invalid_server_message", error);
        return;
      }

      switch (envelope.type)
      {
        case "session.ready":
          SetState(CoordinationConnectionState.Connected);
          SessionReady?.Invoke(envelope);
          TryRequestSnapshot();
          break;
        case "snapshot":
          SnapshotReceived?.Invoke(envelope);
          break;
        case "presence.updated":
          PresenceReceived?.Invoke(envelope.presence);
          break;
        case "presence.removed":
          PresenceRemoved?.Invoke(envelope);
          break;
        case "lease.granted":
        case "lease.denied":
        case "lease.updated":
        case "lease.released":
        case "lease.overridden":
          LeaseResultReceived?.Invoke(envelope);
          break;
        case "error":
          ErrorReceived?.Invoke(envelope);
          break;
      }
    }

    private void OnSocketClosed(int statusCode, string reason)
    {
      dispatcher.Post(() =>
      {
        session = null;
        if (statusCode == 4003)
        {
          SetState(CoordinationConnectionState.AuthenticationFailed);
          Revoked?.Invoke();
          return;
        }

        SetState(IsDisabled ? CoordinationConnectionState.Disabled : CoordinationConnectionState.Reconnecting);
        PublishTransportError("connection_closed", reason);
      });
    }

    private void SetState(CoordinationConnectionState state)
    {
      if (State == state)
      {
        return;
      }

      State = state;
      StateChanged?.Invoke(state);
    }

    private void PromptForCredentials()
    {
      if (hasPromptedForCredentials)
      {
        return;
      }

      hasPromptedForCredentials = true;
      requestCredentials?.Invoke();
    }

    private void PublishTransportError(string code, string message)
    {
      ErrorReceived?.Invoke(new CoordinationServerEnvelope
      {
        protocolVersion = CoordinationProtocol.Version,
        type = "error",
        stateVersion = protocolState.NewestAppliedStateVersion,
        code = code,
        message = message ?? string.Empty
      });
    }

    private Uri SessionUri()
    {
      return EndpointUri(CoordinationConfig.GetEffectiveServerBaseUrl(configuration, settings), "sessions");
    }

    private Uri WebSocketUri()
    {
      return EndpointUri(CoordinationConfig.GetWebSocketBaseUrl(configuration, settings), "connect");
    }

    private Uri EndpointUri(string baseUrl, string endpoint)
    {
      return new Uri(baseUrl + "/v1/projects/" + Uri.EscapeDataString(configuration.projectId)
        + "/" + endpoint);
    }
  }
}
