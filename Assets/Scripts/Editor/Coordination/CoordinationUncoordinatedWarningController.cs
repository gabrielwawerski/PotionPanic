using System;
using System.Linq;

namespace PotionPanic.Editor.Coordination
{
  public sealed class CoordinationUncoordinatedWarningController : IDisposable
  {
    private readonly CoordinationStageLifecycleAdapter lifecycle;
    private readonly ICoordinationWarningService service;
    private readonly CoordinationStateStore stateStore;
    private readonly CoordinationUncoordinatedSaveState warnings;
    private CoordinationLocalIdentity localIdentity;
    private bool isEnabled;

    public CoordinationUncoordinatedWarningController(
      CoordinationStageLifecycleAdapter lifecycle,
      ICoordinationWarningService service,
      CoordinationStateStore stateStore,
      CoordinationUncoordinatedSaveState warnings)
    {
      this.lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
      this.service = service ?? throw new ArgumentNullException(nameof(service));
      this.stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
      this.warnings = warnings ?? throw new ArgumentNullException(nameof(warnings));
    }

    public void Enable()
    {
      if (isEnabled)
      {
        return;
      }

      lifecycle.Transitioned += HandleTransition;
      service.StateChanged += HandleStateChanged;
      service.SessionReady += HandleSessionReady;
      stateStore.Changed += ClearAuthoritativelyOwnedPaths;
      isEnabled = true;
    }

    public void Disable()
    {
      if (!isEnabled)
      {
        return;
      }

      lifecycle.Transitioned -= HandleTransition;
      service.StateChanged -= HandleStateChanged;
      service.SessionReady -= HandleSessionReady;
      stateStore.Changed -= ClearAuthoritativelyOwnedPaths;
      localIdentity = null;
      isEnabled = false;
    }

    public void Dispose()
    {
      Disable();
    }

    private void HandleTransition(CoordinationStageTransition transition)
    {
      if (transition?.Kind == CoordinationStageTransitionKind.Closed)
      {
        warnings.ClearPath(transition.Stage.Path);
      }
    }

    private void HandleStateChanged(CoordinationConnectionState state)
    {
      if (state != CoordinationConnectionState.Connected)
      {
        localIdentity = null;
      }
    }

    private void HandleSessionReady(CoordinationServerEnvelope envelope)
    {
      localIdentity = envelope != null && !string.IsNullOrEmpty(envelope.developerId)
        && !string.IsNullOrEmpty(envelope.connectionId)
        ? new CoordinationLocalIdentity(envelope.developerId, envelope.connectionId)
        : null;
      ClearAuthoritativelyOwnedPaths();
    }

    private void ClearAuthoritativelyOwnedPaths()
    {
      if (service.State != CoordinationConnectionState.Connected || localIdentity == null)
      {
        return;
      }

      var paths = warnings.Warnings
        .SelectMany(value => value.AffectedPaths)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
      foreach (var path in paths)
      {
        if (stateStore.TryGetLease(path, out var lease) && lease.mode == "editing"
          && lease.developerId == localIdentity.DeveloperId
          && lease.connectionId == localIdentity.ConnectionId)
        {
          warnings.ClearPath(path);
        }
      }
    }
  }
}
