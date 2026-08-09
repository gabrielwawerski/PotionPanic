using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

[assembly: InternalsVisibleTo("PotionPanic.EditModeTests")]

namespace PotionPanic.Editor.Coordination
{
  internal enum CoordinationUncoordinatedSaveReason
  {
    Manual,
    Offline,
    Reconnecting,
    AuthenticationFailed,
    RequestTimeout,
    OverrideTransportFailure,
  }

  [Serializable]
  internal sealed class CoordinationUncoordinatedSaveRecord
  {
    public string path;
    public string firstSavedAtUtc;
    public string latestSavedAtUtc;
    public int saveCount;
    public string reason;
    public string lastKnownOwner;
    public string branch;
    public string task;

    public CoordinationUncoordinatedSaveRecord Copy()
    {
      return new CoordinationUncoordinatedSaveRecord
      {
        path = path,
        firstSavedAtUtc = firstSavedAtUtc,
        latestSavedAtUtc = latestSavedAtUtc,
        saveCount = saveCount,
        reason = reason,
        lastKnownOwner = lastKnownOwner,
        branch = branch,
        task = task
      };
    }
  }

  internal sealed class CoordinationUncoordinatedSaveLoadResult
  {
    public IReadOnlyList<CoordinationUncoordinatedSaveRecord> Records { get; }
    public string QuarantinePath { get; }
    public string Error { get; }

    public CoordinationUncoordinatedSaveLoadResult(
      IReadOnlyList<CoordinationUncoordinatedSaveRecord> records,
      string quarantinePath,
      string error)
    {
      Records = records ?? Array.Empty<CoordinationUncoordinatedSaveRecord>();
      QuarantinePath = quarantinePath;
      Error = error;
    }
  }

  internal sealed class CoordinationUncoordinatedSaveWriteResult
  {
    public bool Succeeded { get; }
    public string Error { get; }

    private CoordinationUncoordinatedSaveWriteResult(bool succeeded, string error)
    {
      Succeeded = succeeded;
      Error = error;
    }

    public static CoordinationUncoordinatedSaveWriteResult Success()
    {
      return new CoordinationUncoordinatedSaveWriteResult(true, null);
    }

    public static CoordinationUncoordinatedSaveWriteResult Failure(string error)
    {
      return new CoordinationUncoordinatedSaveWriteResult(false, error);
    }
  }

  internal interface ICoordinationUncoordinatedSaveStore
  {
    CoordinationUncoordinatedSaveLoadResult Load();
    CoordinationUncoordinatedSaveWriteResult Save(
      IReadOnlyList<CoordinationUncoordinatedSaveRecord> records);
  }

  internal sealed class CoordinationUncoordinatedSaveStore
    : ICoordinationUncoordinatedSaveStore
  {
    private const int CurrentSchemaVersion = 1;
    private const string RelativePath =
      "UserSettings/PotionPanic/coordination-uncoordinated-saves.json";

    private readonly string destinationPath;
    private readonly ICoordinationClock clock;
    private readonly Func<string, string> readAllText;

    public CoordinationUncoordinatedSaveStore()
      : this(
        GetDefaultDestinationPath(),
        new SystemCoordinationClock(),
        File.ReadAllText)
    {
    }

    public CoordinationUncoordinatedSaveStore(
      string destinationPath,
      ICoordinationClock clock)
      : this(destinationPath, clock, File.ReadAllText)
    {
    }

    public CoordinationUncoordinatedSaveStore(
      string destinationPath,
      ICoordinationClock clock,
      Func<string, string> readAllText)
    {
      if (string.IsNullOrWhiteSpace(destinationPath))
      {
        throw new ArgumentException("A destination path is required.", nameof(destinationPath));
      }

      this.destinationPath = Path.GetFullPath(destinationPath);
      this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
      this.readAllText = readAllText ?? throw new ArgumentNullException(nameof(readAllText));
    }

    public CoordinationUncoordinatedSaveLoadResult Load()
    {
      try
      {
        var document = JsonUtility.FromJson<CoordinationUncoordinatedSaveDocument>(
          readAllText(destinationPath));
        if (!TryValidateDocument(document, out var records, out var error))
        {
          return QuarantineInvalidFile(error);
        }

        return new CoordinationUncoordinatedSaveLoadResult(
          records,
          null,
          null);
      }
      catch (FileNotFoundException)
      {
        return EmptyLoadResult();
      }
      catch (DirectoryNotFoundException)
      {
        return EmptyLoadResult();
      }
      catch (ArgumentException exception)
      {
        return QuarantineInvalidFile("Saved warnings are not valid JSON: "
          + exception.Message);
      }
      catch (IOException exception)
      {
        return new CoordinationUncoordinatedSaveLoadResult(
          Array.Empty<CoordinationUncoordinatedSaveRecord>(),
          null,
          "Could not read saved uncoordinated warnings: " + exception.Message);
      }
      catch (UnauthorizedAccessException exception)
      {
        return new CoordinationUncoordinatedSaveLoadResult(
          Array.Empty<CoordinationUncoordinatedSaveRecord>(),
          null,
          "Could not read saved uncoordinated warnings: " + exception.Message);
      }
    }

    public CoordinationUncoordinatedSaveWriteResult Save(
      IReadOnlyList<CoordinationUncoordinatedSaveRecord> records)
    {
      if (records == null)
      {
        return CoordinationUncoordinatedSaveWriteResult.Failure(
          "Could not save uncoordinated warnings because the record set is missing.");
      }

      if (!TryCopyValidatedRecords(records, out var safeRecords, out var validationError))
      {
        return CoordinationUncoordinatedSaveWriteResult.Failure(validationError);
      }

      var directory = Path.GetDirectoryName(destinationPath);
      var temporaryPath = Path.Combine(
        directory,
        Path.GetFileName(destinationPath) + ".tmp-" + Guid.NewGuid().ToString("N"));
      try
      {
        Directory.CreateDirectory(directory);
        var document = new CoordinationUncoordinatedSaveDocument
        {
          schemaVersion = CurrentSchemaVersion,
          records = safeRecords
        };
        var json = JsonUtility.ToJson(document, true);
        WriteFlushedFile(temporaryPath, json);

        if (File.Exists(destinationPath))
        {
          File.Replace(temporaryPath, destinationPath, null);
        }
        else
        {
          File.Move(temporaryPath, destinationPath);
        }

        return CoordinationUncoordinatedSaveWriteResult.Success();
      }
      catch (Exception exception) when (IsFileSystemException(exception))
      {
        return CoordinationUncoordinatedSaveWriteResult.Failure(
          "Could not persist uncoordinated warnings: " + exception.Message);
      }
      finally
      {
        TryDeleteTemporaryFile(temporaryPath);
      }
    }

    private CoordinationUncoordinatedSaveLoadResult QuarantineInvalidFile(string error)
    {
      var directory = Path.GetDirectoryName(destinationPath);
      var fileName = Path.GetFileNameWithoutExtension(destinationPath);
      var timestamp = clock.UtcNow.ToUniversalTime().ToString(
        "yyyyMMdd'T'HHmmssfffffff'Z'",
        CultureInfo.InvariantCulture);
      var quarantinePath = Path.Combine(
        directory,
        fileName + ".invalid-" + timestamp + ".json");

      try
      {
        File.Move(destinationPath, quarantinePath);
        return new CoordinationUncoordinatedSaveLoadResult(
          Array.Empty<CoordinationUncoordinatedSaveRecord>(),
          quarantinePath,
          error);
      }
      catch (Exception exception) when (IsFileSystemException(exception))
      {
        return new CoordinationUncoordinatedSaveLoadResult(
          Array.Empty<CoordinationUncoordinatedSaveRecord>(),
          null,
          error + " The invalid file could not be quarantined: " + exception.Message);
      }
    }

    private static CoordinationUncoordinatedSaveLoadResult EmptyLoadResult()
    {
      return new CoordinationUncoordinatedSaveLoadResult(
        Array.Empty<CoordinationUncoordinatedSaveRecord>(),
        null,
        null);
    }

    private static bool TryValidateDocument(
      CoordinationUncoordinatedSaveDocument document,
      out CoordinationUncoordinatedSaveRecord[] records,
      out string error)
    {
      if (document == null || document.schemaVersion != CurrentSchemaVersion
        || document.records == null)
      {
        records = null;
        error = "Saved warnings have a missing or unsupported schema.";
        return false;
      }

      return TryCopyValidatedRecords(document.records, out records, out error);
    }

    private static bool TryCopyValidatedRecords(
      IReadOnlyList<CoordinationUncoordinatedSaveRecord> records,
      out CoordinationUncoordinatedSaveRecord[] copies,
      out string error)
    {
      copies = new CoordinationUncoordinatedSaveRecord[records.Count];
      var canonicalPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      for (var index = 0; index < records.Count; index += 1)
      {
        var record = records[index];
        if (record == null
          || !CoordinationPathMatcher.TryNormalize(record.path, out var normalizedPath)
          || !canonicalPaths.Add(normalizedPath)
          || !IsStableReason(record.reason)
          || !TryParseUtc(record.firstSavedAtUtc, out var firstSavedAtUtc)
          || !TryParseUtc(record.latestSavedAtUtc, out var latestSavedAtUtc)
          || firstSavedAtUtc > latestSavedAtUtc
          || record.saveCount < 1)
        {
          copies = null;
          error = "Saved warnings contain an invalid record.";
          return false;
        }

        copies[index] = record.Copy();
        copies[index].path = normalizedPath;
        copies[index].lastKnownOwner = record.lastKnownOwner ?? string.Empty;
        copies[index].branch = record.branch ?? string.Empty;
        copies[index].task = record.task ?? string.Empty;
      }

      error = null;
      return true;
    }

    private static bool IsStableReason(string reason)
    {
      return Enum.TryParse(reason, false, out CoordinationUncoordinatedSaveReason parsed)
        && Enum.IsDefined(typeof(CoordinationUncoordinatedSaveReason), parsed)
        && Enum.GetName(typeof(CoordinationUncoordinatedSaveReason), parsed) == reason;
    }

    private static bool TryParseUtc(string value, out DateTimeOffset parsed)
    {
      return DateTimeOffset.TryParseExact(
        value,
        "O",
        CultureInfo.InvariantCulture,
        DateTimeStyles.RoundtripKind,
        out parsed)
        && parsed.Offset == TimeSpan.Zero;
    }

    private static void WriteFlushedFile(string path, string contents)
    {
      using (var stream = new FileStream(
        path,
        FileMode.CreateNew,
        FileAccess.Write,
        FileShare.None))
      using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
      {
        writer.Write(contents);
        writer.Flush();
        stream.Flush(true);
      }
    }

    private static void TryDeleteTemporaryFile(string path)
    {
      try
      {
        if (File.Exists(path))
        {
          File.Delete(path);
        }
      }
      catch (Exception exception) when (IsFileSystemException(exception))
      {
      }
    }

    private static bool IsFileSystemException(Exception exception)
    {
      return exception is IOException
        || exception is UnauthorizedAccessException
        || exception is NotSupportedException;
    }

    private static string GetDefaultDestinationPath()
    {
      var projectDirectory = Directory.GetParent(Application.dataPath).FullName;
      return Path.Combine(projectDirectory, RelativePath);
    }
  }

  internal sealed class CoordinationUncoordinatedSaveLedger
  {
    private readonly ICoordinationUncoordinatedSaveStore store;
    private readonly ICoordinationClock clock;
    private readonly bool hasUnreadPersistentState;
    private List<CoordinationUncoordinatedSaveRecord> records;

    public IReadOnlyList<CoordinationUncoordinatedSaveRecord> Records =>
      records.Select(record => record.Copy()).ToArray();

    public string PersistentError { get; private set; }

    public CoordinationUncoordinatedSaveLedger(
      ICoordinationUncoordinatedSaveStore store,
      ICoordinationClock clock)
    {
      this.store = store ?? throw new ArgumentNullException(nameof(store));
      this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
      var loadResult = store.Load();
      records = loadResult.Records.Select(record => record.Copy()).ToList();
      PersistentError = loadResult.Error;
      hasUnreadPersistentState = loadResult.Error != null
        && string.IsNullOrEmpty(loadResult.QuarantinePath);
    }

    public bool RecordSave(
      string path,
      string reason,
      string lastKnownOwner,
      string branch,
      string task)
    {
      if (!CoordinationPathMatcher.TryNormalize(path, out var normalizedPath))
      {
        PersistentError = "Could not record an uncoordinated save with an invalid path.";
        return false;
      }
      if (!CoordinationUncoordinatedSaveStoreReason.TryParse(reason, out var parsedReason))
      {
        PersistentError = "Could not record an uncoordinated save with an invalid reason.";
        return false;
      }

      return RecordSave(
        normalizedPath,
        parsedReason,
        lastKnownOwner,
        branch,
        task);
    }

    public bool RecordSave(
      string path,
      CoordinationUncoordinatedSaveReason reason,
      string lastKnownOwner,
      string branch,
      string task)
    {
      if (!CoordinationPathMatcher.TryNormalize(path, out var normalizedPath))
      {
        PersistentError = "Could not record an uncoordinated save with an invalid path.";
        return false;
      }

      var savedAtUtc = clock.UtcNow.ToUniversalTime().ToString(
        "O",
        CultureInfo.InvariantCulture);
      var record = records.FirstOrDefault(candidate => string.Equals(
        candidate.path,
        normalizedPath,
        StringComparison.OrdinalIgnoreCase));
      if (record == null)
      {
        records.Add(new CoordinationUncoordinatedSaveRecord
        {
          path = normalizedPath,
          firstSavedAtUtc = savedAtUtc,
          latestSavedAtUtc = savedAtUtc,
          saveCount = 1,
          reason = reason.ToString(),
          lastKnownOwner = lastKnownOwner ?? string.Empty,
          branch = branch ?? string.Empty,
          task = task ?? string.Empty
        });
      }
      else
      {
        record.latestSavedAtUtc = savedAtUtc;
        record.saveCount += 1;
        record.reason = reason.ToString();
        record.lastKnownOwner = lastKnownOwner ?? string.Empty;
        record.branch = branch ?? string.Empty;
        record.task = task ?? string.Empty;
      }

      if (hasUnreadPersistentState)
      {
        return false;
      }

      var result = store.Save(records);
      PersistentError = result.Error;
      return result.Succeeded;
    }

    public bool ReconcilePath(string path)
    {
      if (!CoordinationPathMatcher.TryNormalize(path, out var normalizedPath))
      {
        PersistentError = "Could not reconcile an invalid asset path.";
        return false;
      }

      var updatedRecords = records.Where(record => !string.Equals(
        record.path,
        normalizedPath,
        StringComparison.OrdinalIgnoreCase)).ToList();
      if (hasUnreadPersistentState)
      {
        return false;
      }
      if (updatedRecords.Count == records.Count)
      {
        return true;
      }

      var result = store.Save(updatedRecords);
      PersistentError = result.Error;
      if (!result.Succeeded)
      {
        return false;
      }

      records = updatedRecords;
      return true;
    }
  }

  internal static class CoordinationUncoordinatedSaveStoreReason
  {
    public static bool TryParse(
      string reason,
      out CoordinationUncoordinatedSaveReason parsed)
    {
      return Enum.TryParse(reason, false, out parsed)
        && Enum.IsDefined(typeof(CoordinationUncoordinatedSaveReason), parsed)
        && Enum.GetName(typeof(CoordinationUncoordinatedSaveReason), parsed) == reason;
    }
  }

  [Serializable]
  internal sealed class CoordinationUncoordinatedSaveDocument
  {
    public int schemaVersion;
    public CoordinationUncoordinatedSaveRecord[] records;
  }
}
