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

  public sealed class CoordinationWindowViewModel : IDisposable
  {
    private readonly ICoordinationWindowService service;
    private readonly CoordinationStateStore stateStore;
    private readonly ICoordinationUncoordinatedSaveState warningState;
    private readonly CoordinationUserSettings settings;
    private readonly ICoordinationUserSettingsStore settingsStore;
    private readonly CoordinatedPathRule[] rules;
    private readonly ICoordinationClipboard clipboard;
    private readonly ICoordinationWindowPathSource pathSource;
    private readonly ICoordinationOverrideConfirmation overrideConfirmation;
    private bool isEnabled;
    private string selectedPath = string.Empty;
    private string pathSourceMessage = string.Empty;

    public event Action Changed;
    public string Branch { get; }
    public CoordinationConnectionState ConnectionState => service.State;
    public string Identity => string.IsNullOrEmpty(service.DeveloperId)
      ? "Not authenticated"
      : DisplayOwner(service.DisplayName, service.DeveloperId)
        + " (" + service.DeveloperId + ")";
    public bool CanEditDisabled => service.IsSupportedPlatform;
    public bool IsDisabled => !service.IsSupportedPlatform || settings.disabled;
    public IReadOnlyList<CoordinationWindowRow> Presence => PresenceRows();
    public IReadOnlyList<CoordinationWindowRow> EditingLeases => LeaseRows("editing");
    public IReadOnlyList<CoordinationWindowRow> Reservations => LeaseRows("reserved");
    public IReadOnlyList<CoordinationUncoordinatedSaveWarning> Warnings
      => warningState.Warnings;

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
        var raw = value ?? string.Empty;
        var next = CoordinationPathMatcher.TryNormalize(raw, out var normalized)
          ? normalized
          : raw;
        pathSourceMessage = string.Empty;
        if (selectedPath == next)
        {
          return;
        }

        selectedPath = next;
        Changed?.Invoke();
      }
    }

    public bool CanReconnect => service.IsSupportedPlatform && !settings.disabled
      && service.State != CoordinationConnectionState.Connected
      && service.State != CoordinationConnectionState.Reconnecting;
    public bool CanReserve => CanSendForSelectedPath() && SelectedLease() == null;
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
      ICoordinationUncoordinatedSaveState warningState,
      CoordinationUserSettings settings,
      ICoordinationUserSettingsStore settingsStore,
      IEnumerable<CoordinatedPathRule> rules,
      ICoordinationGitContext gitContext,
      ICoordinationClipboard clipboard,
      ICoordinationWindowPathSource pathSource,
      ICoordinationOverrideConfirmation overrideConfirmation)
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
      if (!service.IsSupportedPlatform || settings.disabled == disabled)
      {
        return;
      }

      settings.disabled = disabled;
      settingsStore.Save(settings);
      _ = ObserveAsync(service.SetDisabledAsync(disabled));
      Changed?.Invoke();
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
      var lease = SelectedLease();
      return CanOverride && TrySelectedPath(out var path)
        && overrideConfirmation.Confirm(path, DisplayOwner(lease.displayName, lease.developerId))
        && service.TryOverrideLease(path, out _);
    }

    public bool UseActiveStage()
    {
      return pathSource.TryGetActiveStagePath(out var path)
        ? TrySetPathFromSource(path, "The active stage is not a saved asset under Assets/.")
        : SetPathSourceFailure("The active stage is not a saved asset under Assets/.");
    }

    public bool UseProjectSelection()
    {
      return pathSource.TryGetProjectSelectionPath(out var path)
        ? TrySetPathFromSource(path, "The Project selection is not an asset under Assets/.")
        : SetPathSourceFailure("The Project selection is not an asset under Assets/.");
    }

    public void SelectRow(CoordinationWindowRow row)
    {
      if (row == null)
      {
        throw new ArgumentNullException(nameof(row));
      }
      SelectedPath = row.Path;
    }

    public bool IsSelected(CoordinationWindowRow row)
    {
      return row != null && TrySelectedPath(out var selected)
        && CoordinationPathMatcher.TryNormalize(row.Path, out var candidate)
        && CoordinationPathMatcher.ToCanonicalKey(selected)
          == CoordinationPathMatcher.ToCanonicalKey(candidate);
    }

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
      SelectRow(row);
      return Release();
    }

    public bool CancelReservation(CoordinationWindowRow row)
    {
      if (!CanCancelReservationRow(row))
      {
        return false;
      }
      SelectRow(row);
      return CancelReservation();
    }

    public bool Override(CoordinationWindowRow row)
    {
      if (!CanOverrideRow(row))
      {
        return false;
      }
      SelectRow(row);
      return Override();
    }

    public bool CopyPath(CoordinationWindowRow row)
    {
      if (row == null)
      {
        return false;
      }
      SelectRow(row);
      return CopyCanonicalPath();
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

      _ = ObserveAsync(service.ForgetCredentialsAsync());
      return true;
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
      return service.State == CoordinationConnectionState.Connected
        && !IsDisabled && TrySelectedCoordinatedPath(out _);
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
      return service.State == CoordinationConnectionState.Connected && !IsDisabled
        && TryLeaseForRow(row, out lease);
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

    private bool TrySetPathFromSource(string path, string failureMessage)
    {
      if (!CoordinationPathMatcher.TryNormalize(path, out var normalized)
        || !normalized.StartsWith("Assets/", StringComparison.Ordinal))
      {
        return SetPathSourceFailure(failureMessage);
      }

      SelectedPath = normalized;
      return true;
    }

    private bool SetPathSourceFailure(string message)
    {
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
        return "Choose a row, the active stage, the Project selection, or an advanced path.";
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
      if (settings.disabled)
      {
        return "Coordination is disabled. Copy path remains available.";
      }
      if (service.State != CoordinationConnectionState.Connected)
      {
        return "Reconnect to change claims. Copy path remains available.";
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
