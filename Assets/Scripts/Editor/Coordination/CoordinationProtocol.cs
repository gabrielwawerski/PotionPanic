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
    public string snapshotId;
    public int chunkIndex;
    public int chunkCount;
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

      return TryApplyServerEnvelope(envelope, out error);
    }

    public bool TryApplyServerEnvelope(CoordinationServerEnvelope envelope, out string error)
    {
      if (envelope == null)
      {
        error = "The server envelope is missing.";
        return false;
      }

      if (envelope.stateVersion < NewestAppliedStateVersion)
      {
        error = "Received an older server state version.";
        return false;
      }

      NewestAppliedStateVersion = envelope.stateVersion;
      error = null;
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
          || parsed.stateVersion < 0 || !HasTopLevelJsonField(json, "stateVersion")
          || (HasTopLevelJsonField(json, "requestId") && !IsUuidV4(parsed.requestId)))
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
          return IsUuidV4(envelope.snapshotId) && HasTopLevelJsonField(json, "chunkIndex")
            && HasTopLevelJsonField(json, "chunkCount") && envelope.chunkIndex >= 0
            && envelope.chunkCount > 0 && envelope.chunkIndex < envelope.chunkCount
            && HasPresenceRecords(envelope.presence) && HasLeaseRecords(envelope.leases)
            && HasStrings(envelope.serverTime);
        case "presence.updated":
          return HasPresenceRecords(envelope.presence);
        case "presence.removed":
          return HasStrings(envelope.path, envelope.connectionId);
        case "lease.granted":
          return HasStrings(envelope.path) && IsLeaseRecord(envelope.lease);
        case "lease.denied":
          return HasStrings(envelope.path, envelope.code)
            && HasTopLevelJsonField(json, "currentLease")
            && (IsTopLevelJsonNullField(json, "currentLease")
              || IsLeaseRecord(envelope.currentLease));
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

    private static bool HasTopLevelJsonField(string json, string field)
    {
      return TryFindTopLevelJsonField(json, field, out _);
    }

    private static bool IsTopLevelJsonNullField(string json, string field)
    {
      if (!TryFindTopLevelJsonField(json, field, out var valueIndex)
        || valueIndex + 4 > json.Length
        || string.CompareOrdinal(json, valueIndex, "null", 0, 4) != 0)
      {
        return false;
      }

      valueIndex += 4;
      SkipJsonWhitespace(json, ref valueIndex);
      return valueIndex < json.Length
        && (json[valueIndex] == ',' || json[valueIndex] == '}');
    }

    private static bool TryFindTopLevelJsonField(
      string json,
      string field,
      out int valueIndex)
    {
      valueIndex = -1;
      var index = 0;
      SkipJsonWhitespace(json, ref index);
      if (index >= json.Length || json[index] != '{')
      {
        return false;
      }

      index++;
      while (index < json.Length)
      {
        SkipJsonWhitespace(json, ref index);
        if (index < json.Length && json[index] == '}')
        {
          return false;
        }

        if (!TryReadJsonString(json, ref index, out var propertyName))
        {
          return false;
        }

        SkipJsonWhitespace(json, ref index);
        if (index >= json.Length || json[index] != ':')
        {
          return false;
        }

        index++;
        SkipJsonWhitespace(json, ref index);
        if (string.Equals(propertyName, field, StringComparison.Ordinal))
        {
          valueIndex = index;
          return true;
        }

        if (!TrySkipJsonValue(json, ref index))
        {
          return false;
        }

        SkipJsonWhitespace(json, ref index);
        if (index >= json.Length || json[index] == '}')
        {
          return false;
        }

        if (json[index] != ',')
        {
          return false;
        }

        index++;
      }

      return false;
    }

    private static bool TryReadJsonString(string json, ref int index, out string value)
    {
      value = null;
      if (index >= json.Length || json[index] != '"')
      {
        return false;
      }

      index++;
      var segmentStart = index;
      StringBuilder builder = null;
      while (index < json.Length)
      {
        var character = json[index];
        if (character == '"')
        {
          if (builder == null)
          {
            value = json.Substring(segmentStart, index - segmentStart);
          }
          else
          {
            builder.Append(json, segmentStart, index - segmentStart);
            value = builder.ToString();
          }

          index++;
          return true;
        }

        if (character != '\\')
        {
          index++;
          continue;
        }

        if (builder == null)
        {
          builder = new StringBuilder();
        }
        builder.Append(json, segmentStart, index - segmentStart);
        index++;
        if (index >= json.Length || !TryAppendJsonEscape(json, ref index, builder))
        {
          return false;
        }
        segmentStart = index;
      }

      return false;
    }

    private static bool TryAppendJsonEscape(
      string json,
      ref int index,
      StringBuilder builder)
    {
      var escape = json[index++];
      switch (escape)
      {
        case '"':
        case '\\':
        case '/':
          builder.Append(escape);
          return true;
        case 'b':
          builder.Append('\b');
          return true;
        case 'f':
          builder.Append('\f');
          return true;
        case 'n':
          builder.Append('\n');
          return true;
        case 'r':
          builder.Append('\r');
          return true;
        case 't':
          builder.Append('\t');
          return true;
        case 'u':
          return TryAppendJsonUnicodeEscape(json, ref index, builder);
        default:
          return false;
      }
    }

    private static bool TryAppendJsonUnicodeEscape(
      string json,
      ref int index,
      StringBuilder builder)
    {
      if (index + 4 > json.Length)
      {
        return false;
      }

      var codeUnit = 0;
      for (var offset = 0; offset < 4; offset++)
      {
        var value = HexValue(json[index + offset]);
        if (value < 0)
        {
          return false;
        }

        codeUnit = codeUnit * 16 + value;
      }

      builder.Append((char)codeUnit);
      index += 4;
      return true;
    }

    private static int HexValue(char value)
    {
      if (value >= '0' && value <= '9')
      {
        return value - '0';
      }
      if (value >= 'a' && value <= 'f')
      {
        return value - 'a' + 10;
      }
      if (value >= 'A' && value <= 'F')
      {
        return value - 'A' + 10;
      }
      return -1;
    }

    private static bool TrySkipJsonValue(string json, ref int index)
    {
      if (index >= json.Length)
      {
        return false;
      }

      if (json[index] == '"')
      {
        return TryReadJsonString(json, ref index, out _);
      }

      if (json[index] == '{' || json[index] == '[')
      {
        return TrySkipJsonComposite(json, ref index);
      }

      var start = index;
      while (index < json.Length && json[index] != ',' && json[index] != '}'
        && json[index] != ']')
      {
        index++;
      }
      return index > start;
    }

    private static bool TrySkipJsonComposite(string json, ref int index)
    {
      var depth = 0;
      while (index < json.Length)
      {
        if (json[index] == '"')
        {
          if (!TryReadJsonString(json, ref index, out _))
          {
            return false;
          }
          continue;
        }

        if (json[index] == '{' || json[index] == '[')
        {
          depth++;
        }
        else if (json[index] == '}' || json[index] == ']')
        {
          depth--;
          index++;
          if (depth == 0)
          {
            return true;
          }
          continue;
        }

        index++;
      }

      return false;
    }

    private static void SkipJsonWhitespace(string json, ref int index)
    {
      while (index < json.Length && (json[index] == ' ' || json[index] == '\t'
        || json[index] == '\r' || json[index] == '\n'))
      {
        index++;
      }
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
