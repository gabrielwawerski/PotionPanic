using NUnit.Framework;
using PotionPanic.Editor.Coordination;

namespace PotionPanic.Tests.EditMode.Coordination
{
  public sealed class CoordinationSnapshotAssemblerTests
  {
    private const string SnapshotId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
    private const string OtherSnapshotId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
    private const string RequestId = "123e4567-e89b-42d3-a456-426614174000";
    private const string OtherRequestId = "223e4567-e89b-42d3-a456-426614174000";
    private const string ServerTime = "2026-08-08T00:00:00Z";

    [Test]
    public void CompletesOutOfOrderChunksWithoutApplyingTheFirstChunk()
    {
      var assembler = new CoordinationSnapshotAssembler();
      var second = Snapshot(SnapshotId, 1, 2, 8);
      var first = Snapshot(SnapshotId, 0, 2, 8);

      Assert.That(assembler.TryAdd(second, 128, out var early, out _),
        Is.EqualTo(CoordinationSnapshotAssemblyStatus.Awaiting));
      Assert.That(early, Is.Null);
      Assert.That(assembler.TryAdd(first, 128, out var completed, out _),
        Is.EqualTo(CoordinationSnapshotAssemblyStatus.Completed));
      Assert.That(completed.snapshotId, Is.EqualTo(SnapshotId));
      Assert.That(completed.requestId, Is.EqualTo(RequestId));
      Assert.That(completed.stateVersion, Is.EqualTo(8));
      Assert.That(completed.serverTime, Is.EqualTo(ServerTime));
      Assert.That(completed.presence, Has.Length.EqualTo(2));
      Assert.That(completed.presence[0].path, Is.EqualTo("assets/chunk-0.asset"));
      Assert.That(completed.presence[1].path, Is.EqualTo("assets/chunk-1.asset"));
      Assert.That(completed.leases, Has.Length.EqualTo(2));
      Assert.That(completed.leases[0].leaseId, Is.EqualTo("lease-0"));
      Assert.That(completed.leases[1].leaseId, Is.EqualTo("lease-1"));
    }

    [Test]
    public void KeepsAnIncompleteSnapshotUnpublished()
    {
      var assembler = new CoordinationSnapshotAssembler();

      var status = assembler.TryAdd(
        Snapshot(SnapshotId, 0, 2, 8), 128, out var completed, out var error);

      Assert.That(status, Is.EqualTo(CoordinationSnapshotAssemblyStatus.Awaiting));
      Assert.That(completed, Is.Null);
      Assert.That(error, Is.Null);
    }

    [Test]
    public void IdenticalDuplicateDoesNotConsumeAggregateCapacity()
    {
      var assembler = new CoordinationSnapshotAssembler();
      const int chunkCount = 16;

      for (var index = 0; index < chunkCount - 1; index++)
      {
        Assert.That(assembler.TryAdd(
          Snapshot(SnapshotId, index, chunkCount, 8), 16 * 1024, out _, out _),
          Is.EqualTo(CoordinationSnapshotAssemblyStatus.Awaiting));
      }

      Assert.That(assembler.TryAdd(
        Snapshot(SnapshotId, 0, chunkCount, 8), 16 * 1024, out _, out _),
        Is.EqualTo(CoordinationSnapshotAssemblyStatus.Duplicate));
      Assert.That(assembler.TryAdd(
        Snapshot(SnapshotId, chunkCount - 1, chunkCount, 8), 16 * 1024,
        out var completed, out _),
        Is.EqualTo(CoordinationSnapshotAssemblyStatus.Completed));
      Assert.That(completed.presence, Has.Length.EqualTo(chunkCount));
    }

    [Test]
    public void ConflictingDuplicateRejectsAndDropsThePartialAssembly()
    {
      var assembler = new CoordinationSnapshotAssembler();
      var conflicting = Snapshot(SnapshotId, 0, 2, 8);
      conflicting.presence[0].displayName = "Different";

      Assert.That(assembler.TryAdd(
        Snapshot(SnapshotId, 0, 2, 8), 128, out _, out _),
        Is.EqualTo(CoordinationSnapshotAssemblyStatus.Awaiting));
      Assert.That(assembler.TryAdd(conflicting, 128, out var completed, out var error),
        Is.EqualTo(CoordinationSnapshotAssemblyStatus.Rejected));
      Assert.That(completed, Is.Null);
      Assert.That(error, Is.EqualTo("snapshot_duplicate_inconsistent"));
      Assert.That(assembler.TryAdd(
        Snapshot(SnapshotId, 1, 2, 8), 128, out _, out _),
        Is.EqualTo(CoordinationSnapshotAssemblyStatus.Awaiting));
    }

    [Test]
    public void DifferentSnapshotIdReplacesAndProcessesTheNewChunk()
    {
      var assembler = new CoordinationSnapshotAssembler();

      Assert.That(assembler.TryAdd(
        Snapshot(SnapshotId, 0, 2, 8), 128, out _, out _),
        Is.EqualTo(CoordinationSnapshotAssemblyStatus.Awaiting));
      Assert.That(assembler.TryAdd(
        Snapshot(OtherSnapshotId, 1, 2, 9), 128, out _, out _),
        Is.EqualTo(CoordinationSnapshotAssemblyStatus.Awaiting));
      Assert.That(assembler.TryAdd(
        Snapshot(OtherSnapshotId, 0, 2, 9), 128, out var completed, out _),
        Is.EqualTo(CoordinationSnapshotAssemblyStatus.Completed));
      Assert.That(completed.snapshotId, Is.EqualTo(OtherSnapshotId));
      Assert.That(completed.stateVersion, Is.EqualTo(9));
      Assert.That(completed.presence, Has.Length.EqualTo(2));
    }

    [TestCase("chunk-count")]
    [TestCase("state-version")]
    [TestCase("request-id-presence")]
    [TestCase("request-id-value")]
    [TestCase("server-time")]
    public void RejectsInconsistentMetadataAndDropsThePartialAssembly(string metadata)
    {
      var assembler = new CoordinationSnapshotAssembler();
      var inconsistent = Snapshot(SnapshotId, 1, 2, 8);
      switch (metadata)
      {
        case "chunk-count":
          inconsistent.chunkCount = 3;
          break;
        case "state-version":
          inconsistent.stateVersion = 9;
          break;
        case "request-id-presence":
          inconsistent.requestId = null;
          break;
        case "request-id-value":
          inconsistent.requestId = OtherRequestId;
          break;
        case "server-time":
          inconsistent.serverTime = "2026-08-08T00:00:01Z";
          break;
      }

      Assert.That(assembler.TryAdd(
        Snapshot(SnapshotId, 0, 2, 8), 128, out _, out _),
        Is.EqualTo(CoordinationSnapshotAssemblyStatus.Awaiting));
      Assert.That(assembler.TryAdd(inconsistent, 128, out _, out var error),
        Is.EqualTo(CoordinationSnapshotAssemblyStatus.Rejected), metadata);
      Assert.That(error, Is.EqualTo("snapshot_metadata_inconsistent"), metadata);
      Assert.That(assembler.TryAdd(
        Snapshot(SnapshotId, 1, 2, 8), 128, out _, out _),
        Is.EqualTo(CoordinationSnapshotAssemblyStatus.Awaiting), metadata);
    }

    [Test]
    public void RejectsAnAggregateOf262145BytesAndClearsTheAssembly()
    {
      var assembler = new CoordinationSnapshotAssembler();
      const int chunkCount = 17;

      for (var index = 0; index < chunkCount - 1; index++)
      {
        Assert.That(assembler.TryAdd(
          Snapshot(SnapshotId, index, chunkCount, 8), 16 * 1024, out _, out _),
          Is.EqualTo(CoordinationSnapshotAssemblyStatus.Awaiting));
      }

      Assert.That(assembler.TryAdd(
        Snapshot(SnapshotId, chunkCount - 1, chunkCount, 8), 1,
        out var completed, out var error),
        Is.EqualTo(CoordinationSnapshotAssemblyStatus.Rejected));
      Assert.That(completed, Is.Null);
      Assert.That(error, Is.EqualTo("snapshot_aggregate_too_large"));
      Assert.That(assembler.TryAdd(
        Snapshot(SnapshotId, chunkCount - 1, chunkCount, 8), 1, out _, out _),
        Is.EqualTo(CoordinationSnapshotAssemblyStatus.Awaiting));
    }

    [Test]
    public void ResetDropsThePartialAssembly()
    {
      var assembler = new CoordinationSnapshotAssembler();
      Assert.That(assembler.TryAdd(
        Snapshot(SnapshotId, 0, 2, 8), 128, out _, out _),
        Is.EqualTo(CoordinationSnapshotAssemblyStatus.Awaiting));

      assembler.Reset();

      Assert.That(assembler.TryAdd(
        Snapshot(SnapshotId, 1, 2, 8), 128, out _, out _),
        Is.EqualTo(CoordinationSnapshotAssemblyStatus.Awaiting));
    }

    private static CoordinationServerEnvelope Snapshot(
      string snapshotId,
      int chunkIndex,
      int chunkCount,
      long stateVersion)
    {
      return new CoordinationServerEnvelope
      {
        protocolVersion = CoordinationProtocol.Version,
        type = "snapshot",
        snapshotId = snapshotId,
        chunkIndex = chunkIndex,
        chunkCount = chunkCount,
        stateVersion = stateVersion,
        requestId = RequestId,
        serverTime = ServerTime,
        presence = new[]
        {
          new CoordinationPresenceRecord
          {
            path = "assets/chunk-" + chunkIndex + ".asset",
            displayPath = "Assets/Chunk-" + chunkIndex + ".asset",
            developerId = "dev-" + chunkIndex,
            displayName = "Developer " + chunkIndex,
            connectionId = "connection-" + chunkIndex,
            branch = "feature/chunk-" + chunkIndex,
            task = "PP-7",
            expiresAt = "2026-08-08T00:02:00Z"
          }
        },
        leases = new[]
        {
          new CoordinationLeaseRecord
          {
            leaseId = "lease-" + chunkIndex,
            path = "assets/chunk-" + chunkIndex + ".asset",
            displayPath = "Assets/Chunk-" + chunkIndex + ".asset",
            mode = "editing",
            developerId = "dev-" + chunkIndex,
            displayName = "Developer " + chunkIndex,
            branch = "feature/chunk-" + chunkIndex,
            task = "PP-7",
            expiresAt = "2026-08-08T00:02:00Z",
            connectionId = "connection-" + chunkIndex
          }
        }
      };
    }
  }
}
