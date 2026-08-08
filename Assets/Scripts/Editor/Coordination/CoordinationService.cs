using System;
using System.Collections.Generic;
using System.Threading;
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

  public interface ICoordinationDelay
  {
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
  }

  public sealed class SystemCoordinationDelay : ICoordinationDelay
  {
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
      return Task.Delay(delay, cancellationToken);
    }
  }

  public sealed class CoordinationRequestHandle
  {
    public string RequestId { get; }
    public string Type { get; }
    public string NormalizedPath { get; }

    internal CoordinationRequestHandle(string requestId, string type, string normalizedPath)
    {
      RequestId = requestId;
      Type = type;
      NormalizedPath = normalizedPath;
    }
  }

  public sealed class CoordinationRequestCompletion
  {
    public CoordinationRequestHandle Request { get; }
    public CoordinationServerEnvelope Response { get; }
    public bool IsStaleReplay { get; }

    internal CoordinationRequestCompletion(
      CoordinationRequestHandle request,
      CoordinationServerEnvelope response,
      bool isStaleReplay)
    {
      Request = request;
      Response = response;
      IsStaleReplay = isStaleReplay;
    }
  }

  public sealed class CoordinationRequestSendFailure
  {
    public CoordinationRequestHandle Request { get; }
    public string Message { get; }

    internal CoordinationRequestSendFailure(CoordinationRequestHandle request, string message)
    {
      Request = request;
      Message = message;
    }
  }

  public sealed class CoordinationService : ICoordinationAssetService, ICoordinationSaveService
  {
    private static readonly int[] ReconnectDelaysSeconds = { 1, 2, 4, 8, 16, 30 };
    private static readonly object RandomLock = new object();
    private static readonly System.Random Random = new System.Random();

    private readonly CoordinationConfig configuration;
    private readonly CoordinationUserSettings settings;
    private readonly ICredentialStore credentialStore;
    private readonly ICoordinationHttpClient httpClient;
    private readonly ICoordinationWebSocketClient webSocketClient;
    private readonly IMainThreadDispatcher dispatcher;
    private readonly ICoordinationGitContext gitContext;
    private readonly bool isSupportedPlatform;
    private readonly Action requestCredentials;
    private readonly ICoordinationDelay delay;
    private readonly Func<TimeSpan, TimeSpan> reconnectJitter;
    private readonly CoordinationProtocolState protocolState = new CoordinationProtocolState();
    private readonly Dictionary<string, CoordinationRequestHandle> pendingRequests
      = new Dictionary<string, CoordinationRequestHandle>();
    private readonly object lifecycleLock = new object();

    private CoordinationSessionResponse session;
    private CancellationTokenSource connectionCancellation = new CancellationTokenSource();
    private CancellationTokenSource heartbeatCancellation;
    private Task activeConnectionAttempt;
    private Task reconnectLoop;
    private Task heartbeatLoop;
    private bool hasPromptedForCredentials;
    private bool credentialUnavailable;
    private bool shutdown;

    public CoordinationConnectionState State { get; private set; }
    public bool HasSession => session != null;
    public event Action<CoordinationConnectionState> StateChanged;
    public event Action<CoordinationServerEnvelope> SessionReady;
    public event Action<CoordinationServerEnvelope> SnapshotReceived;
    public event Action<CoordinationServerEnvelope> PresenceReceived;
    public event Action<CoordinationServerEnvelope> PresenceRemoved;
    public event Action<CoordinationServerEnvelope> LeaseResultReceived;
    public event Action<CoordinationServerEnvelope> ErrorReceived;
    public event Action<CoordinationRequestCompletion> RequestCompleted;
    public event Action<CoordinationRequestSendFailure> RequestSendFailed;
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
      Action requestCredentials = null,
      ICoordinationDelay delay = null,
      Func<TimeSpan, TimeSpan> reconnectJitter = null)
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
      this.delay = delay ?? new SystemCoordinationDelay();
      this.reconnectJitter = reconnectJitter ?? ApplyDefaultJitter;
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

    public Task ConnectAsync()
    {
      if (shutdown)
      {
        return Task.CompletedTask;
      }

      credentialUnavailable = false;
      EnsureConnectionCancellation();
      return StartConnectionAttempt();
    }

    public async Task ForgetCredentialsAsync()
    {
      credentialUnavailable = true;
      session = null;
      CancelConnectionWork();
      try
      {
        credentialStore.Delete(CoordinationCredentialStore.GetDeveloperTokenTarget(configuration.projectId));
      }
      catch (Exception exception)
      {
        PublishTransportError("credential_delete_failed", exception.Message);
      }

      try
      {
        await webSocketClient.CloseAsync(CancellationToken.None);
      }
      catch (Exception exception)
      {
        PublishTransportError("connection_close_failed", exception.Message);
      }

      SetState(IsDisabled ? CoordinationConnectionState.Disabled : CoordinationConnectionState.Offline);
    }

    public async Task ShutdownAsync()
    {
      if (shutdown)
      {
        return;
      }

      shutdown = true;
      CancelConnectionWork();
      try
      {
        await webSocketClient.CloseAsync(CancellationToken.None);
      }
      catch
      {
      }

      await AwaitQuietly(heartbeatLoop);
      await AwaitQuietly(reconnectLoop);
      await AwaitQuietly(activeConnectionAttempt);
      webSocketClient.MessageReceived -= OnSocketMessage;
      webSocketClient.Closed -= OnSocketClosed;
    }

    public bool TryOpenPresence(string path, out CoordinationRequestHandle request)
    {
      return TrySend("presence.open", path, true, out request);
    }

    public bool TryClosePresence(string path, out CoordinationRequestHandle request)
    {
      return TrySend("presence.close", path, false, out request);
    }

    public bool TryAcquireLease(string path, out CoordinationRequestHandle request)
    {
      return TrySend("lease.acquire", path, true, out request);
    }

    public bool TryReleaseLease(string path, out CoordinationRequestHandle request)
    {
      return TrySend("lease.release", path, false, out request);
    }

    public bool TryReserveLease(string path, out CoordinationRequestHandle request)
    {
      return TrySend("lease.reserve", path, true, out request);
    }

    public bool TryOverrideLease(string path, out CoordinationRequestHandle request)
    {
      return TrySend("lease.override", path, true, out request);
    }

    public bool TrySendHeartbeat(out CoordinationRequestHandle request)
    {
      return TrySend("heartbeat", null, false, out request);
    }

    public bool TryRequestSnapshot(out CoordinationRequestHandle request)
    {
      return TrySend("snapshot.request", null, false, out request);
    }

    private bool IsDisabled => !isSupportedPlatform || settings.disabled;

    private Task StartConnectionAttempt()
    {
      if (shutdown || credentialUnavailable)
      {
        return Task.CompletedTask;
      }

      if (IsDisabled)
      {
        SetState(CoordinationConnectionState.Disabled);
        return Task.CompletedTask;
      }

      lock (lifecycleLock)
      {
        if (activeConnectionAttempt != null && !activeConnectionAttempt.IsCompleted)
        {
          return activeConnectionAttempt;
        }

        var attempt = ConnectCoreAsync(connectionCancellation.Token);
        activeConnectionAttempt = attempt;
        _ = attempt.ContinueWith(completed =>
        {
          lock (lifecycleLock)
          {
            if (ReferenceEquals(activeConnectionAttempt, completed))
            {
              activeConnectionAttempt = null;
            }
          }
        }, TaskScheduler.Default);
        return attempt;
      }
    }

    private async Task ConnectCoreAsync(CancellationToken cancellationToken)
    {
      var credentialTarget = CoordinationCredentialStore.GetDeveloperTokenTarget(configuration.projectId);
      string developerToken;
      try
      {
        if (!credentialStore.TryRead(credentialTarget, out developerToken)
          || string.IsNullOrWhiteSpace(developerToken))
        {
          credentialUnavailable = true;
          session = null;
          SetState(CoordinationConnectionState.Offline);
          PromptForCredentials();
          return;
        }
      }
      catch (Exception exception)
      {
        credentialUnavailable = true;
        session = null;
        SetState(CoordinationConnectionState.Offline);
        PublishTransportError("credential_read_failed", exception.Message);
        return;
      }

      SetState(CoordinationConnectionState.Reconnecting);
      CoordinationHttpResponse response;
      try
      {
        response = await httpClient.CreateSessionAsync(SessionUri(), developerToken);
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        return;
      }
      catch (Exception exception)
      {
        HandleConnectionFailure("session_unavailable", exception.Message);
        return;
      }

      if (cancellationToken.IsCancellationRequested)
      {
        return;
      }

      if (response.StatusCode == 401 || response.StatusCode == 403)
      {
        session = null;
        credentialUnavailable = true;
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
        HandleConnectionFailure("session_unavailable", error ?? "The session request failed.");
        return;
      }

      try
      {
        await webSocketClient.ConnectAsync(WebSocketUri(), session.SessionToken, cancellationToken);
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
      }
      catch (Exception exception)
      {
        session = null;
        HandleConnectionFailure("connection_failed", exception.Message);
      }
    }

    private void HandleConnectionFailure(string code, string message)
    {
      session = null;
      StopHeartbeat();
      if (IsDisabled)
      {
        SetState(CoordinationConnectionState.Disabled);
        return;
      }

      SetState(CoordinationConnectionState.Offline);
      PublishTransportError(code, message);
      StartReconnectLoop();
    }

    private void StartReconnectLoop()
    {
      if (shutdown || credentialUnavailable || IsDisabled)
      {
        return;
      }

      lock (lifecycleLock)
      {
        if (reconnectLoop != null && !reconnectLoop.IsCompleted)
        {
          return;
        }

        reconnectLoop = ReconnectLoopAsync(connectionCancellation.Token);
      }
    }

    private async Task ReconnectLoopAsync(CancellationToken cancellationToken)
    {
      try
      {
        var attempt = 0;
        while (!cancellationToken.IsCancellationRequested && !shutdown
          && !credentialUnavailable && !IsDisabled)
        {
          var baseDelay = TimeSpan.FromSeconds(ReconnectDelaysSeconds[
            Math.Min(attempt, ReconnectDelaysSeconds.Length - 1)]);
          await delay.DelayAsync(reconnectJitter(baseDelay), cancellationToken);
          if (cancellationToken.IsCancellationRequested || shutdown || credentialUnavailable || IsDisabled)
          {
            return;
          }

          await StartConnectionAttempt();
          if (State == CoordinationConnectionState.Connected
            || State == CoordinationConnectionState.Reconnecting
            || State == CoordinationConnectionState.AuthenticationFailed)
          {
            return;
          }

          attempt++;
        }
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
      }
    }

    private void StartHeartbeat()
    {
      StopHeartbeat();
      if (shutdown || credentialUnavailable || IsDisabled || State != CoordinationConnectionState.Connected)
      {
        return;
      }

      var cancellation = CancellationTokenSource.CreateLinkedTokenSource(connectionCancellation.Token);
      heartbeatCancellation = cancellation;
      heartbeatLoop = HeartbeatLoopAsync(cancellation);
    }

    private async Task HeartbeatLoopAsync(CancellationTokenSource cancellation)
    {
      try
      {
        while (!cancellation.IsCancellationRequested && State == CoordinationConnectionState.Connected)
        {
          await delay.DelayAsync(TimeSpan.FromSeconds(configuration.heartbeatSeconds),
            cancellation.Token);
          if (cancellation.IsCancellationRequested || State != CoordinationConnectionState.Connected)
          {
            return;
          }

          TrySendHeartbeat(out _);
        }
      }
      catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
      {
      }
      finally
      {
        if (ReferenceEquals(heartbeatCancellation, cancellation))
        {
          heartbeatCancellation = null;
        }
        cancellation.Dispose();
      }
    }

    private void StopHeartbeat()
    {
      var cancellation = heartbeatCancellation;
      heartbeatCancellation = null;
      cancellation?.Cancel();
    }

    private bool TrySend(string type, string path, bool includeContext,
      out CoordinationRequestHandle request)
    {
      request = null;
      if (State != CoordinationConnectionState.Connected || shutdown || credentialUnavailable)
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
      if (!CoordinationProtocol.TryParseClientEnvelope(json, out var parsed, out _))
      {
        return false;
      }

      request = new CoordinationRequestHandle(parsed.requestId, parsed.type, parsed.path ?? string.Empty);
      lock (pendingRequests)
      {
        pendingRequests.Add(request.RequestId, request);
      }

      _ = SendAsync(request, JsonUtility.ToJson(parsed));
      return true;
    }

    private async Task SendAsync(CoordinationRequestHandle request, string json)
    {
      try
      {
        await webSocketClient.SendAsync(json, connectionCancellation.Token);
      }
      catch (OperationCanceledException) when (connectionCancellation.IsCancellationRequested)
      {
        ReportRequestSendFailure(request, "The coordination connection was cancelled.");
      }
      catch (Exception exception)
      {
        ReportRequestSendFailure(request, exception.Message);
        dispatcher.Post(() => HandleConnectionFailure("connection_failed", exception.Message));
      }
    }

    private void ReportRequestSendFailure(CoordinationRequestHandle request, string message)
    {
      lock (pendingRequests)
      {
        pendingRequests.Remove(request.RequestId);
      }
      dispatcher.Post(() => RequestSendFailed?.Invoke(new CoordinationRequestSendFailure(request, message)));
    }

    private void OnSocketMessage(string json)
    {
      dispatcher.Post(() => ApplySocketMessage(json));
    }

    private void ApplySocketMessage(string json)
    {
      if (!CoordinationProtocol.TryParseServerEnvelope(json, out var envelope, out var error))
      {
        PublishTransportError("invalid_server_message", error);
        return;
      }

      if (envelope.stateVersion < protocolState.NewestAppliedStateVersion)
      {
        CompleteRequest(envelope, true);
        return;
      }

      if (!protocolState.TryApplyServerEnvelope(envelope, out error))
      {
        PublishTransportError("invalid_server_message", error);
        return;
      }

      switch (envelope.type)
      {
        case "session.ready":
          SetState(CoordinationConnectionState.Connected);
          SessionReady?.Invoke(envelope);
          StartHeartbeat();
          break;
        case "snapshot":
          SnapshotReceived?.Invoke(envelope);
          break;
        case "presence.updated":
          PresenceReceived?.Invoke(envelope);
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

      CompleteRequest(envelope, false);
    }

    private void CompleteRequest(CoordinationServerEnvelope envelope, bool staleReplay)
    {
      if (string.IsNullOrWhiteSpace(envelope.requestId))
      {
        return;
      }

      CoordinationRequestHandle request;
      lock (pendingRequests)
      {
        if (!pendingRequests.TryGetValue(envelope.requestId, out request))
        {
          return;
        }
        pendingRequests.Remove(envelope.requestId);
      }

      RequestCompleted?.Invoke(new CoordinationRequestCompletion(request, envelope, staleReplay));
    }

    private void OnSocketClosed(int statusCode, string reason)
    {
      dispatcher.Post(() =>
      {
        session = null;
        StopHeartbeat();
        if (shutdown || credentialUnavailable)
        {
          return;
        }

        if (statusCode == 4003)
        {
          credentialUnavailable = true;
          SetState(CoordinationConnectionState.AuthenticationFailed);
          Revoked?.Invoke();
          return;
        }

        if (IsDisabled)
        {
          SetState(CoordinationConnectionState.Disabled);
          return;
        }

        SetState(CoordinationConnectionState.Reconnecting);
        PublishTransportError("connection_closed", reason);
        if (statusCode == 4001)
        {
          _ = StartConnectionAttempt();
          return;
        }

        StartReconnectLoop();
      });
    }

    private void EnsureConnectionCancellation()
    {
      lock (lifecycleLock)
      {
        if (!connectionCancellation.IsCancellationRequested)
        {
          return;
        }

        connectionCancellation = new CancellationTokenSource();
      }
    }

    private void CancelConnectionWork()
    {
      CancellationTokenSource cancellation;
      lock (lifecycleLock)
      {
        cancellation = connectionCancellation;
      }
      cancellation.Cancel();
      StopHeartbeat();
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

    private static TimeSpan ApplyDefaultJitter(TimeSpan delay)
    {
      lock (RandomLock)
      {
        return TimeSpan.FromMilliseconds(delay.TotalMilliseconds * (0.5 + Random.NextDouble()));
      }
    }

    private static async Task AwaitQuietly(Task task)
    {
      if (task == null)
      {
        return;
      }

      try
      {
        await task;
      }
      catch (OperationCanceledException)
      {
      }
      catch
      {
      }
    }
  }
}
