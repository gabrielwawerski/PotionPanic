using System;
using System.Collections.Generic;
using System.Linq;

namespace PotionPanic.Editor.Coordination
{
  public interface ICoordinationSaveService
  {
    event Action<CoordinationConnectionState> StateChanged;
    event Action<CoordinationServerEnvelope> SessionReady;
    event Action<CoordinationRequestCompletion> RequestCompleted;
    event Action<CoordinationRequestSendFailure> RequestSendFailed;

    CoordinationConnectionState State { get; }
    bool TryAcquireLease(string path, out CoordinationRequestHandle request);
    bool TryOverrideLease(string path, out CoordinationRequestHandle request);
  }

  public interface ICoordinationSaveInvoker
  {
    bool Save(IReadOnlyList<string> paths);
  }

  public interface ICoordinationSaveWarningLogger
  {
    void LogWarning(string message);
  }

  public sealed class CoordinationUncoordinatedSaveWarning
  {
    public IReadOnlyList<string> AffectedPaths { get; }
    public IReadOnlyList<CoordinationSavePathInfo> PathDetails { get; }

    internal CoordinationUncoordinatedSaveWarning(
      IReadOnlyList<CoordinationSavePathInfo> paths)
    {
      PathDetails = paths.ToArray();
      AffectedPaths = paths.Select(value => value.Path).ToArray();
    }
  }

  public interface ICoordinationUncoordinatedSaveState
  {
    event Action Changed;
    IReadOnlyList<CoordinationUncoordinatedSaveWarning> Warnings { get; }
  }

  public sealed class CoordinationUncoordinatedSaveState
    : ICoordinationUncoordinatedSaveState
  {
    private readonly CoordinationUncoordinatedSaveLedger ledger;

    public event Action Changed;
    public IReadOnlyList<CoordinationUncoordinatedSaveWarning> Warnings
    {
      get
      {
        var paths = ledger.Records.Select(record =>
          new CoordinationSavePathInfo(record.path, record.lastKnownOwner)).ToArray();
        return paths.Length == 0
          ? Array.Empty<CoordinationUncoordinatedSaveWarning>()
          : new[] { new CoordinationUncoordinatedSaveWarning(paths) };
      }
    }
    internal IReadOnlyList<CoordinationUncoordinatedSaveRecord> Records
      => ledger.Records;
    internal string PersistentError => ledger.PersistentError;

    public CoordinationUncoordinatedSaveState()
      : this(new CoordinationUncoordinatedSaveLedger(
        new CoordinationUncoordinatedSaveStore(),
        new SystemCoordinationClock()))
    {
    }

    internal CoordinationUncoordinatedSaveState(
      CoordinationUncoordinatedSaveLedger ledger)
    {
      this.ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
    }

    internal bool RecordSave(
      string path,
      CoordinationUncoordinatedSaveReason reason,
      string lastKnownOwner,
      string branch,
      string task)
    {
      var succeeded = ledger.RecordSave(
        path,
        reason,
        lastKnownOwner,
        branch,
        task);
      Changed?.Invoke();
      return succeeded;
    }

    public void ClearPath(string path)
    {
      var count = ledger.Records.Count;
      if (ledger.ReconcilePath(path) && ledger.Records.Count != count)
      {
        Changed?.Invoke();
      }
    }
  }

  public sealed class CoordinationSaveResumeCoordinator : IDisposable
  {
    private readonly ICoordinationSaveService service;
    private readonly CoordinationStateStore stateStore;
    private readonly CoordinationUncoordinatedSaveState warningState;
    private readonly ICoordinationSaveScheduler scheduler;
    private readonly ISaveConflictDialog conflictDialog;
    private readonly IUncoordinatedSavePrompt localSavePrompt;
    private readonly ICoordinationSaveInvoker saveInvoker;
    private readonly ICoordinationSaveWarningLogger warningLogger;
    private readonly ICoordinationGitContext gitContext;
    private readonly Func<string> taskProvider;
    private readonly ICoordinationNotificationSource notificationSource;
    private readonly TimeSpan requestTimeout;
    private readonly Dictionary<PendingSaveKey, PendingRequest> pendingRequests
      = new Dictionary<PendingSaveKey, PendingRequest>();
    private readonly Dictionary<string, PendingSaveKey> requestKeys
      = new Dictionary<string, PendingSaveKey>();
    private readonly HashSet<string> pendingPaths = new HashSet<string>();
    private readonly HashSet<string> resumeAuthorizations = new HashSet<string>();
    private CoordinationLocalIdentity localIdentity;
    private string authenticationDetail = string.Empty;
    private bool isEnabled;
    private bool isDisposed;

    internal CoordinationSaveResumeCoordinator(
      ICoordinationSaveService service,
      CoordinationStateStore stateStore,
      CoordinationUncoordinatedSaveState warningState,
      ICoordinationSaveScheduler scheduler,
      ISaveConflictDialog conflictDialog,
      IUncoordinatedSavePrompt localSavePrompt,
      ICoordinationSaveInvoker saveInvoker,
      ICoordinationSaveWarningLogger warningLogger,
      ICoordinationGitContext gitContext,
      Func<string> taskProvider,
      TimeSpan requestTimeout)
    {
      this.service = service ?? throw new ArgumentNullException(nameof(service));
      this.stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
      this.warningState = warningState
        ?? throw new ArgumentNullException(nameof(warningState));
      this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
      this.conflictDialog = conflictDialog
        ?? throw new ArgumentNullException(nameof(conflictDialog));
      this.localSavePrompt = localSavePrompt
        ?? throw new ArgumentNullException(nameof(localSavePrompt));
      this.saveInvoker = saveInvoker
        ?? throw new ArgumentNullException(nameof(saveInvoker));
      this.warningLogger = warningLogger
        ?? throw new ArgumentNullException(nameof(warningLogger));
      this.gitContext = gitContext ?? throw new ArgumentNullException(nameof(gitContext));
      this.taskProvider = taskProvider ?? throw new ArgumentNullException(nameof(taskProvider));
      notificationSource = service as ICoordinationNotificationSource;
      if (requestTimeout <= TimeSpan.Zero)
      {
        throw new ArgumentOutOfRangeException(
          nameof(requestTimeout),
          "The request timeout must be positive.");
      }

      this.requestTimeout = requestTimeout;
    }

    public void Enable()
    {
      ThrowIfDisposed();
      if (isEnabled)
      {
        return;
      }

      service.StateChanged += HandleStateChanged;
      service.SessionReady += HandleSessionReady;
      service.RequestCompleted += HandleRequestCompleted;
      service.RequestSendFailed += HandleRequestSendFailed;
      if (notificationSource != null)
      {
        notificationSource.ErrorReceived += HandleTransportError;
      }
      isEnabled = true;
    }

    public void Disable()
    {
      if (!isEnabled)
      {
        return;
      }

      service.StateChanged -= HandleStateChanged;
      service.SessionReady -= HandleSessionReady;
      service.RequestCompleted -= HandleRequestCompleted;
      service.RequestSendFailed -= HandleRequestSendFailed;
      if (notificationSource != null)
      {
        notificationSource.ErrorReceived -= HandleTransportError;
      }
      pendingRequests.Clear();
      requestKeys.Clear();
      pendingPaths.Clear();
      resumeAuthorizations.Clear();
      localIdentity = null;
      authenticationDetail = string.Empty;
      isEnabled = false;
    }

    public void Dispose()
    {
      Disable();
      isDisposed = true;
    }

    internal SavePathDecision EvaluatePath(string path)
    {
      if (!CoordinationPathMatcher.TryNormalize(path, out var normalizedPath))
      {
        return SavePathDecision.Allow;
      }

      var canonicalPath = CoordinationPathMatcher.ToCanonicalKey(normalizedPath);
      if (resumeAuthorizations.Remove(canonicalPath))
      {
        return SavePathDecision.Allow;
      }

      if (pendingPaths.Contains(canonicalPath))
      {
        return SavePathDecision.BlockPending;
      }

      return IsAuthoritativelyOwned(normalizedPath)
        ? SavePathDecision.Allow
        : SavePathDecision.BlockAndSchedule;
    }

    internal PreparedSave PrepareSave(IEnumerable<string> paths)
    {
      var normalizedPaths = new List<string>();
      foreach (var path in paths ?? Array.Empty<string>())
      {
        if (!CoordinationPathMatcher.TryNormalize(path, out var normalizedPath))
        {
          continue;
        }

        var canonicalPath = CoordinationPathMatcher.ToCanonicalKey(normalizedPath);
        if (pendingPaths.Add(canonicalPath))
        {
          normalizedPaths.Add(normalizedPath);
        }
      }

      return new PreparedSave(normalizedPaths);
    }

    internal void BeginPreparedSave(PreparedSave save)
    {
      if (!isEnabled || isDisposed || save == null)
      {
        return;
      }

      foreach (var path in save.Paths.ToArray())
      {
        if (!save.Contains(path))
        {
          continue;
        }

        if (IsAuthoritativelyOwned(path))
        {
          Resume(save, path);
          continue;
        }

        if (TryGetStateFallbackReason(service.State, out var fallbackReason))
        {
          QueueLocalFallback(save, fallbackReason, StateDetail(fallbackReason));
          return;
        }

        if (service.State != CoordinationConnectionState.Connected
          || !service.TryAcquireLease(path, out var request) || request == null)
        {
          CompletePath(save, path);
          continue;
        }

        TrackRequest(save, path, request);
      }
    }

    private void TrackRequest(
      PreparedSave save,
      string path,
      CoordinationRequestHandle request)
    {
      var key = new PendingSaveKey(request.RequestId, save.PathSetKey);
      pendingRequests[key] = new PendingRequest(save, path, request.Type);
      requestKeys[request.RequestId] = key;
      scheduler.PostAfter(requestTimeout, () => HandleTimeout(request.RequestId));
    }

    private void HandleSessionReady(CoordinationServerEnvelope envelope)
    {
      if (envelope == null || string.IsNullOrEmpty(envelope.developerId)
        || string.IsNullOrEmpty(envelope.connectionId))
      {
        localIdentity = null;
        return;
      }

      stateStore.ApplySessionReady(envelope);
      localIdentity = new CoordinationLocalIdentity(
        envelope.developerId,
        envelope.connectionId);
      authenticationDetail = string.Empty;
    }

    private void HandleStateChanged(CoordinationConnectionState state)
    {
      if (state != CoordinationConnectionState.Connected)
      {
        localIdentity = null;
      }

      if (!TryGetStateFallbackReason(state, out var fallbackReason))
      {
        return;
      }

      var pendingSaves = pendingRequests.Values
        .Select(value => value.Save)
        .Distinct()
        .ToArray();
      foreach (var save in pendingSaves)
      {
        QueueLocalFallback(save, fallbackReason, StateDetail(fallbackReason));
      }
    }

    private void HandleTransportError(CoordinationServerEnvelope envelope)
    {
      if (envelope?.code == "authentication_failed")
      {
        authenticationDetail = "The saved developer credential was rejected.";
      }
    }

    private void HandleRequestCompleted(CoordinationRequestCompletion completion)
    {
      if (completion?.Request == null
        || !TryTakeRequest(completion.Request.RequestId, out var pending))
      {
        return;
      }

      stateStore.ApplyRequestCompletion(completion);
      if (IsAuthoritativelyOwned(pending.Path))
      {
        Resume(pending.Save, pending.Path);
        return;
      }

      if (!completion.IsStaleReplay && completion.Response?.type == "lease.denied"
        && pending.RequestType == "lease.acquire"
        && HasRemoteOwner(pending.Path))
      {
        QueueConflictDialog(pending.Save, pending.Path);
        return;
      }

      CompletePath(pending.Save, pending.Path);
    }

    private void HandleRequestSendFailed(CoordinationRequestSendFailure failure)
    {
      if (failure?.Request == null
        || !TryTakeRequest(failure.Request.RequestId, out var pending))
      {
        return;
      }

      if (pending.RequestType == "lease.override")
      {
        QueueLocalFallback(
          pending.Save,
          CoordinationUncoordinatedSaveReason.OverrideTransportFailure,
          failure.Message);
        return;
      }

      scheduler.Post(() => ResolveAcquireSendFailure(pending));
    }

    private void ResolveAcquireSendFailure(PendingRequest pending)
    {
      if (isDisposed || !pending.Save.Contains(pending.Path))
      {
        return;
      }

      if (service.State == CoordinationConnectionState.Offline
        || service.State == CoordinationConnectionState.Reconnecting)
      {
        var reason = service.State == CoordinationConnectionState.Offline
          ? CoordinationUncoordinatedSaveReason.Offline
          : CoordinationUncoordinatedSaveReason.Reconnecting;
        QueueLocalFallback(pending.Save, reason, StateDetail(reason));
        return;
      }

      CompletePath(pending.Save, pending.Path);
    }

    private void HandleTimeout(string requestId)
    {
      if (isDisposed || !TryTakeRequest(requestId, out var pending))
      {
        return;
      }

      QueueLocalFallback(
        pending.Save,
        CoordinationUncoordinatedSaveReason.RequestTimeout,
        "No editing-lease response arrived before the request timeout.");
    }

    private bool TryTakeRequest(string requestId, out PendingRequest pending)
    {
      pending = null;
      if (string.IsNullOrEmpty(requestId)
        || !requestKeys.TryGetValue(requestId, out var key)
        || !pendingRequests.TryGetValue(key, out pending))
      {
        return false;
      }

      requestKeys.Remove(requestId);
      pendingRequests.Remove(key);
      return true;
    }

    private void QueueConflictDialog(PreparedSave save, string path)
    {
      scheduler.Post(() =>
      {
        if (isDisposed || !save.Contains(path))
        {
          return;
        }

        var action = conflictDialog.Show(CreatePathInfo(new[] { path }));
        if (action != SaveConflictAction.OverrideAndSave)
        {
          CompletePath(save, path);
          return;
        }

        if (!service.TryOverrideLease(path, out var request) || request == null)
        {
          if (CanOfferOverrideFallback())
          {
            QueueLocalFallback(
              save,
              CoordinationUncoordinatedSaveReason.OverrideTransportFailure,
              "The override request could not be sent.");
          }
          else
          {
            CompletePath(save, path);
          }
          return;
        }

        TrackRequest(save, path, request);
      });
    }

    private void QueueLocalFallback(
      PreparedSave save,
      CoordinationUncoordinatedSaveReason reason,
      string detail)
    {
      if (save == null || save.FallbackQueued)
      {
        return;
      }

      save.FallbackQueued = true;
      RemoveRequests(save);
      scheduler.Post(() => OfferLocalFallback(save, reason, detail));
    }

    private void OfferLocalFallback(
      PreparedSave save,
      CoordinationUncoordinatedSaveReason reason,
      string detail)
    {
      if (isDisposed)
      {
        return;
      }

      if (IsConnectionStateReason(reason)
        && service.State != CoordinationConnectionState.Offline
        && service.State != CoordinationConnectionState.Reconnecting
        && service.State != CoordinationConnectionState.AuthenticationFailed
        && service.State != CoordinationConnectionState.Disabled)
      {
        save.FallbackQueued = false;
        if (service.State == CoordinationConnectionState.Connected)
        {
          BeginPreparedSave(save);
        }
        else
        {
          CompleteAll(save);
        }
        return;
      }

      var paths = save.Paths.Where(save.Contains).ToArray();
      var pathInfo = CreatePathInfo(paths);
      var request = new CoordinationUncoordinatedSaveRequest
      {
        Reason = reason,
        AssetPaths = paths,
        Detail = detail ?? string.Empty
      };
      if (pathInfo.Count == 0 || !localSavePrompt.ChooseLocalSave(request)
        || !localSavePrompt.ConfirmLocalSave(request))
      {
        CompleteAll(save);
        return;
      }

      var savedPathInfo = new List<CoordinationSavePathInfo>();
      foreach (var path in paths)
      {
        CompletePath(save, path);
        var canonicalPath = Canonical(path);
        resumeAuthorizations.Add(canonicalPath);
        try
        {
          if (saveInvoker.Save(new[] { path }))
          {
            savedPathInfo.Add(pathInfo.Single(value => value.Path == path));
          }
        }
        finally
        {
          resumeAuthorizations.Remove(canonicalPath);
        }
      }

      if (savedPathInfo.Count == 0)
      {
        return;
      }

      var branch = gitContext.GetBranch() ?? string.Empty;
      var task = taskProvider() ?? string.Empty;
      foreach (var path in savedPathInfo)
      {
        warningState.RecordSave(
          path.Path,
          reason,
          OwnerMetadataFor(path.Path),
          branch,
          task);
      }
      warningLogger.LogWarning(
        "Saved locally without coordination: "
          + string.Join(", ", savedPathInfo.Select(value => value.Path)));
    }

    private void Resume(PreparedSave save, string path)
    {
      if (!IsAuthoritativelyOwned(path))
      {
        CompletePath(save, path);
        return;
      }

      CompletePath(save, path);
      var canonicalPath = Canonical(path);
      resumeAuthorizations.Add(canonicalPath);
      try
      {
        saveInvoker.Save(new[] { path });
      }
      finally
      {
        resumeAuthorizations.Remove(canonicalPath);
      }
    }

    private bool CanOfferOverrideFallback()
    {
      return service.State == CoordinationConnectionState.Connected
        || service.State == CoordinationConnectionState.Offline
        || service.State == CoordinationConnectionState.Reconnecting;
    }

    private bool IsAuthoritativelyOwned(string path)
    {
      return service.State == CoordinationConnectionState.Connected
        && localIdentity != null && stateStore.TryGetLease(path, out var lease)
        && lease.mode == "editing"
        && lease.developerId == localIdentity.DeveloperId
        && lease.connectionId == localIdentity.ConnectionId;
    }

    private bool HasRemoteOwner(string path)
    {
      return stateStore.TryGetLease(path, out var lease)
        && !string.IsNullOrEmpty(lease.developerId)
        && (localIdentity == null || lease.developerId != localIdentity.DeveloperId
          || lease.connectionId != localIdentity.ConnectionId);
    }

    private IReadOnlyList<CoordinationSavePathInfo> CreatePathInfo(
      IEnumerable<string> paths)
    {
      return paths
        .Select(path => new CoordinationSavePathInfo(path, OwnerFor(path)))
        .ToArray();
    }

    private string OwnerFor(string path)
    {
      if (!stateStore.TryGetLease(path, out var lease))
      {
        return "No owner known";
      }

      return !string.IsNullOrWhiteSpace(lease.displayName)
        ? lease.displayName
        : !string.IsNullOrWhiteSpace(lease.developerId)
          ? lease.developerId
          : "No owner known";
    }

    private string OwnerMetadataFor(string path)
    {
      if (!stateStore.TryGetLease(path, out var lease))
      {
        return string.Empty;
      }

      return !string.IsNullOrWhiteSpace(lease.displayName)
        ? lease.displayName
        : lease.developerId ?? string.Empty;
    }

    private string StateDetail(CoordinationUncoordinatedSaveReason reason)
    {
      return reason == CoordinationUncoordinatedSaveReason.AuthenticationFailed
        ? authenticationDetail
        : string.Empty;
    }

    private static bool IsConnectionStateReason(
      CoordinationUncoordinatedSaveReason reason)
    {
      return reason == CoordinationUncoordinatedSaveReason.Offline
        || reason == CoordinationUncoordinatedSaveReason.Reconnecting
        || reason == CoordinationUncoordinatedSaveReason.AuthenticationFailed;
    }

    private static bool TryGetStateFallbackReason(
      CoordinationConnectionState state,
      out CoordinationUncoordinatedSaveReason reason)
    {
      switch (state)
      {
        case CoordinationConnectionState.Disabled:
          reason = CoordinationUncoordinatedSaveReason.Manual;
          return true;
        case CoordinationConnectionState.Offline:
          reason = CoordinationUncoordinatedSaveReason.Offline;
          return true;
        case CoordinationConnectionState.Reconnecting:
          reason = CoordinationUncoordinatedSaveReason.Reconnecting;
          return true;
        case CoordinationConnectionState.AuthenticationFailed:
          reason = CoordinationUncoordinatedSaveReason.AuthenticationFailed;
          return true;
        default:
          reason = default;
          return false;
      }
    }

    private void RemoveRequests(PreparedSave save)
    {
      var requestsForSave = pendingRequests
        .Where(value => ReferenceEquals(value.Value.Save, save))
        .ToArray();
      foreach (var pair in requestsForSave)
      {
        pendingRequests.Remove(pair.Key);
        requestKeys.Remove(pair.Key.RequestId);
      }
    }

    private void CompleteAll(PreparedSave save)
    {
      foreach (var path in save.Paths.ToArray())
      {
        CompletePath(save, path);
      }
    }

    private void CompletePath(PreparedSave save, string path)
    {
      if (save.Remove(path))
      {
        pendingPaths.Remove(Canonical(path));
      }
    }

    private static string Canonical(string path)
    {
      CoordinationPathMatcher.TryNormalize(path, out var normalizedPath);
      return CoordinationPathMatcher.ToCanonicalKey(normalizedPath);
    }

    private void ThrowIfDisposed()
    {
      if (isDisposed)
      {
        throw new ObjectDisposedException(nameof(CoordinationSaveResumeCoordinator));
      }
    }

    internal enum SavePathDecision
    {
      Allow,
      BlockPending,
      BlockAndSchedule
    }

    internal sealed class PreparedSave
    {
      private readonly HashSet<string> remainingPaths;

      public IReadOnlyList<string> Paths { get; }
      public string PathSetKey { get; }
      public bool FallbackQueued { get; set; }

      public PreparedSave(IReadOnlyList<string> paths)
      {
        Paths = paths.ToArray();
        remainingPaths = new HashSet<string>(Paths.Select(Canonical));
        PathSetKey = string.Join("\n", remainingPaths.OrderBy(value => value));
      }

      public bool Contains(string path) => remainingPaths.Contains(Canonical(path));
      public bool Remove(string path) => remainingPaths.Remove(Canonical(path));
    }

    private readonly struct PendingSaveKey : IEquatable<PendingSaveKey>
    {
      public string RequestId { get; }
      private string PathSetKey { get; }

      public PendingSaveKey(string requestId, string pathSetKey)
      {
        RequestId = requestId;
        PathSetKey = pathSetKey;
      }

      public bool Equals(PendingSaveKey other)
      {
        return RequestId == other.RequestId && PathSetKey == other.PathSetKey;
      }

      public override bool Equals(object value)
      {
        return value is PendingSaveKey other && Equals(other);
      }

      public override int GetHashCode()
      {
        unchecked
        {
          return ((RequestId != null ? RequestId.GetHashCode() : 0) * 397)
            ^ (PathSetKey != null ? PathSetKey.GetHashCode() : 0);
        }
      }
    }

    private sealed class PendingRequest
    {
      public PreparedSave Save { get; }
      public string Path { get; }
      public string RequestType { get; }

      public PendingRequest(PreparedSave save, string path, string requestType)
      {
        Save = save;
        Path = path;
        RequestType = requestType;
      }
    }
  }
}
