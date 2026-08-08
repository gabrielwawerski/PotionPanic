using System;

namespace PotionPanic.Editor.Coordination
{
  public enum CoordinationNotificationKind
  {
    Claim,
    Conflict,
    Override,
    Reservation,
    AuthenticationFailure,
    ProlongedDisconnect
  }

  public sealed class CoordinationNotification
  {
    public CoordinationNotificationKind Kind { get; }
    public string Message { get; }

    public CoordinationNotification(CoordinationNotificationKind kind, string message)
    {
      Kind = kind;
      Message = message ?? string.Empty;
    }
  }

  public interface ICoordinationNotificationSink
  {
    void Publish(CoordinationNotification notification);
  }

  public interface ICoordinationClock
  {
    DateTimeOffset UtcNow { get; }
  }

  public sealed class SystemCoordinationClock : ICoordinationClock
  {
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
  }

  public sealed class CoordinationNotificationController : IDisposable
  {
    private readonly ICoordinationNotificationSource source;
    private readonly ICoordinationNotificationSink sink;
    private readonly ICoordinationClock clock;
    private readonly TimeSpan prolongedDisconnect;
    private DateTimeOffset? disconnectedAt;
    private bool disconnectPublished;
    private bool isEnabled;

    public CoordinationNotificationController(
      ICoordinationNotificationSource source,
      ICoordinationNotificationSink sink,
      ICoordinationClock clock,
      TimeSpan prolongedDisconnect)
    {
      this.source = source ?? throw new ArgumentNullException(nameof(source));
      this.sink = sink ?? throw new ArgumentNullException(nameof(sink));
      this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
      if (prolongedDisconnect <= TimeSpan.Zero)
      {
        throw new ArgumentOutOfRangeException(nameof(prolongedDisconnect));
      }

      this.prolongedDisconnect = prolongedDisconnect;
    }

    public void Enable()
    {
      if (isEnabled)
      {
        return;
      }

      source.StateChanged += HandleStateChanged;
      source.LeaseResultReceived += HandleLease;
      source.ErrorReceived += HandleError;
      isEnabled = true;
      TrackConnectionState(source.State);
    }

    public void Disable()
    {
      if (!isEnabled)
      {
        return;
      }

      source.StateChanged -= HandleStateChanged;
      source.LeaseResultReceived -= HandleLease;
      source.ErrorReceived -= HandleError;
      disconnectedAt = null;
      disconnectPublished = false;
      isEnabled = false;
    }

    public void Dispose()
    {
      Disable();
    }

    public void Tick()
    {
      if (!isEnabled || disconnectPublished || !disconnectedAt.HasValue
        || clock.UtcNow - disconnectedAt.Value < prolongedDisconnect)
      {
        return;
      }

      disconnectPublished = true;
      sink.Publish(new CoordinationNotification(
        CoordinationNotificationKind.ProlongedDisconnect,
        "Coordination has been disconnected for "
          + Math.Ceiling(prolongedDisconnect.TotalMinutes) + " minutes."));
    }

    private void HandleStateChanged(CoordinationConnectionState state)
    {
      TrackConnectionState(state);
      if (state == CoordinationConnectionState.AuthenticationFailed)
      {
        sink.Publish(new CoordinationNotification(
          CoordinationNotificationKind.AuthenticationFailure,
          "Coordination authentication failed. Check or forget the stored credentials."));
      }
    }

    private void TrackConnectionState(CoordinationConnectionState state)
    {
      if (state == CoordinationConnectionState.Connected
        || state == CoordinationConnectionState.Disabled
        || state == CoordinationConnectionState.AuthenticationFailed)
      {
        disconnectedAt = null;
        disconnectPublished = false;
        return;
      }

      if (!disconnectedAt.HasValue)
      {
        disconnectedAt = clock.UtcNow;
      }
    }

    private void HandleLease(CoordinationServerEnvelope envelope)
    {
      if (!TryCreateLeaseNotification(envelope, out var notification))
      {
        return;
      }

      sink.Publish(notification);
    }

    private static bool TryCreateLeaseNotification(
      CoordinationServerEnvelope envelope,
      out CoordinationNotification notification)
    {
      notification = null;
      if (envelope == null)
      {
        return false;
      }

      var lease = envelope.lease ?? envelope.currentLease;
      var path = lease?.displayPath ?? envelope.path ?? string.Empty;
      var owner = lease == null
        ? string.Empty
        : string.IsNullOrEmpty(lease.displayName) ? lease.developerId : lease.displayName;
      switch (envelope.type)
      {
        case "lease.denied":
          notification = new CoordinationNotification(
            CoordinationNotificationKind.Conflict,
            "Coordination conflict for " + path + OwnerSuffix(owner));
          return true;
        case "lease.overridden":
          notification = new CoordinationNotification(
            CoordinationNotificationKind.Override,
            "Coordination ownership was overridden for " + path + OwnerSuffix(owner));
          return true;
        case "lease.granted":
          if (lease?.mode == "reserved")
          {
            notification = new CoordinationNotification(
              CoordinationNotificationKind.Reservation,
              path + " was reserved" + OwnerSuffix(owner));
            return true;
          }
          if (lease?.mode == "editing")
          {
            notification = new CoordinationNotification(
              CoordinationNotificationKind.Claim,
              path + " was claimed" + OwnerSuffix(owner));
            return true;
          }
          return false;
        case "lease.updated":
          if (lease?.mode == "reserved")
          {
            notification = new CoordinationNotification(
              CoordinationNotificationKind.Reservation,
              path + " was reserved" + OwnerSuffix(owner));
            return true;
          }
          return false;
        default:
          return false;
      }
    }

    private static string OwnerSuffix(string owner)
    {
      return string.IsNullOrEmpty(owner) ? "." : " by " + owner + ".";
    }

    private void HandleError(CoordinationServerEnvelope _)
    {
      // Authentication failures are published from the state transition. Other transport
      // errors remain visible in the window connection state and Console.
    }
  }
}
