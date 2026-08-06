using System;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace PotionPanic.Editor.Coordination
{
  [Serializable]
  public sealed class CoordinationClientEnvelope
  {
    public int protocolVersion;
    public string type;
    public string requestId;
    public string path;
    public string branch;
    public string task;
  }

  [Serializable]
  public sealed class CoordinationPresenceRecord
  {
    public string path;
    public string displayPath;
    public string developerId;
    public string displayName;
    public string connectionId;
    public string branch;
    public string task;
    public string expiresAt;
  }

  [Serializable]
  public sealed class CoordinationLeaseRecord
  {
    public string leaseId;
    public string path;
    public string displayPath;
    public string mode;
    public string developerId;
    public string displayName;
    public string branch;
    public string task;
    public string expiresAt;
    public string connectionId;
  }

  [Serializable]
  public sealed class CoordinationServerEnvelope
  {
    public int protocolVersion;
    public string type;
    public long stateVersion;
    public string requestId;
    public string developerId;
    public string displayName;
    public string serverTime;
    public string connectionId;
    public int leaseTtlSeconds;
    public int reservationTtlSeconds;
    public CoordinationPresenceRecord[] presence;
    public CoordinationLeaseRecord[] leases;
    public string path;
    public CoordinationLeaseRecord lease;
    public string code;
    public CoordinationLeaseRecord currentLease;
    public string leaseId;
    public string previousDeveloperId;
    public string message;
  }

  public sealed class CoordinationProtocolState
  {
    public long NewestAppliedStateVersion { get; private set; }

    public bool TryApplyServerEnvelope(
      string json,
      out CoordinationServerEnvelope envelope,
      out string error)
    {
      if (!CoordinationProtocol.TryParseServerEnvelope(json, out envelope, out error))
      {
        return false;
      }

      if (envelope.stateVersion < NewestAppliedStateVersion)
      {
        error = "Received an older server state version.";
        return false;
      }

      NewestAppliedStateVersion = envelope.stateVersion;
      return true;
    }
  }

  public static class CoordinationProtocol
  {
    public const int Version = 1;
    public const int MaximumEnvelopeBytes = 16 * 1024;
    public const int MaximumPathLength = 1024;
    public const int MaximumContextLength = 256;

    private static readonly string[] ClientMessageTypes =
    {
      "presence.open", "presence.close", "lease.acquire", "lease.release",
      "lease.reserve", "lease.override", "heartbeat", "snapshot.request"
    };

    private static readonly string[] ServerMessageTypes =
    {
      "session.ready", "snapshot", "presence.updated", "presence.removed",
      "lease.granted", "lease.denied", "lease.updated", "lease.released",
      "lease.overridden", "error"
    };

    public static bool TryParseClientEnvelope(
      string json,
      out CoordinationClientEnvelope envelope,
      out string error)
    {
      envelope = null;
      if (string.IsNullOrWhiteSpace(json) || Encoding.UTF8.GetByteCount(json) > MaximumEnvelopeBytes)
      {
        error = "Client envelope is empty or too large.";
        return false;
      }

      if (ContainsForbiddenClientIdentity(json))
      {
        error = "Client envelope contains a server-assigned identity field.";
        return false;
      }

      try
      {
        var parsed = JsonUtility.FromJson<CoordinationClientEnvelope>(json);
        if (parsed == null || parsed.protocolVersion != Version || !Contains(ClientMessageTypes, parsed.type)
          || !IsUuidV4(parsed.requestId))
        {
          error = "Client envelope has an invalid protocol version, type, or request ID.";
          return false;
        }

        if (RequiresPath(parsed.type))
        {
          if (parsed.path == null || parsed.path.Length > MaximumPathLength
            || !CoordinationPathMatcher.TryNormalize(parsed.path, out var normalizedPath)
            || normalizedPath.Length > MaximumPathLength)
          {
            error = "Client envelope has an invalid path.";
            return false;
          }

          parsed.path = normalizedPath;
        }

        if (RequiresContext(parsed.type) && (!HasValidContext(parsed.branch)
          || !HasValidContext(parsed.task)))
        {
          error = "Client envelope has an invalid branch or task.";
          return false;
        }

        envelope = parsed;
        error = null;
        return true;
      }
      catch (ArgumentException)
      {
        error = "Client envelope is not valid JSON.";
        return false;
      }
    }

    public static bool TryParseServerEnvelope(
      string json,
      out CoordinationServerEnvelope envelope,
      out string error)
    {
      envelope = null;
      if (string.IsNullOrWhiteSpace(json) || Encoding.UTF8.GetByteCount(json) > MaximumEnvelopeBytes)
      {
        error = "Server envelope is empty or too large.";
        return false;
      }

      try
      {
        var parsed = JsonUtility.FromJson<CoordinationServerEnvelope>(json);
        if (parsed == null || parsed.protocolVersion != Version || !Contains(ServerMessageTypes, parsed.type)
          || parsed.stateVersion < 0 || !HasJsonField(json, "stateVersion")
          || (HasJsonField(json, "requestId") && !IsUuidV4(parsed.requestId)))
        {
          error = "Server envelope has invalid required envelope fields.";
          return false;
        }

        if (!HasRequiredServerFields(parsed, json))
        {
          error = "Server envelope is missing message-specific fields.";
          return false;
        }

        envelope = parsed;
        error = null;
        return true;
      }
      catch (ArgumentException)
      {
        error = "Server envelope is not valid JSON.";
        return false;
      }
    }

    private static bool HasRequiredServerFields(CoordinationServerEnvelope envelope, string json)
    {
      switch (envelope.type)
      {
        case "session.ready":
          return HasStrings(envelope.developerId, envelope.displayName, envelope.serverTime,
              envelope.connectionId)
            && envelope.leaseTtlSeconds > 0 && envelope.reservationTtlSeconds > 0;
        case "snapshot":
          return HasPresenceRecords(envelope.presence) && HasLeaseRecords(envelope.leases)
            && HasStrings(envelope.serverTime);
        case "presence.updated":
          return HasPresenceRecords(envelope.presence);
        case "presence.removed":
          return HasStrings(envelope.path, envelope.connectionId);
        case "lease.granted":
          return HasStrings(envelope.path) && IsLeaseRecord(envelope.lease);
        case "lease.denied":
          return HasStrings(envelope.path, envelope.code) && HasJsonField(json, "currentLease")
            && (envelope.currentLease == null || IsLeaseRecord(envelope.currentLease));
        case "lease.updated":
          return IsLeaseRecord(envelope.lease);
        case "lease.released":
          return HasStrings(envelope.path, envelope.leaseId);
        case "lease.overridden":
          return HasStrings(envelope.path, envelope.previousDeveloperId)
            && IsLeaseRecord(envelope.lease);
        case "error":
          return HasStrings(envelope.code, envelope.message);
        default:
          return false;
      }
    }

    private static bool RequiresPath(string type)
    {
      return type != "heartbeat" && type != "snapshot.request";
    }

    private static bool RequiresContext(string type)
    {
      return type == "presence.open" || type == "lease.acquire" || type == "lease.reserve"
        || type == "lease.override";
    }

    private static bool HasValidContext(string value)
    {
      return value != null && value.Length <= MaximumContextLength;
    }

    private static bool ContainsForbiddenClientIdentity(string json)
    {
      return Regex.IsMatch(json, "\\\"(?:projectId|developerId|connectionId)\\\"\\s*:",
        RegexOptions.CultureInvariant);
    }

    private static bool HasJsonField(string json, string field)
    {
      return Regex.IsMatch(json, "\\\"" + Regex.Escape(field) + "\\\"\\s*:",
        RegexOptions.CultureInvariant);
    }

    private static bool IsUuidV4(string requestId)
    {
      return requestId != null && Regex.IsMatch(requestId,
        "^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }

    private static bool Contains(string[] values, string value)
    {
      foreach (var candidate in values)
      {
        if (candidate == value)
        {
          return true;
        }
      }

      return false;
    }

    private static bool HasStrings(params string[] values)
    {
      foreach (var value in values)
      {
        if (value == null)
        {
          return false;
        }
      }

      return true;
    }

    private static bool HasPresenceRecords(CoordinationPresenceRecord[] records)
    {
      if (records == null)
      {
        return false;
      }

      foreach (var record in records)
      {
        if (record == null || !HasStrings(record.path, record.displayPath, record.developerId,
          record.displayName, record.connectionId, record.branch, record.task, record.expiresAt))
        {
          return false;
        }
      }

      return true;
    }

    private static bool HasLeaseRecords(CoordinationLeaseRecord[] records)
    {
      if (records == null)
      {
        return false;
      }

      foreach (var record in records)
      {
        if (!IsLeaseRecord(record))
        {
          return false;
        }
      }

      return true;
    }

    private static bool IsLeaseRecord(CoordinationLeaseRecord record)
    {
      if (record == null || !HasStrings(record.leaseId, record.path, record.displayPath,
        record.developerId, record.displayName, record.branch, record.task, record.expiresAt)
        || (record.mode != "editing" && record.mode != "reserved"))
      {
        return false;
      }

      return record.mode == "editing" ? record.connectionId != null : record.connectionId == null;
    }
  }
}
