using System;
using System.Text;
using System.Text.RegularExpressions;

namespace PotionPanic.Editor.Coordination
{
  [Serializable]
  public sealed class CoordinatedPathRule
  {
    public string pattern;
    public bool enabled;
    public bool exclusive;
  }

  public static class CoordinationPathMatcher
  {
    public static bool Matches(CoordinatedPathRule rule, string path)
    {
      if (rule == null || !rule.enabled || !TryNormalize(path, out var normalizedPath)
        || !TryNormalizePattern(rule.pattern, out var normalizedPattern))
      {
        return false;
      }

      return Regex.IsMatch(normalizedPath, ToRegex(normalizedPattern),
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }

    public static bool TryNormalize(string path, out string normalizedPath)
    {
      return TryNormalizeInternal(path, false, out normalizedPath);
    }

    public static string ToCanonicalKey(string normalizedPath)
    {
      var builder = new StringBuilder(normalizedPath.Length);
      foreach (var character in normalizedPath)
      {
        builder.Append(character >= 'A' && character <= 'Z'
          ? (char)(character + ('a' - 'A'))
          : character);
      }

      return builder.ToString();
    }

    private static bool TryNormalizePattern(string pattern, out string normalizedPattern)
    {
      return TryNormalizeInternal(pattern, true, out normalizedPattern);
    }

    private static bool TryNormalizeInternal(string value, bool allowsGlob, out string normalized)
    {
      normalized = null;
      if (string.IsNullOrEmpty(value))
      {
        return false;
      }

      var unicodeNormalized = value.Normalize(NormalizationForm.FormC).Replace('\\', '/');
      if (unicodeNormalized[0] == '/' || HasControlCharacter(unicodeNormalized)
        || HasDrivePrefix(unicodeNormalized))
      {
        return false;
      }

      var segments = unicodeNormalized.Split('/');
      var builder = new StringBuilder(unicodeNormalized.Length);
      foreach (var segment in segments)
      {
        if (segment.Length == 0)
        {
          continue;
        }

        if (segment == "." || segment == ".." || (!allowsGlob && ContainsGlob(segment)))
        {
          return false;
        }

        if (builder.Length > 0)
        {
          builder.Append('/');
        }

        builder.Append(segment);
      }

      normalized = builder.ToString();
      return normalized.Length > 0;
    }

    private static bool HasControlCharacter(string value)
    {
      foreach (var character in value)
      {
        if (char.IsControl(character))
        {
          return true;
        }
      }

      return false;
    }

    private static bool HasDrivePrefix(string value)
    {
      return value.Length >= 2 && char.IsLetter(value[0]) && value[1] == ':';
    }

    private static bool ContainsGlob(string value)
    {
      return value.IndexOfAny(new[] { '*', '?' }) >= 0;
    }

    private static string ToRegex(string pattern)
    {
      var builder = new StringBuilder("^");
      for (var index = 0; index < pattern.Length; index += 1)
      {
        var character = pattern[index];
        if (character == '*' && index + 2 < pattern.Length && pattern[index + 1] == '*'
          && pattern[index + 2] == '/')
        {
          builder.Append("(?:.*/)?");
          index += 2;
        }
        else if (character == '*' && index + 1 < pattern.Length && pattern[index + 1] == '*')
        {
          builder.Append(".*");
          index += 1;
        }
        else if (character == '*')
        {
          builder.Append("[^/]*");
        }
        else if (character == '?')
        {
          builder.Append("[^/]");
        }
        else
        {
          builder.Append(Regex.Escape(character.ToString()));
        }
      }

      builder.Append('$');
      return builder.ToString();
    }
  }
}
