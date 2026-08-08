using NUnit.Framework;
using PotionPanic.Editor.Coordination;
using System.Linq;

namespace PotionPanic.Tests.EditMode.Coordination
{
  public sealed class CoordinationProtocolTests
  {
    private const string RequestId = "123e4567-e89b-42d3-a456-426614174000";
    private const string SnapshotId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
    private const string Lease = "{\"leaseId\":\"lease-1\",\"path\":\"assets/scenes/a.unity\","
      + "\"displayPath\":\"Assets/Scenes/A.unity\",\"mode\":\"editing\","
      + "\"developerId\":\"dev-1\",\"displayName\":\"Rin\",\"branch\":\"feature/a\","
      + "\"task\":\"PP-7\",\"expiresAt\":\"2026-08-06T00:02:00Z\","
      + "\"connectionId\":\"conn-1\"}";

    [Test]
    public void AcceptsAValidLeaseAcquireEnvelope()
    {
      const string json = "{\"protocolVersion\":1,\"type\":\"lease.acquire\","
        + "\"requestId\":\"123e4567-e89b-42d3-a456-426614174000\","
        + "\"path\":\"Assets\\\\Scenes\\\\SampleScene.unity\","
        + "\"branch\":\"feature/coordination\",\"task\":\"PP-7\"}";

      Assert.That(CoordinationProtocol.TryParseClientEnvelope(json, out var envelope, out _),
        Is.True);
      Assert.That(envelope.path, Is.EqualTo("Assets/Scenes/SampleScene.unity"));
    }

    [Test]
    public void AcceptsEveryV1ClientMessageWithItsRequiredFields()
    {
      var envelopes = new[]
      {
        ClientEnvelope("presence.open", "\"path\":\"Assets/Scenes/A.unity\",\"branch\":\"feature/a\",\"task\":\"PP-7\""),
        ClientEnvelope("presence.close", "\"path\":\"Assets/Scenes/A.unity\""),
        ClientEnvelope("lease.acquire", "\"path\":\"Assets/Scenes/A.unity\",\"branch\":\"feature/a\",\"task\":\"PP-7\""),
        ClientEnvelope("lease.release", "\"path\":\"Assets/Scenes/A.unity\""),
        ClientEnvelope("lease.reserve", "\"path\":\"Assets/Scenes/A.unity\",\"branch\":\"feature/a\",\"task\":\"PP-7\""),
        ClientEnvelope("reservation.cancel", "\"path\":\"Assets/Scenes/A.unity\""),
        ClientEnvelope("lease.override", "\"path\":\"Assets/Scenes/A.unity\",\"branch\":\"feature/a\",\"task\":\"PP-7\""),
        ClientEnvelope("heartbeat", string.Empty),
        ClientEnvelope("snapshot.request", string.Empty)
      };

      foreach (var json in envelopes)
      {
        Assert.That(CoordinationProtocol.TryParseClientEnvelope(json, out _, out _), Is.True,
          json);
      }
    }

    [Test]
    public void RejectsClientMessagesWithMissingRequiredFieldsOrOversizedContext()
    {
      var envelopes = new[]
      {
        ClientEnvelope("presence.open", "\"branch\":\"feature/a\",\"task\":\"PP-7\""),
        ClientEnvelope("presence.close", string.Empty),
        ClientEnvelope("lease.acquire", "\"path\":\"Assets/Scenes/A.unity\",\"task\":\"PP-7\""),
        ClientEnvelope("lease.release", string.Empty),
        ClientEnvelope("lease.reserve", "\"path\":\"Assets/Scenes/A.unity\",\"branch\":\"feature/a\""),
        ClientEnvelope("reservation.cancel", string.Empty),
        ClientEnvelope("lease.override", "\"path\":\"Assets/Scenes/A.unity\",\"branch\":\"feature/a\""),
        ClientEnvelope("presence.open", "\"path\":\"Assets/Scenes/A.unity\",\"branch\":\""
          + new string('x', 257) + "\",\"task\":\"PP-7\"")
      };

      foreach (var json in envelopes)
      {
        Assert.That(CoordinationProtocol.TryParseClientEnvelope(json, out _, out _), Is.False,
          json);
      }
    }

    [Test]
    public void RejectsContextFieldsOnReservationCancellation()
    {
      var json = ClientEnvelope(
        "reservation.cancel",
        "\"path\":\"Assets/Scenes/A.unity\",\"branch\":\"feature/a\",\"task\":\"PP-7\"");

      Assert.That(CoordinationProtocol.TryParseClientEnvelope(json, out _, out _), Is.False);
    }

    [Test]
    public void ContextLimitsUseUtf16CodeUnitsAndPreserveSurrogatePairs()
    {
      var accepted = string.Concat(Enumerable.Repeat("😀", 128));
      var rejected = string.Concat(Enumerable.Repeat("😀", 129));

      Assert.That(CoordinationProtocol.IsValidContext(string.Empty), Is.True);
      Assert.That(CoordinationProtocol.IsValidContext(accepted), Is.True);
      Assert.That(CoordinationProtocol.IsValidContext(rejected), Is.False);
      Assert.That(CoordinationProtocol.ClampContext(rejected), Is.EqualTo(accepted));
    }

    [TestCase("{\"protocolVersion\":2,\"type\":\"heartbeat\",\"requestId\":\"123e4567-e89b-42d3-a456-426614174000\"}")]
    [TestCase("{\"protocolVersion\":1,\"type\":\"heartbeat\",\"requestId\":\"not-a-uuid\"}")]
    [TestCase("{\"protocolVersion\":1,\"type\":\"lease.acquire\",\"requestId\":\"123e4567-e89b-42d3-a456-426614174000\",\"path\":\"../secret\",\"branch\":\"\",\"task\":\"\"}")]
    [TestCase("{\"protocolVersion\":1,\"type\":\"heartbeat\",\"requestId\":\"123e4567-e89b-42d3-a456-426614174000\",\"developerId\":\"forbidden\"}")]
    public void RejectsInvalidClientEnvelope(string json)
    {
      Assert.That(CoordinationProtocol.TryParseClientEnvelope(json, out _, out _), Is.False);
    }

    [Test]
    public void RejectsOversizeEnvelope()
    {
      var json = "{\"protocolVersion\":1,\"type\":\"heartbeat\","
        + "\"requestId\":\"123e4567-e89b-42d3-a456-426614174000\",\"padding\":\""
        + new string('x', 16 * 1024) + "\"}";

      Assert.That(CoordinationProtocol.TryParseClientEnvelope(json, out _, out _), Is.False);
    }

    [Test]
    public void RejectsSubmittedPathOverTheLimitBeforeSeparatorNormalization()
    {
      var path = "Assets/" + new string('/', 1020) + "A.unity";
      var json = "{\"protocolVersion\":1,\"type\":\"lease.release\","
        + "\"requestId\":\"123e4567-e89b-42d3-a456-426614174000\",\"path\":\""
        + path + "\"}";

      Assert.That(CoordinationProtocol.TryParseClientEnvelope(json, out _, out _), Is.False);
    }

    [Test]
    public void RejectsReservedLeaseWithConnectionId()
    {
      const string json = "{\"protocolVersion\":1,\"type\":\"lease.updated\","
        + "\"stateVersion\":1,\"lease\":{\"leaseId\":\"lease-1\","
        + "\"path\":\"assets/scenes/a.unity\",\"displayPath\":\"Assets/Scenes/A.unity\","
        + "\"mode\":\"reserved\",\"developerId\":\"dev-1\",\"displayName\":\"Rin\","
        + "\"branch\":\"feature/a\",\"task\":\"PP-7\","
        + "\"expiresAt\":\"2026-08-06T00:02:00Z\",\"connectionId\":\"conn-1\"}}";

      Assert.That(CoordinationProtocol.TryParseServerEnvelope(json, out _, out _), Is.False);
    }

    [Test]
    public void RejectsReservedLeaseWithEscapedConnectionIdPropertyName()
    {
      const string json = "{\"protocolVersion\":1,\"type\":\"lease.updated\","
        + "\"stateVersion\":1,\"lease\":{\"leaseId\":\"lease-1\","
        + "\"path\":\"assets/scenes/a.unity\",\"displayPath\":\"Assets/Scenes/A.unity\","
        + "\"mode\":\"reserved\",\"developerId\":\"dev-1\",\"displayName\":\"Rin\","
        + "\"branch\":\"feature/a\",\"task\":\"PP-7\","
        + "\"expiresAt\":\"2026-08-06T00:02:00Z\",\"\\u0063onnectionId\":\"conn-1\"}}";

      Assert.That(CoordinationProtocol.TryParseServerEnvelope(json, out _, out _), Is.False);
    }

    [Test]
    public void RejectsOversizeServerEnvelopes()
    {
      var envelopes = new[]
      {
        "{\"protocolVersion\":1,\"type\":\"snapshot\",\"stateVersion\":1,"
          + "\"snapshotId\":\"" + SnapshotId + "\",\"chunkIndex\":0,\"chunkCount\":1,"
          + "\"presence\":[],\"leases\":[],\"serverTime\":\""
          + new string('x', 16 * 1024) + "\"}",
        "{\"protocolVersion\":1,\"type\":\"error\",\"stateVersion\":1,"
          + "\"code\":\"invalid_path\",\"message\":\""
          + new string('x', 16 * 1024) + "\"}"
      };

      foreach (var json in envelopes)
      {
        Assert.That(CoordinationProtocol.TryParseServerEnvelope(json, out _, out _), Is.False);
      }
    }

    [Test]
    public void AcceptsAValidSnapshotChunk()
    {
      var json = SnapshotChunkJson();

      Assert.That(CoordinationProtocol.TryParseServerEnvelope(
        json, out var envelope, out _), Is.True);
      Assert.That(envelope.snapshotId, Is.EqualTo(SnapshotId));
      Assert.That(envelope.chunkIndex, Is.Zero);
      Assert.That(envelope.chunkCount, Is.EqualTo(2));
    }

    [TestCase("\"snapshotId\":\"aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa\",")]
    [TestCase("\"chunkIndex\":0,")]
    [TestCase("\"chunkCount\":2,")]
    [TestCase("\"stateVersion\":1,")]
    [TestCase("\"presence\":[],")]
    [TestCase("\"leases\":[]")]
    [TestCase("\"serverTime\":\"2026-08-06T00:00:00Z\",")]
    public void RejectsSnapshotChunkWhenARequiredFieldIsMissing(string field)
    {
      var json = SnapshotChunkJson().Replace(field, string.Empty);

      Assert.That(CoordinationProtocol.TryParseServerEnvelope(json, out _, out _), Is.False,
        json);
    }

    [TestCase("\"snapshotId\":\"not-a-uuid\"")]
    [TestCase("\"chunkIndex\":-1")]
    [TestCase("\"chunkIndex\":2")]
    [TestCase("\"chunkCount\":0")]
    public void RejectsSnapshotChunkWithInvalidChunkMetadata(string replacement)
    {
      var original = replacement.StartsWith("\"snapshotId\"")
        ? "\"snapshotId\":\"" + SnapshotId + "\""
        : replacement.StartsWith("\"chunkCount\"")
          ? "\"chunkCount\":2"
          : "\"chunkIndex\":0";
      var json = SnapshotChunkJson().Replace(original, replacement);

      Assert.That(CoordinationProtocol.TryParseServerEnvelope(json, out _, out _), Is.False,
        json);
    }

    [TestCase("stateVersion", "nested")]
    [TestCase("stateVersion", "string")]
    [TestCase("chunkIndex", "nested")]
    [TestCase("chunkIndex", "string")]
    [TestCase("chunkCount", "nested")]
    [TestCase("chunkCount", "string")]
    [TestCase("currentLease", "nested")]
    [TestCase("currentLease", "string")]
    public void RejectsMissingTopLevelFieldWhenItsNameAppearsOnlyInNestedContent(
      string field,
      string spoofKind)
    {
      var json = SpoofedMissingTopLevelField(field, spoofKind);

      Assert.That(CoordinationProtocol.TryParseServerEnvelope(json, out _, out _), Is.False,
        field + ":" + spoofKind + " " + json);
    }

    [TestCase("nested")]
    [TestCase("string")]
    public void IgnoresOptionalRequestIdOutsideTheTopLevelObject(string spoofKind)
    {
      var spoof = spoofKind == "nested"
        ? "\"metadata\":{\"requestId\":\"not-a-uuid\"},"
        : "\"message\":\"embedded \\\"requestId\\\":\\\"not-a-uuid\\\"\", ";
      var json = SnapshotChunkJson().Replace("\"snapshotId\"", spoof + "\"snapshotId\"");

      Assert.That(CoordinationProtocol.TryParseServerEnvelope(json, out _, out _), Is.True,
        json);
    }

    [Test]
    public void AcceptsTopLevelFieldsAfterEscapedStringContent()
    {
      const string json = "{\"protocolVersion\":1,\"type\":\"error\","
        + "\"message\":\"embedded {\\\"stateVersion\\\":999}\","
        + "\"stateVersion\":1,\"code\":\"invalid_path\"}";

      Assert.That(CoordinationProtocol.TryParseServerEnvelope(json, out _, out _), Is.True);
    }

    [Test]
    public void AcceptsEscapedTopLevelPropertyName()
    {
      const string json = "{\"protocolVersion\":1,\"type\":\"error\","
        + "\"\\u0073tateVersion\":4,\"code\":\"invalid_path\",\"message\":\"Bad path.\"}";

      Assert.That(CoordinationProtocol.TryParseServerEnvelope(
        json, out var envelope, out _), Is.True);
      Assert.That(envelope.stateVersion, Is.EqualTo(4));
    }

    [Test]
    public void AcceptsEveryV1ServerMessageWithItsRequiredFields()
    {
      var envelopes = new[]
      {
        "{\"protocolVersion\":1,\"type\":\"session.ready\",\"stateVersion\":1,\"developerId\":\"dev-1\",\"displayName\":\"Rin\",\"serverTime\":\"2026-08-06T00:00:00Z\",\"connectionId\":\"conn-1\",\"leaseTtlSeconds\":120,\"reservationTtlSeconds\":1800}",
        SnapshotChunkJson(),
        "{\"protocolVersion\":1,\"type\":\"presence.updated\",\"stateVersion\":1,\"presence\":[]}",
        "{\"protocolVersion\":1,\"type\":\"presence.removed\",\"stateVersion\":1,\"path\":\"Assets/Scenes/A.unity\",\"connectionId\":\"conn-1\"}",
        "{\"protocolVersion\":1,\"type\":\"lease.granted\",\"stateVersion\":1,\"path\":\"Assets/Scenes/A.unity\",\"lease\":" + Lease + "}",
        "{\"protocolVersion\":1,\"type\":\"lease.denied\",\"stateVersion\":1,\"path\":\"Assets/Scenes/A.unity\",\"code\":\"denied\",\"currentLease\":null}",
        "{\"protocolVersion\":1,\"type\":\"lease.updated\",\"stateVersion\":1,\"lease\":" + Lease + "}",
        "{\"protocolVersion\":1,\"type\":\"lease.released\",\"stateVersion\":1,\"path\":\"Assets/Scenes/A.unity\",\"leaseId\":\"lease-1\"}",
        "{\"protocolVersion\":1,\"type\":\"lease.overridden\",\"stateVersion\":1,\"path\":\"Assets/Scenes/A.unity\",\"previousDeveloperId\":\"dev-1\",\"lease\":" + Lease + "}",
        "{\"protocolVersion\":1,\"type\":\"error\",\"stateVersion\":1,\"code\":\"invalid_path\",\"message\":\"Bad path.\"}"
      };

      foreach (var json in envelopes)
      {
        Assert.That(CoordinationProtocol.TryParseServerEnvelope(json, out _, out _), Is.True,
          json);
      }
    }

    [Test]
    public void RejectsServerMessagesWithMissingRequiredFields()
    {
      var envelopes = new[]
      {
        "{\"protocolVersion\":1,\"type\":\"session.ready\",\"stateVersion\":1}",
        "{\"protocolVersion\":1,\"type\":\"snapshot\",\"stateVersion\":1,"
          + "\"snapshotId\":\"" + SnapshotId + "\",\"chunkIndex\":0,\"chunkCount\":1,"
          + "\"presence\":[],\"leases\":[]}",
        "{\"protocolVersion\":1,\"type\":\"presence.updated\",\"stateVersion\":1}",
        "{\"protocolVersion\":1,\"type\":\"presence.removed\",\"stateVersion\":1,\"path\":\"Assets/Scenes/A.unity\"}",
        "{\"protocolVersion\":1,\"type\":\"lease.granted\",\"stateVersion\":1,\"path\":\"Assets/Scenes/A.unity\"}",
        "{\"protocolVersion\":1,\"type\":\"lease.denied\",\"stateVersion\":1,\"path\":\"Assets/Scenes/A.unity\",\"code\":\"denied\"}",
        "{\"protocolVersion\":1,\"type\":\"lease.updated\",\"stateVersion\":1}",
        "{\"protocolVersion\":1,\"type\":\"lease.released\",\"stateVersion\":1,\"path\":\"Assets/Scenes/A.unity\"}",
        "{\"protocolVersion\":1,\"type\":\"lease.overridden\",\"stateVersion\":1,\"path\":\"Assets/Scenes/A.unity\",\"previousDeveloperId\":\"dev-1\"}",
        "{\"protocolVersion\":1,\"type\":\"error\",\"stateVersion\":1,\"code\":\"invalid_path\"}"
      };

      foreach (var json in envelopes)
      {
        Assert.That(CoordinationProtocol.TryParseServerEnvelope(json, out _, out _), Is.False,
          json);
      }
    }

    [Test]
    public void IgnoresServerStateOlderThanTheNewestAppliedVersion()
    {
      var state = new CoordinationProtocolState();
      const string newer = "{\"protocolVersion\":1,\"type\":\"snapshot\","
        + "\"stateVersion\":3,\"snapshotId\":\"aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa\","
        + "\"chunkIndex\":0,\"chunkCount\":1,\"presence\":[],\"leases\":[],"
        + "\"serverTime\":\"2026-08-06T00:00:00Z\"}";
      const string older = "{\"protocolVersion\":1,\"type\":\"snapshot\","
        + "\"stateVersion\":2,\"snapshotId\":\"bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb\","
        + "\"chunkIndex\":0,\"chunkCount\":1,\"presence\":[],\"leases\":[],"
        + "\"serverTime\":\"2026-08-06T00:00:00Z\"}";

      Assert.That(state.TryApplyServerEnvelope(newer, out _, out _), Is.True);
      Assert.That(state.TryApplyServerEnvelope(older, out _, out _), Is.False);
      Assert.That(state.NewestAppliedStateVersion, Is.EqualTo(3));
    }

    private static string ClientEnvelope(string type, string additionalFields)
    {
      var comma = string.IsNullOrEmpty(additionalFields) ? string.Empty : ",";
      return "{\"protocolVersion\":1,\"type\":\"" + type + "\",\"requestId\":\""
        + RequestId + "\"" + comma + additionalFields + "}";
    }

    private static string SnapshotChunkJson()
    {
      return "{\"protocolVersion\":1,\"type\":\"snapshot\","
        + "\"snapshotId\":\"" + SnapshotId + "\",\"chunkIndex\":0,\"chunkCount\":2,"
        + "\"stateVersion\":1,\"serverTime\":\"2026-08-06T00:00:00Z\","
        + "\"presence\":[],\"leases\":[]}";
    }

    private static string SpoofedMissingTopLevelField(string field, string spoofKind)
    {
      if (field == "currentLease")
      {
        const string denied = "{\"protocolVersion\":1,\"type\":\"lease.denied\","
          + "\"stateVersion\":1,\"path\":\"Assets/Scenes/A.unity\",\"code\":\"denied\","
          + "\"currentLease\":null}";
        var spoof = spoofKind == "nested"
          ? "\"metadata\":{\"currentLease\":null}"
          : "\"message\":\"embedded \\\"currentLease\\\":null\"";
        return denied.Replace("\"currentLease\":null", spoof);
      }

      var value = field == "chunkCount" ? "2" : field == "stateVersion" ? "1" : "0";
      var fieldText = "\"" + field + "\":" + value + ",";
      var replacement = spoofKind == "nested"
        ? "\"metadata\":{\"" + field + "\":" + value + "},"
        : "\"message\":\"embedded \\\"" + field + "\\\":" + value + "\",";
      return SnapshotChunkJson().Replace(fieldText, replacement);
    }
  }
}
