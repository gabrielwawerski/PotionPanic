using System;
using System.IO;
using UnityEngine;

namespace PotionPanic.Editor.Coordination
{
  [Serializable]
  public sealed class CoordinationConfig
  {
    public const int CurrentSchemaVersion = 1;
    public int schemaVersion;
    public string projectId;
    public string serverBaseUrl;
    public int heartbeatSeconds;
    public CoordinatedPathRule[] rules;

    public static bool TryParse(string json, out CoordinationConfig config, out string error)
    {
      config = null;
      error = null;
      if (string.IsNullOrWhiteSpace(json))
      {
        error = "Configuration is empty.";
        return false;
      }

      try
      {
        var parsed = JsonUtility.FromJson<CoordinationConfig>(json);
        if (parsed == null || parsed.schemaVersion != CurrentSchemaVersion
          || string.IsNullOrWhiteSpace(parsed.projectId) || parsed.heartbeatSeconds <= 0
          || parsed.rules == null || !TryNormalizeServerBaseUrl(parsed.serverBaseUrl, out var url))
        {
          error = "Configuration contains missing or invalid required fields.";
          return false;
        }

        foreach (var rule in parsed.rules)
        {
          if (rule == null || string.IsNullOrWhiteSpace(rule.pattern))
          {
            error = "Configuration contains an invalid path rule.";
            return false;
          }
        }

        parsed.serverBaseUrl = url;
        config = parsed;
        return true;
      }
      catch (ArgumentException)
      {
        error = "Configuration is not valid JSON.";
        return false;
      }
    }

    public static bool TryLoad(out CoordinationConfig config, out string error)
    {
      var projectDirectory = Directory.GetParent(Application.dataPath).FullName;
      var path = Path.Combine(projectDirectory, "coordination.json");
      if (!File.Exists(path))
      {
        config = null;
        error = "Configuration file is missing.";
        return false;
      }

      return TryParse(File.ReadAllText(path), out config, out error);
    }

    public static string GetEffectiveServerBaseUrl(
      CoordinationConfig config,
      CoordinationUserSettings settings)
    {
      if (config == null || !TryNormalizeServerBaseUrl(config.serverBaseUrl, out var configuredUrl))
      {
        throw new ArgumentException("Configuration has no valid server base URL.", nameof(config));
      }

      if (settings != null && !string.IsNullOrWhiteSpace(settings.serverBaseUrlOverride))
      {
        if (!TryNormalizeServerBaseUrl(settings.serverBaseUrlOverride, out var overrideUrl))
        {
          throw new ArgumentException("Settings have an invalid endpoint override.", nameof(settings));
        }

        return overrideUrl;
      }

      return configuredUrl;
    }

    public static string GetWebSocketBaseUrl(
      CoordinationConfig config,
      CoordinationUserSettings settings)
    {
      var baseUrl = GetEffectiveServerBaseUrl(config, settings);
      var uri = new Uri(baseUrl, UriKind.Absolute);
      var builder = new UriBuilder(uri)
      {
        Scheme = uri.Scheme == Uri.UriSchemeHttps ? "wss" : "ws",
        Port = uri.IsDefaultPort ? -1 : uri.Port
      };
      return builder.Uri.GetLeftPart(UriPartial.Authority);
    }

    public static bool TryNormalizeServerBaseUrl(string value, out string normalizedUrl)
    {
      normalizedUrl = null;
      if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
        || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)
        || uri.AbsolutePath != "/")
      {
        return false;
      }

      normalizedUrl = uri.GetLeftPart(UriPartial.Authority);
      return true;
    }
  }
}
