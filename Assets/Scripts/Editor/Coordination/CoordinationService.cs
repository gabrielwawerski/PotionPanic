using System;
using System.Collections.Generic;
using System.Text;
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

  public interface ICoordinationWindowService
  {
    CoordinationConnectionState State { get; }
    string DeveloperId { get; }
    string DisplayName { get; }
    string ConnectionId { get; }
    bool IsSupportedPlatform { get; }
    event Action<CoordinationConnectionState> StateChanged;

    Task ConnectAsync();
    Task ForgetCredentialsAsync();
    Task SetDisabledAsync(bool disabled);
    bool TryReserveLease(string path, out CoordinationRequestHandle request);
    bool TryReleaseLease(string path, out CoordinationRequestHandle request);
    bool TryOverrideLease(string path, out CoordinationRequestHandle request);
  }

  public interface ICoordinationNotificationSource
  {
    CoordinationConnectionState State { get; }
    event Action<CoordinationConnectionState> StateChanged;
    event Action<CoordinationServerEnvelope> LeaseResultReceived;
    event Action<CoordinationServerEnvelope> ErrorReceived;
  }

  public interface ICoordinationWarningService
  {
    CoordinationConnectionState State { get; }
    event Action<CoordinationConnectionState> StateChanged;
    event Action<CoordinationServerEnvelope> SessionReady;
  }

  public sealed class CoordinationService : ICoordinationAssetService,
    ICoordinationSaveService,
    ICoordinationWindowService,
    ICoordinationNotificationSource,
    ICoordinationWarningService
  {
    private static readonly int[] ReconnectDelaysSeconds = { 1, 2, 4, 8, 16, 30 };
    private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(2);
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
    private readonly Action<Action> requestCredentials;
    private readonly ICoordinationDelay delay;
    private readonly Func<TimeSpan, TimeSpan> reconnectJitter;
    private readonly object socketMessageGateLock = new object();
    private readonly CoordinationProtocolState protocolState = new CoordinationProtocolState();
    private readonly CoordinationSnapshotAssembler snapshotAssembler
      = new CoordinationSnapshotAssembler();
    private readonly Dictionary<string, CoordinationRequestHandle> pendingRequests
      = new Dictionary<string, CoordinationRequestHandle>();
    private readonly HashSet<Task> sendTasks = new HashSet<Task>();
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
    private bool socketMessagesAccepted;
    private int socketMessageGeneration;
    private string currentConnectionId = string.Empty;

    public CoordinationConnectionState State { get; private set; }
    public bool HasSession => session != null;
    public string DeveloperId => session?.DeveloperId ?? string.Empty;
    public string DisplayName => session?.DisplayName ?? string.Empty;
    public string ConnectionId => currentConnectionId;
    public bool IsSupportedPlatform => isSupportedPlatform;
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
      Action<Action> requestCredentials = null,
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
        onSaved => CoordinationCredentialWindow.ShowForProject(
          configuration.projectId, new WindowsCredentialStore(), onSaved));
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
      InvalidateSocketMessagesAndResetSnapshot();
      credentialUnavailable = true;
      hasPromptedForCredentials = false;
      session = null;
      currentConnectionId = string.Empty;
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
        await CloseSocketAsync();
      }
      catch (Exception exception)
      {
        PublishTransportError("connection_close_failed", exception.Message);
      }

      SetState(IsDisabled ? CoordinationConnectionState.Disabled : CoordinationConnectionState.Offline);
    }

    public async Task SetDisabledAsync(bool disabled)
    {
      if (!isSupportedPlatform || shutdown)
      {
        return;
      }

      settings.disabled = disabled;
      if (!disabled)
      {
        credentialUnavailable = false;
        EnsureConnectionCancellation();
        SetState(CoordinationConnectionState.Offline);
        await ConnectAsync();
        return;
      }

      InvalidateSocketMessagesAndResetSnapshot();
      session = null;
      currentConnectionId = string.Empty;
      CancelConnectionWork();
      await AwaitQuietly(heartbeatLoop);
      await AwaitQuietly(reconnectLoop);
      await AwaitQuietly(activeConnectionAttempt);
      try
      {
        await CloseSocketAsync();
      }
      catch (Exception exception)
      {
        PublishTransportError("connection_close_failed", exception.Message);
      }
      SetState(CoordinationConnectionState.Disabled);
    }

    public async Task ShutdownAsync()
    {
      if (shutdown)
      {
        return;
      }

      shutdown = true;
      InvalidateSocketMessagesAndResetSnapshot();
      CancelConnectionWork();
      try
      {
        await CloseSocketAsync().ConfigureAwait(false);
      }
      catch
      {
      }

      webSocketClient.MessageReceived -= OnSocketMessage;
      webSocketClient.Closed -= OnSocketClosed;
    }

    public async Task FlushPendingSendsAsync(TimeSpan timeout)
    {
      if (timeout <= TimeSpan.Zero)
      {
        throw new ArgumentOutOfRangeException(nameof(timeout));
      }

      Task[] pending;
      lock (sendTasks)
      {
        pending = new List<Task>(sendTasks).ToArray();
      }

      var allSends = Task.WhenAll(pending);
      await Task.WhenAny(allSends, Task.Delay(timeout)).ConfigureAwait(false);
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
        response = await httpClient.CreateSessionAsync(
          SessionUri(), developerToken, cancellationToken);
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
        var replacingConnection = InvalidateSocketMessagesAndResetSnapshot();
        if (replacingConnection)
        {
          await CloseSocketAsync();
        }

        cancellationToken.ThrowIfCancellationRequested();
        EnableSocketMessages();
        await webSocketClient.ConnectAsync(WebSocketUri(), session.SessionToken, cancellationToken);
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        InvalidateSocketMessagesAndResetSnapshot();
      }
      catch (Exception exception)
      {
        InvalidateSocketMessagesAndResetSnapshot();
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
        branch = includeContext ? CoordinationProtocol.ClampContext(gitContext.GetBranch()) : null,
        task = includeContext ? CoordinationProtocol.ClampContext(settings.taskContext) : null
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

      QueueSend(request, JsonUtility.ToJson(parsed));
      return true;
    }

    private void QueueSend(CoordinationRequestHandle request, string json)
    {
      var task = SendAsync(request, json);
      lock (sendTasks)
      {
        sendTasks.Add(task);
      }

      _ = task.ContinueWith(completed =>
      {
        lock (sendTasks)
        {
          sendTasks.Remove(completed);
        }
      }, TaskScheduler.Default);
    }

    private async Task SendAsync(CoordinationRequestHandle request, string json)
    {
      try
      {
        await webSocketClient.SendAsync(json, connectionCancellation.Token)
          .ConfigureAwait(false);
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
      var removed = false;
      lock (pendingRequests)
      {
        removed = pendingRequests.Remove(request.RequestId);
      }
      if (removed)
      {
        dispatcher.Post(() => RequestSendFailed?.Invoke(
          new CoordinationRequestSendFailure(request, message)));
      }
    }

    private void OnSocketMessage(string json)
    {
      int generation;
      lock (socketMessageGateLock)
      {
        if (!socketMessagesAccepted)
        {
          return;
        }

        generation = socketMessageGeneration;
      }

      dispatcher.Post(() =>
      {
        lock (socketMessageGateLock)
        {
          if (socketMessagesAccepted && generation == socketMessageGeneration)
          {
            ApplySocketMessage(json);
          }
        }
      });
    }

    private void ApplySocketMessage(string json)
    {
      if (!CoordinationProtocol.TryParseServerEnvelope(json, out var envelope, out var error))
      {
        PublishTransportError("invalid_server_message", error);
        return;
      }

      if (envelope.type == "snapshot")
      {
        var serializedUtf8Bytes = Encoding.UTF8.GetByteCount(json);
        var status = snapshotAssembler.TryAdd(
          envelope, serializedUtf8Bytes, out var completed, out var assemblyError);
        if (status == CoordinationSnapshotAssemblyStatus.Rejected)
        {
          PublishTransportError(assemblyError, assemblyError);
          return;
        }

        if (status != CoordinationSnapshotAssemblyStatus.Completed)
        {
          return;
        }

        envelope = completed;
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
          currentConnectionId = envelope.connectionId ?? string.Empty;
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
      InvalidateSocketMessagesAndResetSnapshot();
      dispatcher.Post(() =>
      {
        DrainPendingRequests("The coordination socket closed.");
        session = null;
        currentConnectionId = string.Empty;
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

    private bool InvalidateSocketMessagesAndResetSnapshot()
    {
      lock (socketMessageGateLock)
      {
        var wasAcceptingMessages = socketMessagesAccepted;
        socketMessagesAccepted = false;
        socketMessageGeneration++;
        snapshotAssembler.Reset();
        return wasAcceptingMessages;
      }
    }

    private void EnableSocketMessages()
    {
      lock (socketMessageGateLock)
      {
        socketMessagesAccepted = true;
      }
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

    private async Task CloseSocketAsync()
    {
      using (var cancellation = new CancellationTokenSource(CloseTimeout))
      {
        await webSocketClient.CloseAsync(cancellation.Token).ConfigureAwait(false);
      }
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
      requestCredentials?.Invoke(OnCredentialsSaved);
    }

    private void OnCredentialsSaved()
    {
      if (shutdown || IsDisabled || State == CoordinationConnectionState.Connected)
      {
        return;
      }

      credentialUnavailable = false;
      hasPromptedForCredentials = false;
      EnsureConnectionCancellation();
      _ = StartConnectionAttempt();
    }

    private void DrainPendingRequests(string message)
    {
      CoordinationRequestHandle[] drained;
      lock (pendingRequests)
      {
        drained = new CoordinationRequestHandle[pendingRequests.Count];
        pendingRequests.Values.CopyTo(drained, 0);
        pendingRequests.Clear();
      }

      foreach (var request in drained)
      {
        RequestSendFailed?.Invoke(new CoordinationRequestSendFailure(request, message));
      }
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
