using System;
using System.Collections.Generic;
using NUnit.Framework;
using PotionPanic.Editor.Coordination;

namespace PotionPanic.Tests.EditMode.Coordination
{
  public sealed class CoordinationNotificationControllerTests
  {
    [Test]
    public void PublishesOnlyApprovedCoordinationEvents()
    {
      var source = new FakeNotificationSource();
      var sink = new FakeNotificationSink();
      var clock = new FakeClock(new DateTimeOffset(2026, 8, 8, 10, 0, 0, TimeSpan.Zero));
      using var controller = new CoordinationNotificationController(
        source, sink, clock, TimeSpan.FromMinutes(2));
      controller.Enable();

      source.RaiseLease(LeaseEnvelope("lease.granted", "editing"));
      source.RaiseLease(LeaseEnvelope("lease.updated", "editing"));
      source.RaiseLease(LeaseEnvelope("lease.denied", "editing"));
      source.RaiseLease(LeaseEnvelope("lease.overridden", "editing"));
      source.RaiseLease(LeaseEnvelope("lease.granted", "reserved"));
      source.RaiseLease(LeaseEnvelope("lease.released", "editing"));
      source.RaiseError("connection_failed");

      Assert.That(sink.Kinds, Is.EqualTo(new[]
      {
        CoordinationNotificationKind.Claim,
        CoordinationNotificationKind.Conflict,
        CoordinationNotificationKind.Override,
        CoordinationNotificationKind.Reservation
      }));
    }

    [Test]
    public void PublishesAuthenticationFailureAndOneProlongedDisconnectPerOutage()
    {
      var source = new FakeNotificationSource();
      var sink = new FakeNotificationSink();
      var clock = new FakeClock(new DateTimeOffset(2026, 8, 8, 10, 0, 0, TimeSpan.Zero));
      using var controller = new CoordinationNotificationController(
        source, sink, clock, TimeSpan.FromMinutes(2));
      controller.Enable();

      source.RaiseState(CoordinationConnectionState.Offline);
      clock.Advance(TimeSpan.FromMinutes(1));
      controller.Tick();
      clock.Advance(TimeSpan.FromMinutes(1));
      controller.Tick();
      controller.Tick();
      source.RaiseState(CoordinationConnectionState.AuthenticationFailed);

      Assert.That(sink.Kinds, Is.EqualTo(new[]
      {
        CoordinationNotificationKind.ProlongedDisconnect,
        CoordinationNotificationKind.AuthenticationFailure
      }));

      source.RaiseState(CoordinationConnectionState.Connected);
      source.RaiseState(CoordinationConnectionState.Reconnecting);
      clock.Advance(TimeSpan.FromMinutes(2));
      controller.Tick();

      Assert.That(sink.Kinds.FindAll(value =>
        value == CoordinationNotificationKind.ProlongedDisconnect), Has.Count.EqualTo(2));
    }

    [Test]
    public void RetainsNotificationsUntilAWindowCanDisplayThem()
    {
      var presenter = new FakeNotificationPresenter();
      var sink = new UnityCoordinationNotificationSink(presenter);
      var notification = new CoordinationNotification(
        CoordinationNotificationKind.Conflict,
        "Coordination conflict for Assets/Scenes/Laboratory.unity.");

      sink.Publish(notification);

      Assert.That(sink.PendingCount, Is.EqualTo(1));
      Assert.That(presenter.Notifications, Is.Empty);

      presenter.IsAvailable = true;
      sink.FlushPending();

      Assert.That(sink.PendingCount, Is.Zero);
      Assert.That(presenter.Notifications, Is.EqualTo(new[] { notification }));
    }

    private static CoordinationServerEnvelope LeaseEnvelope(string type, string mode)
    {
      return new CoordinationServerEnvelope
      {
        protocolVersion = 1,
        type = type,
        stateVersion = 2,
        path = "assets/scenes/laboratory.unity",
        currentLease = type == "lease.denied" ? Lease(mode) : null,
        lease = type == "lease.denied" || type == "lease.released" ? null : Lease(mode)
      };
    }

    private static CoordinationLeaseRecord Lease(string mode)
    {
      return new CoordinationLeaseRecord
      {
        path = "assets/scenes/laboratory.unity",
        displayPath = "Assets/Scenes/Laboratory.unity",
        mode = mode,
        developerId = "dev-remote",
        displayName = "Sol",
        expiresAt = "2026-08-08T12:00:00Z"
      };
    }

    private sealed class FakeNotificationSource : ICoordinationNotificationSource
    {
      public CoordinationConnectionState State { get; private set; }
        = CoordinationConnectionState.Connected;
      public event Action<CoordinationConnectionState> StateChanged;
      public event Action<CoordinationServerEnvelope> LeaseResultReceived;
      public event Action<CoordinationServerEnvelope> ErrorReceived;

      public void RaiseState(CoordinationConnectionState state)
      {
        State = state;
        StateChanged?.Invoke(state);
      }

      public void RaiseLease(CoordinationServerEnvelope envelope)
        => LeaseResultReceived?.Invoke(envelope);

      public void RaiseError(string code)
      {
        ErrorReceived?.Invoke(new CoordinationServerEnvelope
        {
          protocolVersion = 1,
          type = "error",
          stateVersion = 2,
          code = code,
          message = code
        });
      }
    }

    private sealed class FakeNotificationSink : ICoordinationNotificationSink
    {
      public List<CoordinationNotificationKind> Kinds { get; }
        = new List<CoordinationNotificationKind>();

      public void Publish(CoordinationNotification notification)
      {
        Kinds.Add(notification.Kind);
      }
    }

    private sealed class FakeNotificationPresenter
      : ICoordinationUnityNotificationPresenter
    {
      public bool IsAvailable { get; set; }
      public List<CoordinationNotification> Notifications { get; }
        = new List<CoordinationNotification>();

      public bool TryPublish(CoordinationNotification notification)
      {
        if (!IsAvailable)
        {
          return false;
        }

        Notifications.Add(notification);
        return true;
      }
    }

    private sealed class FakeClock : ICoordinationClock
    {
      public DateTimeOffset UtcNow { get; private set; }

      public FakeClock(DateTimeOffset utcNow)
      {
        UtcNow = utcNow;
      }

      public void Advance(TimeSpan duration) => UtcNow += duration;
    }
  }
}
