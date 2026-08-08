using System;
using NUnit.Framework;
using PotionPanic.Editor.Coordination;

namespace PotionPanic.Tests.EditMode.Coordination
{
  public sealed class CoordinationAssetTrackerTests
  {
    [TestCase("")]
    [TestCase("Assets\\Scenes\\Laboratory.unity")]
    [TestCase("Assets/Scenes/../Laboratory.unity")]
    [TestCase("C:/Projects/PotionPanic/Assets/Scenes/Laboratory.unity")]
    [TestCase("../Assets/Scenes/Laboratory.unity")]
    [TestCase("Packages/Scenes/Laboratory.unity")]
    public void RejectsAPathThatCannotIdentifyANormalizedUnityStage(string path)
    {
      Assert.That(() => new CoordinationStageInfo(CoordinationStageKind.Scene, path, false),
        Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void KeepsTheSceneIdentityAndDirtyStateForAValidScenePath()
    {
      var stage = new CoordinationStageInfo(CoordinationStageKind.Scene,
        "Assets/Scenes/Laboratory.unity", true);

      Assert.That(stage.Kind, Is.EqualTo(CoordinationStageKind.Scene));
      Assert.That(stage.Path, Is.EqualTo("Assets/Scenes/Laboratory.unity"));
      Assert.That(stage.IsDirty, Is.True);
    }

    [Test]
    public void RejectsAPrefabPathForASceneStage()
    {
      Assert.That(() => new CoordinationStageInfo(CoordinationStageKind.Scene,
          "Assets/Prefabs/Potion.prefab", false),
        Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void EvaluatesTheFirstEnabledRuleAndPreservesItsExclusiveFlag()
    {
      var stage = new CoordinationStageInfo(CoordinationStageKind.Scene,
        "Assets/Scenes/Laboratory.unity", false);
      var rules = new[]
      {
        Rule("Assets/Scenes/**/*.unity", false, false),
        Rule("Assets/Scenes/**/*.unity", true, true)
      };

      Assert.That(CoordinationStageEvaluator.TryEvaluate(stage, rules, out var evaluation), Is.True);
      Assert.That(evaluation.Rule, Is.SameAs(rules[1]));
      Assert.That(evaluation.IsExclusive, Is.True);
    }

    [Test]
    public void DoesNotEvaluateAPrefabWhenTheAllowlistIsEmpty()
    {
      var prefab = new CoordinationStageInfo(CoordinationStageKind.Prefab,
        "Assets/Prefabs/Potion.prefab", false);

      Assert.That(CoordinationStageEvaluator.TryEvaluate(prefab,
        Array.Empty<CoordinatedPathRule>(), out _), Is.False);
    }

    [Test]
    public void ClassifiesAMatchingEditingLeaseAsLocallyOwned()
    {
      var stage = EvaluateScene();

      stage.ApplyLease(Lease("editing", "dev-local", "connection-local"),
        new CoordinationLocalIdentity("dev-local", "connection-local"));

      Assert.That(stage.LeaseOwnership, Is.EqualTo(CoordinationLeaseOwnership.OwnedEditing));
    }

    [Test]
    public void ClassifiesAnEditingLeaseFromAnotherConnectionAsOtherOwned()
    {
      var stage = EvaluateScene();

      stage.ApplyLease(Lease("editing", "dev-local", "connection-other"),
        new CoordinationLocalIdentity("dev-local", "connection-local"));

      Assert.That(stage.LeaseOwnership, Is.EqualTo(CoordinationLeaseOwnership.OtherEditing));
    }

    [Test]
    public void ClassifiesAReservationSeparatelyFromAnEditingLease()
    {
      var stage = EvaluateScene();

      stage.ApplyLease(Lease("reserved", "dev-local", null),
        new CoordinationLocalIdentity("dev-local", "connection-local"));

      Assert.That(stage.LeaseOwnership, Is.EqualTo(CoordinationLeaseOwnership.Reserved));
    }

    [Test]
    public void DoesNotApplyOwnershipFromALeaseForAnotherStage()
    {
      var stage = EvaluateScene();
      var lease = Lease("editing", "dev-local", "connection-local");
      lease.path = "Assets/Scenes/Arena.unity";

      stage.ApplyLease(lease, new CoordinationLocalIdentity("dev-local", "connection-local"));

      Assert.That(stage.LeaseOwnership, Is.EqualTo(CoordinationLeaseOwnership.None));
    }

    [Test]
    public void AppliesTheCurrentSnapshotForLeaseAndPresenceQueries()
    {
      var store = new CoordinationStateStore();
      store.ApplySnapshot(new CoordinationServerEnvelope
      {
        type = "snapshot",
        stateVersion = 4,
        presence = new[] { Presence("connection-local") },
        leases = new[] { Lease("editing", "dev-local", "connection-local") }
      });

      Assert.That(store.TryGetLease("Assets/Scenes/Laboratory.unity", out var lease), Is.True);
      Assert.That(lease.developerId, Is.EqualTo("dev-local"));
      Assert.That(store.GetPresence("Assets/Scenes/Laboratory.unity"), Has.Count.EqualTo(1));
      Assert.That(store.NewestStateVersion, Is.EqualTo(4));
    }

    [Test]
    public void AppliesAPresenceUpdateAndThenRemovesThatConnection()
    {
      var store = new CoordinationStateStore();
      store.ApplySnapshot(Snapshot(1));
      store.ApplyPresenceUpdate(new CoordinationServerEnvelope
      {
        type = "presence.updated",
        stateVersion = 2,
        presence = new[] { Presence("connection-remote") }
      });
      store.ApplyPresenceRemoval(new CoordinationServerEnvelope
      {
        type = "presence.removed",
        stateVersion = 3,
        path = "Assets/Scenes/Laboratory.unity",
        connectionId = "connection-remote"
      });

      Assert.That(store.GetPresence("Assets/Scenes/Laboratory.unity"), Is.Empty);
      Assert.That(store.NewestStateVersion, Is.EqualTo(3));
    }

    [Test]
    public void AppliesTheLeaseRecordFromACurrentLeaseUpdate()
    {
      var store = new CoordinationStateStore();
      store.ApplySnapshot(Snapshot(1));
      store.ApplyLeaseUpdate(new CoordinationServerEnvelope
      {
        type = "lease.updated",
        stateVersion = 2,
        lease = Lease("editing", "dev-remote", "connection-remote")
      });

      Assert.That(store.TryGetLease("Assets/Scenes/Laboratory.unity", out var lease), Is.True);
      Assert.That(lease.developerId, Is.EqualTo("dev-remote"));
    }

    [Test]
    public void DoesNotApplyAStaleCorrelatedLeaseResult()
    {
      var store = new CoordinationStateStore();
      store.ApplySnapshot(new CoordinationServerEnvelope
      {
        type = "snapshot",
        stateVersion = 10,
        presence = Array.Empty<CoordinationPresenceRecord>(),
        leases = new[] { Lease("editing", "dev-current", "connection-current") }
      });

      var applied = store.ApplyLeaseResult(new CoordinationServerEnvelope
      {
        type = "lease.granted",
        stateVersion = 9,
        path = "Assets/Scenes/Laboratory.unity",
        lease = Lease("editing", "dev-stale", "connection-stale")
      }, true);

      Assert.That(applied, Is.False);
      Assert.That(store.TryGetLease("Assets/Scenes/Laboratory.unity", out var lease), Is.True);
      Assert.That(lease.developerId, Is.EqualTo("dev-current"));
      Assert.That(store.NewestStateVersion, Is.EqualTo(10));
    }

    private static CoordinatedStage EvaluateScene()
    {
      var scene = new CoordinationStageInfo(CoordinationStageKind.Scene,
        "Assets/Scenes/Laboratory.unity", true);
      var rules = new[] { Rule("Assets/Scenes/**/*.unity", true, true) };
      Assert.That(CoordinationStageEvaluator.TryEvaluate(scene, rules, out var evaluation), Is.True);
      return evaluation;
    }

    private static CoordinatedPathRule Rule(string pattern, bool enabled, bool exclusive)
    {
      return new CoordinatedPathRule { pattern = pattern, enabled = enabled, exclusive = exclusive };
    }

    private static CoordinationServerEnvelope Snapshot(long stateVersion)
    {
      return new CoordinationServerEnvelope
      {
        type = "snapshot",
        stateVersion = stateVersion,
        presence = Array.Empty<CoordinationPresenceRecord>(),
        leases = Array.Empty<CoordinationLeaseRecord>()
      };
    }

    private static CoordinationPresenceRecord Presence(string connectionId)
    {
      return new CoordinationPresenceRecord
      {
        path = "Assets/Scenes/Laboratory.unity",
        connectionId = connectionId,
        developerId = "dev-remote",
        displayName = "Remote Developer",
        branch = "feature/test",
        task = "PP-7",
        displayPath = "Assets/Scenes/Laboratory.unity",
        expiresAt = "2026-08-08T12:00:00Z"
      };
    }

    private static CoordinationLeaseRecord Lease(string mode, string developerId, string connectionId)
    {
      return new CoordinationLeaseRecord
      {
        leaseId = "lease-1",
        path = "Assets/Scenes/Laboratory.unity",
        displayPath = "Assets/Scenes/Laboratory.unity",
        mode = mode,
        developerId = developerId,
        displayName = developerId,
        branch = "feature/test",
        task = "PP-7",
        expiresAt = "2026-08-08T12:00:00Z",
        connectionId = connectionId
      };
    }
  }
}
