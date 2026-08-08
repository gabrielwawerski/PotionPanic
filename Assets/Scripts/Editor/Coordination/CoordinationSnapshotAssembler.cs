using System;
using System.Collections.Generic;
using UnityEngine;

namespace PotionPanic.Editor.Coordination
{
  public enum CoordinationSnapshotAssemblyStatus
  {
    Awaiting,
    Duplicate,
    Completed,
    Rejected
  }

  public sealed class CoordinationSnapshotAssembler
  {
    public const int MaximumAggregateBytes = 256 * 1024;

    private SnapshotAssembly assembly;

    public CoordinationSnapshotAssemblyStatus TryAdd(
      CoordinationServerEnvelope chunk,
      int serializedUtf8Bytes,
      out CoordinationServerEnvelope completed,
      out string error)
    {
      completed = null;
      error = null;

      if (!IsValidChunk(chunk) || serializedUtf8Bytes < 0)
      {
        Reset();
        error = "snapshot_metadata_inconsistent";
        return CoordinationSnapshotAssemblyStatus.Rejected;
      }

      if (assembly == null || !string.Equals(
        assembly.SnapshotId, chunk.snapshotId, StringComparison.Ordinal))
      {
        assembly = new SnapshotAssembly(chunk);
      }
      else if (!assembly.HasConsistentMetadata(chunk))
      {
        Reset();
        error = "snapshot_metadata_inconsistent";
        return CoordinationSnapshotAssemblyStatus.Rejected;
      }

      var canonicalPayload = JsonUtility.ToJson(chunk);
      if (assembly.Chunks.TryGetValue(chunk.chunkIndex, out var existing))
      {
        if (string.Equals(existing.CanonicalPayload, canonicalPayload,
          StringComparison.Ordinal))
        {
          return CoordinationSnapshotAssemblyStatus.Duplicate;
        }

        Reset();
        error = "snapshot_duplicate_inconsistent";
        return CoordinationSnapshotAssemblyStatus.Rejected;
      }

      if (serializedUtf8Bytes > MaximumAggregateBytes - assembly.SerializedUtf8Bytes)
      {
        Reset();
        error = "snapshot_aggregate_too_large";
        return CoordinationSnapshotAssemblyStatus.Rejected;
      }

      assembly.Chunks.Add(chunk.chunkIndex, new StoredChunk(chunk, canonicalPayload));
      assembly.SerializedUtf8Bytes += serializedUtf8Bytes;
      if (assembly.Chunks.Count != assembly.ChunkCount)
      {
        return CoordinationSnapshotAssemblyStatus.Awaiting;
      }

      completed = AssembleCompletedSnapshot(assembly);
      Reset();
      return CoordinationSnapshotAssemblyStatus.Completed;
    }

    public void Reset()
    {
      assembly = null;
    }

    private static bool IsValidChunk(CoordinationServerEnvelope chunk)
    {
      return chunk != null && chunk.type == "snapshot" && chunk.snapshotId != null
        && chunk.chunkCount > 0 && chunk.chunkIndex >= 0
        && chunk.chunkIndex < chunk.chunkCount
        && chunk.serverTime != null && chunk.presence != null && chunk.leases != null;
    }

    private static CoordinationServerEnvelope AssembleCompletedSnapshot(
      SnapshotAssembly completedAssembly)
    {
      var presence = new List<CoordinationPresenceRecord>();
      var leases = new List<CoordinationLeaseRecord>();
      for (var index = 0; index < completedAssembly.ChunkCount; index++)
      {
        var chunk = completedAssembly.Chunks[index].Envelope;
        presence.AddRange(chunk.presence);
        leases.AddRange(chunk.leases);
      }

      return new CoordinationServerEnvelope
      {
        protocolVersion = CoordinationProtocol.Version,
        type = "snapshot",
        stateVersion = completedAssembly.StateVersion,
        requestId = completedAssembly.RequestId,
        snapshotId = completedAssembly.SnapshotId,
        chunkIndex = 0,
        chunkCount = 1,
        serverTime = completedAssembly.ServerTime,
        presence = presence.ToArray(),
        leases = leases.ToArray()
      };
    }

    private sealed class SnapshotAssembly
    {
      public string SnapshotId { get; }
      public int ChunkCount { get; }
      public long StateVersion { get; }
      public bool HasRequestId { get; }
      public string RequestId { get; }
      public string ServerTime { get; }
      public Dictionary<int, StoredChunk> Chunks { get; }
        = new Dictionary<int, StoredChunk>();
      public int SerializedUtf8Bytes { get; set; }

      public SnapshotAssembly(CoordinationServerEnvelope firstChunk)
      {
        SnapshotId = firstChunk.snapshotId;
        ChunkCount = firstChunk.chunkCount;
        StateVersion = firstChunk.stateVersion;
        HasRequestId = firstChunk.requestId != null;
        RequestId = firstChunk.requestId;
        ServerTime = firstChunk.serverTime;
      }

      public bool HasConsistentMetadata(CoordinationServerEnvelope chunk)
      {
        var chunkHasRequestId = chunk.requestId != null;
        return chunk.chunkCount == ChunkCount && chunk.stateVersion == StateVersion
          && chunkHasRequestId == HasRequestId
          && string.Equals(chunk.requestId, RequestId, StringComparison.Ordinal)
          && string.Equals(chunk.serverTime, ServerTime, StringComparison.Ordinal);
      }
    }

    private sealed class StoredChunk
    {
      public CoordinationServerEnvelope Envelope { get; }
      public string CanonicalPayload { get; }

      public StoredChunk(CoordinationServerEnvelope envelope, string canonicalPayload)
      {
        Envelope = envelope;
        CanonicalPayload = canonicalPayload;
      }
    }
  }
}
