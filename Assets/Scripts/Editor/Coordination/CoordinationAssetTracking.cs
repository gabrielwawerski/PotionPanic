using System;
using System.Collections.Generic;

namespace PotionPanic.Editor.Coordination
{
  public enum CoordinationStageKind
  {
    Scene,
    Prefab
  }

  public sealed class CoordinationStageInfo
  {
    public CoordinationStageKind Kind { get; }
    public string Path { get; }
    public bool IsDirty { get; }

    public CoordinationStageInfo(CoordinationStageKind kind, string path, bool isDirty)
    {
      if (!IsNormalizedAssetPath(kind, path))
      {
        throw new ArgumentException("The stage path must be a normalized Unity Assets path.",
          nameof(path));
      }

      Kind = kind;
      Path = path;
      IsDirty = isDirty;
    }

    private static bool IsNormalizedAssetPath(CoordinationStageKind kind, string path)
    {
      if (!CoordinationPathMatcher.TryNormalize(path, out var normalizedPath)
        || !string.Equals(path, normalizedPath, StringComparison.Ordinal)
        || !path.StartsWith("Assets/", StringComparison.Ordinal))
      {
        return false;
      }

      return kind == CoordinationStageKind.Scene
        ? path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)
        : kind == CoordinationStageKind.Prefab
          && path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase);
    }
  }

  public enum CoordinationLeaseOwnership
  {
    None,
    OwnedEditing,
    OtherEditing,
    Reserved
  }

  public sealed class CoordinationLocalIdentity
  {
    public string DeveloperId { get; }
    public string ConnectionId { get; }

    public CoordinationLocalIdentity(string developerId, string connectionId)
    {
      if (string.IsNullOrEmpty(developerId))
      {
        throw new ArgumentException("The local developer ID is required.", nameof(developerId));
      }

      if (string.IsNullOrEmpty(connectionId))
      {
        throw new ArgumentException("The local connection ID is required.", nameof(connectionId));
      }

      DeveloperId = developerId;
      ConnectionId = connectionId;
    }
  }

  public sealed class CoordinatedStage
  {
    public CoordinationStageInfo Stage { get; }
    public CoordinatedPathRule Rule { get; }
    public bool IsExclusive => Rule.exclusive;
    public CoordinationLeaseOwnership LeaseOwnership { get; private set; }

    internal CoordinatedStage(CoordinationStageInfo stage, CoordinatedPathRule rule)
    {
      Stage = stage;
      Rule = rule;
    }

    public void ApplyLease(CoordinationLeaseRecord lease, CoordinationLocalIdentity localIdentity)
    {
      if (lease == null || !MatchesStagePath(lease.path))
      {
        LeaseOwnership = CoordinationLeaseOwnership.None;
        return;
      }

      if (lease.mode == "reserved")
      {
        LeaseOwnership = CoordinationLeaseOwnership.Reserved;
        return;
      }

      LeaseOwnership = lease.mode == "editing" && localIdentity != null
        && lease.developerId == localIdentity.DeveloperId && lease.connectionId == localIdentity.ConnectionId
        ? CoordinationLeaseOwnership.OwnedEditing
        : CoordinationLeaseOwnership.OtherEditing;
    }

    private bool MatchesStagePath(string path)
    {
      return CoordinationPathMatcher.TryNormalize(path, out var normalizedPath)
        && CoordinationPathMatcher.ToCanonicalKey(normalizedPath)
          == CoordinationPathMatcher.ToCanonicalKey(Stage.Path);
    }
  }

  public static class CoordinationStageEvaluator
  {
    public static bool TryEvaluate(
      CoordinationStageInfo stage,
      IEnumerable<CoordinatedPathRule> rules,
      out CoordinatedStage evaluation)
    {
      evaluation = null;
      if (stage == null || rules == null)
      {
        return false;
      }

      foreach (var rule in rules)
      {
        if (CoordinationPathMatcher.Matches(rule, stage.Path))
        {
          evaluation = new CoordinatedStage(stage, rule);
          return true;
        }
      }

      return false;
    }
  }

  public interface ICoordinationAssetService
  {
    event Action<CoordinationServerEnvelope> SessionReady;
    event Action<CoordinationServerEnvelope> SnapshotReceived;
    event Action<CoordinationServerEnvelope> PresenceReceived;
    event Action<CoordinationServerEnvelope> PresenceRemoved;
    event Action<CoordinationServerEnvelope> LeaseResultReceived;
    event Action<CoordinationRequestCompletion> RequestCompleted;
    event Action<CoordinationRequestSendFailure> RequestSendFailed;

    bool TryOpenPresence(string path, out CoordinationRequestHandle request);
    bool TryClosePresence(string path, out CoordinationRequestHandle request);
    bool TryAcquireLease(string path, out CoordinationRequestHandle request);
    bool TryReleaseLease(string path, out CoordinationRequestHandle request);
  }

  public sealed class CoordinationAssetTracker : IDisposable
  {
    private readonly CoordinationStageLifecycleAdapter lifecycle;
    private readonly ICoordinationAssetService service;
    private readonly CoordinatedPathRule[] rules;
    private readonly Dictionary<string, CoordinatedStage> activeStages
      = new Dictionary<string, CoordinatedStage>();
    private readonly Dictionary<string, long> activeActivationIds
      = new Dictionary<string, long>();
    private readonly Dictionary<string, PendingAcquire> pendingAcquires
      = new Dictionary<string, PendingAcquire>();
    private readonly List<string> activeStageOrder = new List<string>();
    private CoordinationLocalIdentity localIdentity;
    private long nextActivationId;
    private bool isDisposed;

    public CoordinationStateStore StateStore { get; }
    public bool IsEnabled { get; private set; }

    public CoordinationAssetTracker(
      CoordinationStageLifecycleAdapter lifecycle,
      ICoordinationAssetService service,
      IEnumerable<CoordinatedPathRule> rules,
      CoordinationStateStore stateStore = null)
    {
      this.lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
      this.service = service ?? throw new ArgumentNullException(nameof(service));
      if (rules == null)
      {
        throw new ArgumentNullException(nameof(rules));
      }
      this.rules = new List<CoordinatedPathRule>(rules).ToArray();
      StateStore = stateStore ?? new CoordinationStateStore();
    }

    public void Enable()
    {
      ThrowIfDisposed();
      if (IsEnabled)
      {
        return;
      }

      IsEnabled = true;
      lifecycle.Transitioned += HandleTransition;
      service.SessionReady += HandleSessionReady;
      service.SnapshotReceived += HandleSnapshot;
      service.PresenceReceived += HandlePresenceReceived;
      service.PresenceRemoved += HandlePresenceRemoved;
      service.LeaseResultReceived += HandleLeaseResult;
      service.RequestCompleted += HandleRequestCompleted;
      service.RequestSendFailed += HandleRequestSendFailed;
      lifecycle.Enable();
    }

    public void Disable()
    {
      if (!IsEnabled)
      {
        return;
      }

      lifecycle.Disable();
      lifecycle.Transitioned -= HandleTransition;
      service.SessionReady -= HandleSessionReady;
      service.SnapshotReceived -= HandleSnapshot;
      service.PresenceReceived -= HandlePresenceReceived;
      service.PresenceRemoved -= HandlePresenceRemoved;
      service.LeaseResultReceived -= HandleLeaseResult;
      service.RequestCompleted -= HandleRequestCompleted;
      service.RequestSendFailed -= HandleRequestSendFailed;
      activeStages.Clear();
      activeActivationIds.Clear();
      pendingAcquires.Clear();
      activeStageOrder.Clear();
      localIdentity = null;
      IsEnabled = false;
    }

    public void Dispose()
    {
      Disable();
      isDisposed = true;
    }

    public void ReleaseOwnedCoordination()
    {
      foreach (var key in activeStageOrder)
      {
        var stage = activeStages[key];
        service.TryClosePresence(stage.Stage.Path, out _);
        if (!StateStore.TryGetLease(stage.Stage.Path, out var lease))
        {
          continue;
        }

        stage.ApplyLease(lease, localIdentity);
        if (stage.LeaseOwnership == CoordinationLeaseOwnership.OwnedEditing)
        {
          service.TryReleaseLease(stage.Stage.Path, out _);
        }
      }
    }

    private void HandleTransition(CoordinationStageTransition transition)
    {
      if (transition == null || transition.Stage == null)
      {
        return;
      }

      var key = StageKey(transition.Stage);
      if (transition.Kind == CoordinationStageTransitionKind.Closed)
      {
        CloseStage(key);
        return;
      }

      if (!CoordinationStageEvaluator.TryEvaluate(transition.Stage, rules, out var stage))
      {
        return;
      }

      var wasTracked = activeStages.ContainsKey(key);
      activeStages[key] = stage;
      if (!wasTracked)
      {
        activeStageOrder.Add(key);
        nextActivationId += 1;
        activeActivationIds[key] = nextActivationId;
        service.TryOpenPresence(stage.Stage.Path, out _);
      }

      if (stage.IsExclusive && stage.Stage.IsDirty
        && (transition.Kind == CoordinationStageTransitionKind.Opened
          || transition.Kind == CoordinationStageTransitionKind.Dirtied))
      {
        TryAcquireLease(key, stage);
      }
    }

    private void CloseStage(string key)
    {
      if (!activeStages.TryGetValue(key, out var stage))
      {
        return;
      }

      activeStages.Remove(key);
      activeActivationIds.Remove(key);
      activeStageOrder.Remove(key);
      service.TryClosePresence(stage.Stage.Path, out _);
      if (StateStore.TryGetLease(stage.Stage.Path, out var lease))
      {
        stage.ApplyLease(lease, localIdentity);
        if (stage.LeaseOwnership == CoordinationLeaseOwnership.OwnedEditing)
        {
          service.TryReleaseLease(stage.Stage.Path, out _);
        }
      }
    }

    private void HandleSessionReady(CoordinationServerEnvelope envelope)
    {
      StateStore.ApplySessionReady(envelope);
      localIdentity = TryCreateIdentity(envelope);
      foreach (var key in activeStageOrder)
      {
        var stage = activeStages[key];
        service.TryOpenPresence(stage.Stage.Path, out _);
        if (stage.IsExclusive && stage.Stage.IsDirty)
        {
          TryAcquireLease(key, stage);
        }
      }
    }

    private void HandleSnapshot(CoordinationServerEnvelope envelope)
      => StateStore.ApplySnapshot(envelope);
    private void HandlePresenceReceived(CoordinationServerEnvelope envelope)
      => StateStore.ApplyPresenceUpdate(envelope);
    private void HandlePresenceRemoved(CoordinationServerEnvelope envelope)
      => StateStore.ApplyPresenceRemoval(envelope);
    private void HandleLeaseResult(CoordinationServerEnvelope envelope)
      => StateStore.ApplyLeaseResult(envelope, false);
    private void HandleRequestCompleted(CoordinationRequestCompletion completion)
    {
      PendingAcquire pendingAcquire = null;
      if (completion?.Request != null && completion.Request.Type == "lease.acquire")
      {
        pendingAcquires.TryGetValue(completion.Request.RequestId, out pendingAcquire);
        pendingAcquires.Remove(completion.Request.RequestId);
      }

      if (!StateStore.ApplyRequestCompletion(completion) || pendingAcquire == null
        || completion.Response.type != "lease.granted")
      {
        return;
      }

      if (activeActivationIds.TryGetValue(pendingAcquire.StageKey, out var activationId))
      {
        if (activationId == pendingAcquire.ActivationId)
        {
          return;
        }

        var activeStage = activeStages[pendingAcquire.StageKey];
        if (activeStage.IsExclusive && activeStage.Stage.IsDirty)
        {
          return;
        }
      }

      var path = pendingAcquire.Path;
      if (!StateStore.TryGetLease(path, out var lease)
        || lease.mode != "editing" || localIdentity == null
        || lease.developerId != localIdentity.DeveloperId
        || lease.connectionId != localIdentity.ConnectionId)
      {
        return;
      }

      service.TryReleaseLease(path, out _);
    }

    private void HandleRequestSendFailed(CoordinationRequestSendFailure failure)
    {
      if (failure?.Request != null && failure.Request.Type == "lease.acquire"
        && !string.IsNullOrEmpty(failure.Request.RequestId))
      {
        pendingAcquires.Remove(failure.Request.RequestId);
      }
    }

    private void TryAcquireLease(string key, CoordinatedStage stage)
    {
      if (!activeActivationIds.TryGetValue(key, out var activationId)
        || !service.TryAcquireLease(stage.Stage.Path, out var request)
        || request == null || string.IsNullOrEmpty(request.RequestId))
      {
        return;
      }

      pendingAcquires[request.RequestId]
        = new PendingAcquire(key, activationId, stage.Stage.Path);
    }

    private sealed class PendingAcquire
    {
      public string StageKey { get; }
      public long ActivationId { get; }
      public string Path { get; }

      public PendingAcquire(string stageKey, long activationId, string path)
      {
        StageKey = stageKey;
        ActivationId = activationId;
        Path = path;
      }
    }

    private static CoordinationLocalIdentity TryCreateIdentity(
      CoordinationServerEnvelope envelope)
    {
      if (envelope == null || string.IsNullOrEmpty(envelope.developerId)
        || string.IsNullOrEmpty(envelope.connectionId))
      {
        return null;
      }

      return new CoordinationLocalIdentity(envelope.developerId, envelope.connectionId);
    }

    private static string StageKey(CoordinationStageInfo stage)
    {
      return stage.Kind + ":" + CoordinationPathMatcher.ToCanonicalKey(stage.Path);
    }

    private void ThrowIfDisposed()
    {
      if (isDisposed)
      {
        throw new ObjectDisposedException(nameof(CoordinationAssetTracker));
      }
    }
  }

  public sealed class CoordinationStateStore
  {
    private readonly Dictionary<string, CoordinationPresenceRecord> presence
      = new Dictionary<string, CoordinationPresenceRecord>();
    private readonly Dictionary<string, CoordinationLeaseRecord> leases
      = new Dictionary<string, CoordinationLeaseRecord>();

    public long NewestStateVersion { get; private set; } = -1;
    public bool HasAuthoritativeSnapshot { get; private set; }
    public event Action Changed;

    public bool ApplySessionReady(CoordinationServerEnvelope envelope)
    {
      if (!CanApply(envelope, "session.ready"))
      {
        return false;
      }

      presence.Clear();
      leases.Clear();
      HasAuthoritativeSnapshot = false;
      NewestStateVersion = envelope.stateVersion;
      Changed?.Invoke();
      return true;
    }

    public bool ApplySnapshot(CoordinationServerEnvelope envelope)
    {
      if (!CanApply(envelope, "snapshot"))
      {
        return false;
      }

      presence.Clear();
      leases.Clear();
      foreach (var record in envelope.presence ?? Array.Empty<CoordinationPresenceRecord>())
      {
        AddPresence(record);
      }
      foreach (var record in envelope.leases ?? Array.Empty<CoordinationLeaseRecord>())
      {
        AddLease(record);
      }

      NewestStateVersion = envelope.stateVersion;
      HasAuthoritativeSnapshot = true;
      Changed?.Invoke();
      return true;
    }

    public bool ApplyPresenceUpdate(CoordinationServerEnvelope envelope)
    {
      if (!CanApply(envelope, "presence.updated"))
      {
        return false;
      }

      foreach (var record in envelope.presence ?? Array.Empty<CoordinationPresenceRecord>())
      {
        AddPresence(record);
      }

      NewestStateVersion = envelope.stateVersion;
      Changed?.Invoke();
      return true;
    }

    public bool ApplyPresenceRemoval(CoordinationServerEnvelope envelope)
    {
      if (!CanApply(envelope, "presence.removed")
        || !TryCanonicalPath(envelope.path, out var path)
        || string.IsNullOrEmpty(envelope.connectionId))
      {
        return false;
      }

      presence.Remove(PresenceKey(path, envelope.connectionId));
      NewestStateVersion = envelope.stateVersion;
      Changed?.Invoke();
      return true;
    }

    public bool ApplyLeaseUpdate(CoordinationServerEnvelope envelope)
    {
      return ApplyLeaseEnvelope(envelope, false);
    }

    public bool ApplyLeaseResult(CoordinationServerEnvelope envelope, bool isStaleReplay)
    {
      return ApplyLeaseEnvelope(envelope, isStaleReplay);
    }

    public bool ApplyRequestCompletion(CoordinationRequestCompletion completion)
    {
      if (completion == null || completion.IsStaleReplay || completion.Response == null)
      {
        return false;
      }

      switch (completion.Response.type)
      {
        case "snapshot":
          return ApplySnapshot(completion.Response);
        case "presence.updated":
          return ApplyPresenceUpdate(completion.Response);
        case "presence.removed":
          return ApplyPresenceRemoval(completion.Response);
        case "lease.granted":
        case "lease.denied":
        case "lease.updated":
        case "lease.released":
        case "lease.overridden":
          return ApplyLeaseResult(completion.Response, false);
        default:
          return false;
      }
    }

    public bool TryGetLease(string path, out CoordinationLeaseRecord lease)
    {
      lease = null;
      return TryCanonicalPath(path, out var canonicalPath)
        && leases.TryGetValue(canonicalPath, out lease);
    }

    public IReadOnlyCollection<CoordinationPresenceRecord> GetPresence(string path)
    {
      var records = new List<CoordinationPresenceRecord>();
      if (!TryCanonicalPath(path, out var canonicalPath))
      {
        return records;
      }

      foreach (var pair in presence)
      {
        if (PresencePath(pair.Key) == canonicalPath)
        {
          records.Add(pair.Value);
        }
      }

      return records;
    }

    public IReadOnlyCollection<CoordinationPresenceRecord> GetAllPresence()
    {
      return new List<CoordinationPresenceRecord>(presence.Values);
    }

    public IReadOnlyCollection<CoordinationLeaseRecord> GetAllLeases()
    {
      return new List<CoordinationLeaseRecord>(leases.Values);
    }

    private bool ApplyLeaseEnvelope(CoordinationServerEnvelope envelope, bool isStaleReplay)
    {
      if (isStaleReplay || !CanApplyLease(envelope))
      {
        return false;
      }

      switch (envelope.type)
      {
        case "lease.granted":
        case "lease.updated":
        case "lease.overridden":
          AddLease(envelope.lease);
          break;
        case "lease.denied":
          if (envelope.currentLease == null)
          {
            RemoveLease(envelope.path);
          }
          else
          {
            AddLease(envelope.currentLease);
          }
          break;
        case "lease.released":
          RemoveLease(envelope.path);
          break;
        default:
          return false;
      }

      NewestStateVersion = envelope.stateVersion;
      Changed?.Invoke();
      return true;
    }

    private bool CanApply(CoordinationServerEnvelope envelope, string expectedType)
    {
      return envelope != null && envelope.type == expectedType
        && envelope.stateVersion >= NewestStateVersion;
    }

    private bool CanApplyLease(CoordinationServerEnvelope envelope)
    {
      return envelope != null && envelope.stateVersion >= NewestStateVersion;
    }

    private void AddPresence(CoordinationPresenceRecord record)
    {
      if (record != null && !string.IsNullOrEmpty(record.connectionId)
        && TryCanonicalPath(record.path, out var path))
      {
        presence[PresenceKey(path, record.connectionId)] = record;
      }
    }

    private void AddLease(CoordinationLeaseRecord record)
    {
      if (record != null && TryCanonicalPath(record.path, out var path))
      {
        leases[path] = record;
      }
    }

    private void RemoveLease(string path)
    {
      if (TryCanonicalPath(path, out var canonicalPath))
      {
        leases.Remove(canonicalPath);
      }
    }

    private static bool TryCanonicalPath(string path, out string canonicalPath)
    {
      canonicalPath = null;
      return CoordinationPathMatcher.TryNormalize(path, out var normalizedPath)
        && (canonicalPath = CoordinationPathMatcher.ToCanonicalKey(normalizedPath)) != null;
    }

    private static string PresenceKey(string path, string connectionId) => path + "\n" + connectionId;
    private static string PresencePath(string key) => key.Substring(0, key.IndexOf('\n'));
  }
}
