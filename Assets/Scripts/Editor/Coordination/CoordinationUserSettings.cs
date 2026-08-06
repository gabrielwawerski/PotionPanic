using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace PotionPanic.Editor.Coordination
{
  [Serializable]
  public sealed class CoordinationUserSettings
  {
    private const string RelativePath = "UserSettings/PotionPanic/coordination.local.json";
    public int schemaVersion;
    public string serverBaseUrlOverride;
    public string taskContext;
    public bool disabled;

    public static CoordinationUserSettings CreateDefault()
    {
      return new CoordinationUserSettings
      {
        schemaVersion = CoordinationConfig.CurrentSchemaVersion,
        serverBaseUrlOverride = string.Empty,
        taskContext = string.Empty,
        disabled = false
      };
    }

    public static bool TryParse(string json, out CoordinationUserSettings settings, out string error)
    {
      settings = null;
      error = null;
      if (string.IsNullOrWhiteSpace(json) || ContainsTokenField(json))
      {
        error = "Settings are empty or contain a prohibited token field.";
        return false;
      }

      try
      {
        var parsed = JsonUtility.FromJson<CoordinationUserSettings>(json);
        if (parsed == null || parsed.schemaVersion != CoordinationConfig.CurrentSchemaVersion
          || !HasJsonField(json, "serverBaseUrlOverride") || !HasJsonField(json, "taskContext")
          || !HasJsonField(json, "disabled") || parsed.serverBaseUrlOverride == null
          || parsed.taskContext == null)
        {
          error = "Settings contain missing or invalid required fields.";
          return false;
        }

        if (!string.IsNullOrWhiteSpace(parsed.serverBaseUrlOverride)
          && !CoordinationConfig.TryNormalizeServerBaseUrl(parsed.serverBaseUrlOverride,
            out var overrideUrl))
        {
          error = "Settings contain an invalid endpoint override.";
          return false;
        }

        parsed.serverBaseUrlOverride = string.IsNullOrWhiteSpace(parsed.serverBaseUrlOverride)
          ? string.Empty
          : overrideUrl;
        settings = parsed;
        return true;
      }
      catch (ArgumentException)
      {
        error = "Settings are not valid JSON.";
        return false;
      }
    }

    public static bool TryLoad(out CoordinationUserSettings settings, out string error)
    {
      var path = GetPath();
      if (!File.Exists(path))
      {
        settings = CreateDefault();
        error = null;
        return true;
      }

      return TryParse(File.ReadAllText(path), out settings, out error);
    }

    public static void Save(CoordinationUserSettings settings)
    {
      if (settings == null)
      {
        throw new ArgumentNullException(nameof(settings));
      }

      var path = GetPath();
      Directory.CreateDirectory(Path.GetDirectoryName(path));
      File.WriteAllText(path, ToJson(settings));
    }

    public static string ToJson(CoordinationUserSettings settings)
    {
      if (settings == null)
      {
        throw new ArgumentNullException(nameof(settings));
      }

      var safe = new CoordinationUserSettings
      {
        schemaVersion = CoordinationConfig.CurrentSchemaVersion,
        serverBaseUrlOverride = settings.serverBaseUrlOverride ?? string.Empty,
        taskContext = settings.taskContext ?? string.Empty,
        disabled = settings.disabled
      };
      return JsonUtility.ToJson(safe, true);
    }

    private static string GetPath()
    {
      var projectDirectory = Directory.GetParent(Application.dataPath).FullName;
      return Path.Combine(projectDirectory, RelativePath);
    }

    private static bool ContainsTokenField(string json)
    {
      return Regex.IsMatch(json, "\\\"(?:developer|session)[Tt]oken\\\"\\s*:",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }

    private static bool HasJsonField(string json, string fieldName)
    {
      return Regex.IsMatch(json, "\\\"" + Regex.Escape(fieldName) + "\\\"\\s*:",
        RegexOptions.CultureInvariant);
    }
  }
}
