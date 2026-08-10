using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace PotionPanic.Editor.Coordination
{
  public interface ICoordinationUserSettingsStore
  {
    void Save(CoordinationUserSettings settings);
  }

  public sealed class UnityCoordinationUserSettingsStore
    : ICoordinationUserSettingsStore
  {
    public void Save(CoordinationUserSettings settings)
    {
      CoordinationUserSettings.Save(settings);
    }
  }

  public interface ICoordinationClipboard
  {
    void SetText(string value);
  }

  public sealed class UnityCoordinationClipboard : ICoordinationClipboard
  {
    public void SetText(string value)
    {
      GUIUtility.systemCopyBuffer = value ?? string.Empty;
    }
  }

  public interface ICoordinationWindowPathSource
  {
    bool TryGetActiveStagePath(out string path);
    bool TryGetProjectSelectionPath(out string path);
  }

  public interface ICoordinationOverrideConfirmation
  {
    bool Confirm(string path, string owner);
  }

  public interface ICoordinationWindowConfirmation
  {
    bool ConfirmManualMode(string message);
    bool ConfirmReconciliation(string path, string message);
    bool ConfirmForgetCredentials(string message);
  }

  internal enum CoordinationMode
  {
    Coordinated,
    Manual,
  }

  internal enum CoordinationTargetSource
  {
    ActiveStage,
    ProjectSelection,
    ManualPath,
  }

  internal enum CoordinationDataFreshness
  {
    WaitingForSnapshot,
    Live,
    Stale,
    Unavailable,
  }

  internal enum CoordinationPrimaryAction
  {
    None,
    Reserve,
    ReleaseEditingLease,
    CancelReservation,
  }

  public enum CoordinationWindowRowKind
  {
    Presence,
    EditingLease,
    Reservation
  }

  public sealed class CoordinationWindowRow
  {
    public CoordinationWindowRowKind Kind { get; }
    public string Path { get; }
    public string Owner { get; }
    public string DeveloperId { get; }
    public string Branch { get; }
    public string Task { get; }
    public string ExpiresAt { get; }
    public bool IsLocal { get; }

    public CoordinationWindowRow(
      CoordinationWindowRowKind kind,
      string path,
      string owner,
      string developerId,
      string branch,
      string task,
      string expiresAt,
      bool isLocal)
    {
      Kind = kind;
      Path = path ?? string.Empty;
      Owner = owner ?? string.Empty;
      DeveloperId = developerId ?? string.Empty;
      Branch = branch ?? string.Empty;
      Task = task ?? string.Empty;
      ExpiresAt = expiresAt ?? string.Empty;
      IsLocal = isLocal;
    }
  }

  internal sealed class CoordinationOutstandingWarning
  {
    public string Path { get; }
    public string FirstSavedAtUtc { get; }
    public string LatestSavedAtUtc { get; }
    public int SaveCount { get; }
    public string Reason { get; }
    public string LastKnownOwner { get; }
    public string Branch { get; }
    public string Task { get; }
    public string Error { get; }

    public CoordinationOutstandingWarning(
      CoordinationUncoordinatedSaveRecord record,
      string error)
    {
      Path = record.path ?? string.Empty;
      FirstSavedAtUtc = record.firstSavedAtUtc ?? string.Empty;
      LatestSavedAtUtc = record.latestSavedAtUtc ?? string.Empty;
      SaveCount = record.saveCount;
      Reason = record.reason ?? string.Empty;
      LastKnownOwner = record.lastKnownOwner ?? string.Empty;
      Branch = record.branch ?? string.Empty;
      Task = record.task ?? string.Empty;
      Error = error ?? string.Empty;
    }
  }

  public sealed class CoordinationWindowViewModel : IDisposable
  {
    private readonly ICoordinationWindowService service;
    private readonly CoordinationStateStore stateStore;
    private readonly CoordinationUncoordinatedSaveState warningState;
    private readonly CoordinationUserSettings settings;
    private readonly ICoordinationUserSettingsStore settingsStore;
    private readonly CoordinatedPathRule[] rules;
    private readonly ICoordinationClipboard clipboard;
    private readonly ICoordinationWindowPathSource pathSource;
    private readonly ICoordinationOverrideConfirmation overrideConfirmation;
    private readonly ICoordinationWindowConfirmation confirmation;
    private bool isEnabled;
    private string selectedPath = string.Empty;
    private string pathSourceMessage = string.Empty;
    private CoordinationTargetSource targetSource = CoordinationTargetSource.ActiveStage;
    private string expandedRowKey = string.Empty;

    public event Action Changed;
    public string Branch { get; }
    public CoordinationConnectionState ConnectionState => service.State;
    public string Identity => string.IsNullOrEmpty(service.DeveloperId)
      ? "Not authenticated"
      : DisplayOwner(service.DisplayName, service.DeveloperId)
        + " (" + service.DeveloperId + ")";
    public bool CanEditDisabled => service.IsSupportedPlatform;
    public bool IsDisabled => !service.IsSupportedPlatform || settings.disabled;
    internal CoordinationMode Mode => IsDisabled
      ? CoordinationMode.Manual
      : CoordinationMode.Coordinated;
    internal CoordinationTargetSource TargetSource => targetSource;
    internal CoordinationDataFreshness Freshness => GetFreshness();
    internal CoordinationPrimaryAction PrimaryAction => GetPrimaryAction();
    public string ConnectionLabel => service.State == CoordinationConnectionState.Disabled
      ? "Manual"
      : service.State.ToString();
    public IReadOnlyList<CoordinationWindowRow> Presence => PresenceRows();
    public IReadOnlyList<CoordinationWindowRow> EditingLeases => LeaseRows("editing");
    public IReadOnlyList<CoordinationWindowRow> Reservations => LeaseRows("reserved");
    public IReadOnlyList<CoordinationUncoordinatedSaveWarning> Warnings
      => warningState.Warnings;
    internal IReadOnlyList<CoordinationOutstandingWarning> OutstandingWarnings
      => warningState.Records.Select(record => new CoordinationOutstandingWarning(
        record, warningState.PersistentError)).ToArray();
    internal string WarningStoreError => warningState.PersistentError ?? string.Empty;

    public string TaskContext
    {
      get => settings.taskContext ?? string.Empty;
      set
      {
        var next = CoordinationProtocol.ClampContext(value);
        if (settings.taskContext == next)
        {
          return;
        }

        settings.taskContext = next;
        settingsStore.Save(settings);
        Changed?.Invoke();
      }
    }

    public string SelectedPath
    {
      get => selectedPath;
      set
      {
        SetSelectedPath(value, CoordinationTargetSource.ManualPath);
      }
    }

    public bool CanReconnect => service.IsSupportedPlatform && !settings.disabled
      && service.State != CoordinationConnectionState.Connected
      && service.State != CoordinationConnectionState.Reconnecting;
    public bool CanReserve => GetPrimaryAction() == CoordinationPrimaryAction.Reserve;
    public bool CanRelease => CanSendForSelectedPath()
      && IsLocallyOwnedEditing(SelectedLease());
    public bool CanCancelReservation => CanSendForSelectedPath()
      && IsLocallyOwnedReservation(SelectedLease());
    public bool CanOverride => CanSendForSelectedPath() && IsRemotelyOwned(SelectedLease());
    public bool CanCopyCanonicalPath => TrySelectedPath(out _);
    public bool CanForgetCredentials => service.IsSupportedPlatform;
    public string TargetHelpText => BuildTargetHelpText();

    public CoordinationWindowViewModel(
      ICoordinationWindowService service,
      CoordinationStateStore stateStore,
      CoordinationUncoordinatedSaveState warningState,
      CoordinationUserSettings settings,
      ICoordinationUserSettingsStore settingsStore,
      IEnumerable<CoordinatedPathRule> rules,
      ICoordinationGitContext gitContext,
      ICoordinationClipboard clipboard,
      ICoordinationWindowPathSource pathSource,
      ICoordinationOverrideConfirmation overrideConfirmation,
      ICoordinationWindowConfirmation confirmation)
    {
      this.service = service ?? throw new ArgumentNullException(nameof(service));
      this.stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
      this.warningState = warningState ?? throw new ArgumentNullException(nameof(warningState));
      this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
      this.settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
      this.rules = rules?.ToArray() ?? throw new ArgumentNullException(nameof(rules));
      this.clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
      this.pathSource = pathSource ?? throw new ArgumentNullException(nameof(pathSource));
      this.overrideConfirmation = overrideConfirmation
        ?? throw new ArgumentNullException(nameof(overrideConfirmation));
      this.confirmation = confirmation
        ?? throw new ArgumentNullException(nameof(confirmation));
      if (gitContext == null)
      {
        throw new ArgumentNullException(nameof(gitContext));
      }

      Branch = CoordinationProtocol.ClampContext(gitContext.GetBranch());
    }

    public void Enable()
    {
      if (isEnabled)
      {
        return;
      }

      service.StateChanged += HandleChanged;
      stateStore.Changed += HandleChanged;
      warningState.Changed += HandleChanged;
      isEnabled = true;
      RefreshActiveStage();
      Changed?.Invoke();
    }

    public void Disable()
    {
      if (!isEnabled)
      {
        return;
      }

      service.StateChanged -= HandleChanged;
      stateStore.Changed -= HandleChanged;
      warningState.Changed -= HandleChanged;
      isEnabled = false;
    }

    public void Dispose()
    {
      Disable();
    }

    public void SetDisabled(bool disabled)
    {
      SetMode(disabled ? CoordinationMode.Manual : CoordinationMode.Coordinated);
    }

    internal bool SetMode(CoordinationMode mode)
    {
      if (!service.IsSupportedPlatform || Mode == mode)
      {
        return false;
      }
      if (mode == CoordinationMode.Manual
        && !confirmation.ConfirmManualMode(ManualConfirmationMessage))
      {
        return false;
      }

      settings.disabled = mode == CoordinationMode.Manual;
      settingsStore.Save(settings);
      _ = ObserveAsync(service.SetDisabledAsync(settings.disabled));
      Changed?.Invoke();
      return true;
    }

    public bool Reconnect()
    {
      if (!CanReconnect)
      {
        return false;
      }

      _ = ObserveAsync(service.ConnectAsync());
      return true;
    }

    public bool Reserve()
    {
      return CanReserve && TrySelectedPath(out var path)
        && service.TryReserveLease(path, out _);
    }

    public bool Release()
    {
      return CanRelease && TrySelectedPath(out var path)
        && service.TryReleaseLease(path, out _);
    }

    public bool CancelReservation()
    {
      return CanCancelReservation && TrySelectedPath(out var path)
        && service.TryCancelReservation(path, out _);
    }

    public bool Override()
    {
      if (!TrySelectedPath(out var path)
        || !TryGetRemoteLeaseForOverride(path, out var lease)
        || !overrideConfirmation.Confirm(path,
          DisplayOwner(lease.displayName, lease.developerId)))
      {
        return false;
      }

      return TryGetRemoteLeaseForOverride(path, out var current)
        && IsSameLease(lease, current)
        && service.TryOverrideLease(path, out _);
    }

    public bool UseActiveStage()
    {
      return FollowActiveStage();
    }

    public bool UseProjectSelection()
    {
      return pathSource.TryGetProjectSelectionPath(out var path)
        ? TrySetPathFromSource(path, CoordinationTargetSource.ProjectSelection,
          "The Project selection is not an asset under Assets/.")
        : SetPathSourceFailure("The Project selection is not an asset under Assets/.");
    }

    public bool FollowActiveStage()
    {
      targetSource = CoordinationTargetSource.ActiveStage;
      return RefreshActiveStage();
    }

    public bool RefreshActiveStage()
    {
      if (targetSource != CoordinationTargetSource.ActiveStage)
      {
        return false;
      }

      if (pathSource.TryGetActiveStagePath(out var path))
      {
        var accepted = TrySetPathFromSource(path, CoordinationTargetSource.ActiveStage,
          "The active stage is not a saved asset under Assets/.");
        if (!accepted)
        {
          ClearActionTarget();
        }
        return accepted;
      }

      ClearActionTarget();
      return SetPathSourceFailure("The active stage is not a saved asset under Assets/.");
    }

    public void SelectRow(CoordinationWindowRow row)
    {
      if (row == null)
      {
        throw new ArgumentNullException(nameof(row));
      }
      var key = RowKey(row);
      expandedRowKey = expandedRowKey == key ? string.Empty : key;
      Changed?.Invoke();
    }

    public bool IsSelected(CoordinationWindowRow row)
    {
      return IsExpanded(row);
    }

    public bool IsExpanded(CoordinationWindowRow row)
      => row != null && expandedRowKey == RowKey(row);

    public bool CanReleaseRow(CoordinationWindowRow row)
    {
      return row != null && row.Kind == CoordinationWindowRowKind.EditingLease
        && CanActOnRow(row, out var lease) && IsLocallyOwnedEditing(lease);
    }

    public bool CanCancelReservationRow(CoordinationWindowRow row)
    {
      return row != null && row.Kind == CoordinationWindowRowKind.Reservation
        && CanActOnRow(row, out var lease) && IsLocallyOwnedReservation(lease);
    }

    public bool CanOverrideRow(CoordinationWindowRow row)
    {
      return row != null && row.Kind != CoordinationWindowRowKind.Presence
        && CanActOnRow(row, out var lease)
        && lease.mode == (row.Kind == CoordinationWindowRowKind.EditingLease
          ? "editing" : "reserved") && IsRemotelyOwned(lease);
    }

    public bool Release(CoordinationWindowRow row)
    {
      if (!CanReleaseRow(row))
      {
        return false;
      }
      return TryLeaseForRow(row, out var lease) && IsLocallyOwnedEditing(lease)
        && service.TryReleaseLease(row.Path, out _);
    }

    public bool CancelReservation(CoordinationWindowRow row)
    {
      if (!CanCancelReservationRow(row))
      {
        return false;
      }
      return TryLeaseForRow(row, out var lease) && IsLocallyOwnedReservation(lease)
        && service.TryCancelReservation(row.Path, out _);
    }

    public bool Override(CoordinationWindowRow row)
    {
      if (!CanOverrideRow(row))
      {
        return false;
      }
      if (!CoordinationPathMatcher.TryNormalize(row.Path, out var path)
        || !TryGetRemoteLeaseForOverride(path, out var lease)
        || !overrideConfirmation.Confirm(path,
          DisplayOwner(lease.displayName, lease.developerId)))
      {
        return false;
      }

      return TryGetRemoteLeaseForOverride(path, out var current)
        && IsSameLease(lease, current)
        && service.TryOverrideLease(path, out _);
    }

    public bool CopyPath(CoordinationWindowRow row)
    {
      if (row == null)
      {
        return false;
      }
      if (!CoordinationPathMatcher.TryNormalize(row.Path, out var path))
      {
        return false;
      }
      clipboard.SetText(path);
      return true;
    }

    public bool CopyCanonicalPath()
    {
      if (!TrySelectedPath(out var path))
      {
        return false;
      }

      clipboard.SetText(path);
      return true;
    }

    public bool ForgetCredentials()
    {
      if (!CanForgetCredentials)
      {
        return false;
      }

      if (!confirmation.ConfirmForgetCredentials(ForgetCredentialsConfirmationMessage))
      {
        return false;
      }

      _ = ObserveAsync(service.ForgetCredentialsAsync());
      return true;
    }

    public bool PerformPrimaryAction()
    {
      switch (PrimaryAction)
      {
        case CoordinationPrimaryAction.Reserve:
          return Reserve();
        case CoordinationPrimaryAction.ReleaseEditingLease:
          return Release();
        case CoordinationPrimaryAction.CancelReservation:
          return CancelReservation();
        default:
          return false;
      }
    }

    internal bool MarkReconciled(CoordinationOutstandingWarning warning)
    {
      if (warning == null || !confirmation.ConfirmReconciliation(
        warning.Path, ReconciliationConfirmationMessage))
      {
        return false;
      }

      var before = warningState.Records.Count;
      warningState.ClearPath(warning.Path);
      return warningState.Records.Count < before;
    }

    private IReadOnlyList<CoordinationWindowRow> PresenceRows()
    {
      return stateStore.GetAllPresence()
        .OrderBy(value => value.displayPath ?? value.path, StringComparer.OrdinalIgnoreCase)
        .Select(value => Row(
          CoordinationWindowRowKind.Presence,
          value.displayPath ?? value.path,
          value.displayName,
          value.developerId,
          value.branch,
          value.task,
          value.expiresAt,
          value.developerId == service.DeveloperId
            && value.connectionId == service.ConnectionId))
        .ToArray();
    }

    private IReadOnlyList<CoordinationWindowRow> LeaseRows(string mode)
    {
      return stateStore.GetAllLeases()
        .Where(value => value.mode == mode)
        .OrderBy(value => value.displayPath ?? value.path, StringComparer.OrdinalIgnoreCase)
        .Select(value => Row(
          mode == "editing"
            ? CoordinationWindowRowKind.EditingLease
            : CoordinationWindowRowKind.Reservation,
          value.displayPath ?? value.path,
          value.displayName,
          value.developerId,
          value.branch,
          value.task,
          value.expiresAt,
          mode == "editing"
            ? IsLocallyOwnedEditing(value)
            : value.developerId == service.DeveloperId))
        .ToArray();
    }

    private static CoordinationWindowRow Row(
      CoordinationWindowRowKind kind,
      string path,
      string displayName,
      string developerId,
      string branch,
      string task,
      string expiresAt,
      bool isLocal)
    {
      return new CoordinationWindowRow(
        kind,
        path,
        DisplayOwner(displayName, developerId),
        developerId,
        branch,
        task,
        expiresAt,
        isLocal);
    }

    private CoordinationLeaseRecord SelectedLease()
    {
      return TrySelectedPath(out var path) && stateStore.TryGetLease(path, out var lease)
        ? lease
        : null;
    }

    private bool IsLocallyOwnedEditing(CoordinationLeaseRecord lease)
    {
      return lease != null && lease.mode == "editing"
        && lease.developerId == service.DeveloperId
        && lease.connectionId == service.ConnectionId;
    }

    private bool IsLocallyOwnedReservation(CoordinationLeaseRecord lease)
    {
      return lease != null && lease.mode == "reserved"
        && lease.developerId == service.DeveloperId;
    }

    private bool IsRemotelyOwned(CoordinationLeaseRecord lease)
    {
      return lease != null && !string.IsNullOrEmpty(lease.developerId)
        && (lease.developerId != service.DeveloperId
          || lease.mode == "editing" && lease.connectionId != service.ConnectionId);
    }

    private bool CanSendForSelectedPath()
    {
      return TrySelectedPath(out var path) && CanSendForPath(path);
    }

    private bool CanSendForPath(string path)
    {
      return service.State == CoordinationConnectionState.Connected
        && Mode == CoordinationMode.Coordinated
        && Freshness == CoordinationDataFreshness.Live
        && rules.Any(rule => CoordinationPathMatcher.Matches(rule, path));
    }

    private bool TrySelectedCoordinatedPath(out string normalizedPath)
    {
      if (!TrySelectedPath(out normalizedPath))
      {
        return false;
      }

      var path = normalizedPath;
      return rules.Any(rule => CoordinationPathMatcher.Matches(rule, path));
    }

    private bool TrySelectedPath(out string normalizedPath)
    {
      return CoordinationPathMatcher.TryNormalize(selectedPath, out normalizedPath)
        && normalizedPath.StartsWith("Assets/", StringComparison.Ordinal);
    }

    private bool CanActOnRow(
      CoordinationWindowRow row,
      out CoordinationLeaseRecord lease)
    {
      lease = null;
      return TryLeaseForRow(row, out lease)
        && CanSendForPath(lease.displayPath ?? lease.path);
    }

    private bool TryLeaseForRow(
      CoordinationWindowRow row,
      out CoordinationLeaseRecord lease)
    {
      lease = null;
      return row != null && CoordinationPathMatcher.TryNormalize(row.Path, out var path)
        && rules.Any(rule => CoordinationPathMatcher.Matches(rule, path))
        && stateStore.TryGetLease(path, out lease);
    }

    private bool TrySetPathFromSource(
      string path,
      CoordinationTargetSource source,
      string failureMessage)
    {
      if (!CoordinationPathMatcher.TryNormalize(path, out var normalized)
        || !normalized.StartsWith("Assets/", StringComparison.Ordinal))
      {
        return SetPathSourceFailure(failureMessage);
      }

      SetSelectedPath(normalized, source);
      return true;
    }

    private void ClearActionTarget()
    {
      if (string.IsNullOrEmpty(selectedPath))
      {
        return;
      }

      selectedPath = string.Empty;
      Changed?.Invoke();
    }

    private void SetSelectedPath(string value, CoordinationTargetSource source)
    {
      var raw = value ?? string.Empty;
      var next = CoordinationPathMatcher.TryNormalize(raw, out var normalized)
        ? normalized
        : raw;
      var changed = selectedPath != next || targetSource != source;
      selectedPath = next;
      targetSource = source;
      pathSourceMessage = string.Empty;
      if (changed)
      {
        Changed?.Invoke();
      }
    }

    private bool SetPathSourceFailure(string message)
    {
      if (pathSourceMessage == message)
      {
        return false;
      }
      pathSourceMessage = message;
      Changed?.Invoke();
      return false;
    }

    private string BuildTargetHelpText()
    {
      if (!string.IsNullOrEmpty(pathSourceMessage))
      {
        return pathSourceMessage;
      }
      if (string.IsNullOrWhiteSpace(selectedPath))
      {
        return "Choose the active stage, the Project selection, or a manual path.";
      }
      if (!TrySelectedPath(out var path))
      {
        return "Choose an asset under Assets/.";
      }
      if (!rules.Any(rule => CoordinationPathMatcher.Matches(rule, path)))
      {
        return "This path is not covered by a coordination rule.";
      }
      if (!service.IsSupportedPlatform)
      {
        return "Coordination actions are available only in the Windows editor.";
      }
      if (Mode == CoordinationMode.Manual)
      {
        return "Manual mode does not allow claim changes. Copy path remains available.";
      }
      if (service.State != CoordinationConnectionState.Connected)
      {
        return "Reconnect to change claims. Copy path remains available.";
      }
      if (Freshness == CoordinationDataFreshness.WaitingForSnapshot)
      {
        return "Waiting for team data. Claim changes remain unavailable.";
      }

      var lease = SelectedLease();
      if (lease == null)
      {
        return "No current claim. Reserve is available.";
      }
      if (IsLocallyOwnedEditing(lease))
      {
        return "You own this editing lease. Release editing lease is available.";
      }
      if (IsLocallyOwnedReservation(lease))
      {
        return "You own this reservation. Cancel reservation is available.";
      }
      return "Claimed by " + DisplayOwner(lease.displayName, lease.developerId)
        + ". Override requires confirmation.";
    }

    private void HandleChanged() => Changed?.Invoke();
    private void HandleChanged(CoordinationConnectionState _) => Changed?.Invoke();

    private static string DisplayOwner(string displayName, string developerId)
    {
      return string.IsNullOrEmpty(displayName) ? developerId ?? string.Empty : displayName;
    }

    private static string RowKey(CoordinationWindowRow row)
    {
      return row.Kind + ":" + CoordinationPathMatcher.ToCanonicalKey(row.Path);
    }

    private CoordinationDataFreshness GetFreshness()
    {
      if (Mode == CoordinationMode.Manual)
      {
        return HasRetainedRows()
          ? CoordinationDataFreshness.Stale
          : CoordinationDataFreshness.Unavailable;
      }
      if (service.State == CoordinationConnectionState.Connected)
      {
        return stateStore.HasAuthoritativeSnapshot
          ? CoordinationDataFreshness.Live
          : CoordinationDataFreshness.WaitingForSnapshot;
      }

      return HasRetainedRows()
        ? CoordinationDataFreshness.Stale
        : CoordinationDataFreshness.Unavailable;
    }

    private bool HasRetainedRows()
    {
      return stateStore.GetAllPresence().Count > 0 || stateStore.GetAllLeases().Count > 0;
    }

    private bool TryGetRemoteLeaseForOverride(
      string path,
      out CoordinationLeaseRecord lease)
    {
      lease = null;
      return CanSendForPath(path)
        && stateStore.TryGetLease(path, out lease)
        && IsRemotelyOwned(lease);
    }

    private static bool IsSameLease(
      CoordinationLeaseRecord expected,
      CoordinationLeaseRecord current)
    {
      return expected != null && current != null
        && expected.leaseId == current.leaseId
        && expected.mode == current.mode
        && expected.developerId == current.developerId
        && expected.displayName == current.displayName
        && expected.connectionId == current.connectionId;
    }

    private CoordinationPrimaryAction GetPrimaryAction()
    {
      if (!CanSendForSelectedPath())
      {
        return CoordinationPrimaryAction.None;
      }

      var lease = SelectedLease();
      if (lease == null)
      {
        return CoordinationPrimaryAction.Reserve;
      }
      if (IsLocallyOwnedEditing(lease))
      {
        return CoordinationPrimaryAction.ReleaseEditingLease;
      }
      return IsLocallyOwnedReservation(lease)
        ? CoordinationPrimaryAction.CancelReservation
        : CoordinationPrimaryAction.None;
    }

    private const string ManualConfirmationMessage =
      "Manual mode closes the live connection. Connection-owned presence and editing "
      + "leases will be released. Reservations may remain until released or expired. "
      + "Every coordinated-asset save will require two confirmations and create a warning.";
    private const string ReconciliationConfirmationMessage =
      "Mark this warning reconciled? This does not merge files or update server history.";
    private const string ForgetCredentialsConfirmationMessage =
      "Forget the saved developer credential? The live connection will close and "
      + "you will need to enter the credential again before connecting.";

    private static async Task ObserveAsync(Task task)
    {
      try
      {
        await task;
      }
      catch (Exception exception)
      {
        Debug.LogException(exception);
      }
    }
  }
}
