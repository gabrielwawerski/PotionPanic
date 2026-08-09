using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PotionPanic.Editor.Coordination;

namespace PotionPanic.Tests.EditMode.Coordination
{
  public sealed class CoordinationUncoordinatedSaveStoreTests
  {
    private const string StoreFileName = "coordination-uncoordinated-saves.json";
    private static readonly string[] ReasonNames =
    {
      "Manual",
      "Offline",
      "Reconnecting",
      "AuthenticationFailed",
      "RequestTimeout",
      "OverrideTransportFailure"
    };

    private string temporaryDirectory;
    private string destinationPath;
    private FakeClock clock;
    private CoordinationUncoordinatedSaveStore store;

    [SetUp]
    public void SetUp()
    {
      temporaryDirectory = Path.Combine(
        Path.GetTempPath(),
        "PotionPanic",
        Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(temporaryDirectory);
      destinationPath = Path.Combine(temporaryDirectory, StoreFileName);
      clock = new FakeClock(new DateTimeOffset(2026, 8, 9, 10, 0, 0, TimeSpan.Zero));
      store = new CoordinationUncoordinatedSaveStore(destinationPath, clock);
    }

    [TearDown]
    public void TearDown()
    {
      if (File.Exists(temporaryDirectory))
      {
        File.Delete(temporaryDirectory);
      }
      else if (Directory.Exists(temporaryDirectory))
      {
        Directory.Delete(temporaryDirectory, true);
      }
    }

    [Test]
    public void RecordsTheStableReasonNames()
    {
      var reasonType = typeof(CoordinationUncoordinatedSaveStore).Assembly.GetType(
        "PotionPanic.Editor.Coordination.CoordinationUncoordinatedSaveReason");

      Assert.That(reasonType, Is.Not.Null);
      Assert.That(reasonType.IsEnum, Is.True);
      Assert.That(Enum.GetNames(reasonType), Is.EqualTo(ReasonNames));
    }

    [Test]
    public void ANewPathCreatesOneRecordWithCountOne()
    {
      var ledger = new CoordinationUncoordinatedSaveLedger(store, clock);

      Assert.That(ledger.RecordSave(
        "Assets\\Scenes\\Laboratory.unity", "Offline", "Alex", "feature/lab", "PP-9"),
        Is.True);

      var record = ledger.Records.Single();
      Assert.That(record.path, Is.EqualTo("Assets/Scenes/Laboratory.unity"));
      Assert.That(record.firstSavedAtUtc, Is.EqualTo("2026-08-09T10:00:00.0000000+00:00"));
      Assert.That(record.latestSavedAtUtc, Is.EqualTo(record.firstSavedAtUtc));
      Assert.That(record.saveCount, Is.EqualTo(1));
      Assert.That(record.reason, Is.EqualTo("Offline"));
      Assert.That(record.lastKnownOwner, Is.EqualTo("Alex"));
      Assert.That(record.branch, Is.EqualTo("feature/lab"));
      Assert.That(record.task, Is.EqualTo("PP-9"));
    }

    [Test]
    public void ARepeatedNormalizedPathPreservesFirstSaveAndUpdatesLatestFields()
    {
      var ledger = new CoordinationUncoordinatedSaveLedger(store, clock);
      ledger.RecordSave("Assets/Scenes/Laboratory.unity", "Offline", "Alex", "feature/one", "PP-9");
      clock.SetUtcNow(new DateTimeOffset(2026, 8, 9, 10, 5, 0, TimeSpan.Zero));

      Assert.That(ledger.RecordSave(
        "Assets\\Scenes\\Laboratory.unity", "RequestTimeout", "Blair", "feature/two", "PP-10"),
        Is.True);

      var record = ledger.Records.Single();
      Assert.That(record.firstSavedAtUtc, Is.EqualTo("2026-08-09T10:00:00.0000000+00:00"));
      Assert.That(record.latestSavedAtUtc, Is.EqualTo("2026-08-09T10:05:00.0000000+00:00"));
      Assert.That(record.saveCount, Is.EqualTo(2));
      Assert.That(record.reason, Is.EqualTo("RequestTimeout"));
      Assert.That(record.lastKnownOwner, Is.EqualTo("Blair"));
      Assert.That(record.branch, Is.EqualTo("feature/two"));
      Assert.That(record.task, Is.EqualTo("PP-10"));
    }

    [Test]
    public void DifferentPathsCreateSeparateRecords()
    {
      var ledger = new CoordinationUncoordinatedSaveLedger(store, clock);

      ledger.RecordSave("Assets/Scenes/Laboratory.unity", "Manual", "", "", "");
      ledger.RecordSave("Assets/Prefabs/Player.prefab", "Reconnecting", "", "", "");

      Assert.That(ledger.Records.Select(record => record.path), Is.EquivalentTo(new[]
      {
        "Assets/Scenes/Laboratory.unity",
        "Assets/Prefabs/Player.prefab"
      }));
    }

    [Test]
    public void JsonRoundTripsEveryReasonAsItsStableName()
    {
      var ledger = new CoordinationUncoordinatedSaveLedger(store, clock);
      for (var index = 0; index < ReasonNames.Length; index += 1)
      {
        ledger.RecordSave(
          "Assets/Scenes/Reason" + index + ".unity", ReasonNames[index], "", "", "");
      }

      var reloaded = new CoordinationUncoordinatedSaveStore(destinationPath, clock).Load();

      Assert.That(reloaded.Records.Select(record => record.reason), Is.EqualTo(ReasonNames));
      Assert.That(File.ReadAllText(destinationPath), Does.Contain("\"reason\": \"Manual\""));
    }

    [Test]
    public void SerializedOutputContainsNoCredentialRelatedProperties()
    {
      var ledger = new CoordinationUncoordinatedSaveLedger(store, clock);
      ledger.RecordSave("Assets/Scenes/Laboratory.unity", "Offline", "Alex", "feature/lab", "PP-9");

      var serializedJson = File.ReadAllText(destinationPath);
      var json = serializedJson.ToLowerInvariant();

      Assert.That(serializedJson, Does.Contain("\"schemaVersion\": 1"));
      Assert.That(json, Does.Not.Contain("token"));
      Assert.That(json, Does.Not.Contain("secret"));
      Assert.That(json, Does.Not.Contain("credential"));
      Assert.That(json, Does.Not.Contain("authorization"));
      Assert.That(json, Does.Not.Contain("bearer"));
      UnityEngine.Debug.Log("UNCOORDINATED_SAVE_JSON="
        + serializedJson.Replace("\r", string.Empty).Replace("\n", string.Empty));
    }

    [Test]
    public void MalformedJsonIsQuarantinedAndLoadsAnEmptyLedger()
    {
      File.WriteAllText(destinationPath, "{");

      var result = store.Load();

      Assert.That(result.Records, Is.Empty);
      Assert.That(result.QuarantinePath, Is.Not.Null.And.Not.Empty);
      Assert.That(result.QuarantinePath, Does.Match(
        ".*coordination-uncoordinated-saves\\.invalid-20260809T1000000000000Z\\.json$"));
      Assert.That(File.Exists(result.QuarantinePath), Is.True);
      Assert.That(File.Exists(destinationPath), Is.False);
    }

    [Test]
    public void FailedWriteRetainsTheWarningInMemoryAndExposesAnError()
    {
      var blockingPath = Path.Combine(temporaryDirectory, "blocked");
      File.WriteAllText(blockingPath, "not a directory");
      var blockedStore = new CoordinationUncoordinatedSaveStore(
        Path.Combine(blockingPath, StoreFileName), clock);
      var ledger = new CoordinationUncoordinatedSaveLedger(blockedStore, clock);

      Assert.That(ledger.RecordSave("Assets/Scenes/Laboratory.unity", "Offline", "", "", ""),
        Is.False);

      Assert.That(ledger.Records.Single().path, Is.EqualTo("Assets/Scenes/Laboratory.unity"));
      Assert.That(ledger.PersistentError, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void ReconciliationRemovesARecordOnlyAfterTheUpdatedSetSaves()
    {
      var ledger = new CoordinationUncoordinatedSaveLedger(store, clock);
      ledger.RecordSave("Assets/Scenes/Laboratory.unity", "Offline", "", "", "");
      ledger.RecordSave("Assets/Prefabs/Player.prefab", "Manual", "", "", "");

      Assert.That(ledger.ReconcilePath("Assets\\Scenes\\Laboratory.unity"), Is.True);

      Assert.That(ledger.Records.Select(record => record.path), Is.EqualTo(new[]
      {
        "Assets/Prefabs/Player.prefab"
      }));
      Assert.That(store.Load().Records.Select(record => record.path), Is.EqualTo(new[]
      {
        "Assets/Prefabs/Player.prefab"
      }));
    }

    [Test]
    public void ReconciliationWriteFailureLeavesTheRecordVisible()
    {
      var ledger = new CoordinationUncoordinatedSaveLedger(store, clock);
      ledger.RecordSave("Assets/Scenes/Laboratory.unity", "Offline", "", "", "");
      Directory.Delete(temporaryDirectory, true);
      File.WriteAllText(temporaryDirectory, "not a directory");

      Assert.That(ledger.ReconcilePath("Assets/Scenes/Laboratory.unity"), Is.False);

      Assert.That(ledger.Records.Single().path, Is.EqualTo("Assets/Scenes/Laboratory.unity"));
      Assert.That(ledger.PersistentError, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void RecoveredAccessDoesNotOverwriteWarningsThatFailedToLoad()
    {
      var persistedLedger = new CoordinationUncoordinatedSaveLedger(store, clock);
      persistedLedger.RecordSave(
        "Assets/Scenes/Laboratory.unity", "Offline", "", "", "");

      CoordinationUncoordinatedSaveLedger unreadLedger;
      using (new FileStream(destinationPath, FileMode.Open, FileAccess.Read, FileShare.None))
      {
        unreadLedger = new CoordinationUncoordinatedSaveLedger(store, clock);
      }

      Assert.That(unreadLedger.RecordSave(
        "Assets/Prefabs/Player.prefab", "Manual", "", "", ""), Is.False);
      Assert.That(unreadLedger.Records.Single().path,
        Is.EqualTo("Assets/Prefabs/Player.prefab"));
      Assert.That(unreadLedger.PersistentError, Is.Not.Null.And.Not.Empty);
      Assert.That(store.Load().Records.Select(record => record.path), Is.EqualTo(new[]
      {
        "Assets/Scenes/Laboratory.unity"
      }));
    }

    [Test]
    public void ReconciliationCannotSucceedWhilePersistedWarningsAreUnread()
    {
      var persistedLedger = new CoordinationUncoordinatedSaveLedger(store, clock);
      persistedLedger.RecordSave(
        "Assets/Scenes/Laboratory.unity", "Offline", "", "", "");

      CoordinationUncoordinatedSaveLedger unreadLedger;
      using (new FileStream(destinationPath, FileMode.Open, FileAccess.Read, FileShare.None))
      {
        unreadLedger = new CoordinationUncoordinatedSaveLedger(store, clock);
      }
      var loadError = unreadLedger.PersistentError;

      Assert.That(
        unreadLedger.ReconcilePath("Assets/Scenes/Laboratory.unity"),
        Is.False);

      Assert.That(unreadLedger.PersistentError, Is.EqualTo(loadError));
      Assert.That(store.Load().Records.Single().path,
        Is.EqualTo("Assets/Scenes/Laboratory.unity"));
    }

    [Test]
    public void LoadReturnsCanonicalPathsFromOtherwiseValidJson()
    {
      const string json = "{\"schemaVersion\":1,\"records\":[{"
        + "\"path\":\"Assets\\\\Scenes\\\\Laboratory.unity\","
        + "\"firstSavedAtUtc\":\"2026-08-09T10:00:00.0000000+00:00\","
        + "\"latestSavedAtUtc\":\"2026-08-09T10:00:00.0000000+00:00\","
        + "\"saveCount\":1,\"reason\":\"Offline\",\"lastKnownOwner\":\"\","
        + "\"branch\":\"\",\"task\":\"\"}]}";
      File.WriteAllText(destinationPath, json);

      var result = store.Load();

      Assert.That(result.Records.Single().path,
        Is.EqualTo("Assets/Scenes/Laboratory.unity"));
    }

    [Test]
    public void NoOpReconciliationPreservesAnUnresolvedWriteError()
    {
      var blockingPath = Path.Combine(temporaryDirectory, "blocked");
      File.WriteAllText(blockingPath, "not a directory");
      var blockedStore = new CoordinationUncoordinatedSaveStore(
        Path.Combine(blockingPath, StoreFileName), clock);
      var ledger = new CoordinationUncoordinatedSaveLedger(blockedStore, clock);
      ledger.RecordSave("Assets/Scenes/Laboratory.unity", "Offline", "", "", "");
      var writeError = ledger.PersistentError;

      Assert.That(ledger.ReconcilePath("Assets/Prefabs/Player.prefab"), Is.True);

      Assert.That(ledger.PersistentError, Is.EqualTo(writeError));
    }

    private sealed class FakeClock : ICoordinationClock
    {
      public DateTimeOffset UtcNow { get; private set; }

      public FakeClock(DateTimeOffset utcNow)
      {
        UtcNow = utcNow;
      }

      public void SetUtcNow(DateTimeOffset utcNow)
      {
        UtcNow = utcNow;
      }
    }
  }
}
