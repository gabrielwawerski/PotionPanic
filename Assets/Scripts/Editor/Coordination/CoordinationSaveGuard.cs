using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PotionPanic.Editor.Coordination
{
  public interface ICoordinationSaveScheduler
  {
    void Post(Action action);
    void PostAfter(TimeSpan delay, Action action);
  }

  public sealed class UnityCoordinationSaveScheduler : ICoordinationSaveScheduler
  {
    public void Post(Action action)
    {
      if (action != null)
      {
        EditorApplication.delayCall += () => action();
      }
    }

    public void PostAfter(TimeSpan delay, Action action)
    {
      if (action == null)
      {
        return;
      }

      var dueAt = EditorApplication.timeSinceStartup + delay.TotalSeconds;
      void RunWhenDue()
      {
        if (EditorApplication.timeSinceStartup < dueAt)
        {
          return;
        }

        EditorApplication.update -= RunWhenDue;
        Post(action);
      }

      EditorApplication.update += RunWhenDue;
    }
  }

  public sealed class CoordinationSavePathFilter
  {
    private readonly CoordinationSaveResumeCoordinator coordinator;
    private readonly CoordinatedPathRule[] rules;
    private readonly ICoordinationSaveScheduler scheduler;

    public CoordinationSavePathFilter(
      CoordinationSaveResumeCoordinator coordinator,
      IEnumerable<CoordinatedPathRule> rules,
      ICoordinationSaveScheduler scheduler)
    {
      this.coordinator = coordinator
        ?? throw new ArgumentNullException(nameof(coordinator));
      this.rules = rules?.ToArray() ?? throw new ArgumentNullException(nameof(rules));
      this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
    }

    public string[] FilterPaths(string[] paths)
    {
      if (paths == null || paths.Length == 0)
      {
        return Array.Empty<string>();
      }

      var safePaths = new List<string>(paths.Length);
      var pathsToSchedule = new List<string>();
      foreach (var path in paths)
      {
        if (!IsExclusivelyCoordinated(path))
        {
          safePaths.Add(path);
          continue;
        }

        switch (coordinator.EvaluatePath(path))
        {
          case CoordinationSaveResumeCoordinator.SavePathDecision.Allow:
            safePaths.Add(path);
            break;
          case CoordinationSaveResumeCoordinator.SavePathDecision.BlockAndSchedule:
            pathsToSchedule.Add(path);
            break;
        }
      }

      if (pathsToSchedule.Count > 0)
      {
        var preparedSave = coordinator.PrepareSave(pathsToSchedule);
        scheduler.Post(() => coordinator.BeginPreparedSave(preparedSave));
      }

      return safePaths.ToArray();
    }

    private bool IsExclusivelyCoordinated(string path)
    {
      return rules.Any(rule => rule.exclusive
        && CoordinationPathMatcher.Matches(rule, path));
    }
  }

  public sealed class CoordinationSaveGuard : AssetModificationProcessor
  {
    private static CoordinationSavePathFilter activeFilter;

    public static void Install(CoordinationSavePathFilter filter)
    {
      activeFilter = filter ?? throw new ArgumentNullException(nameof(filter));
    }

    public static void Uninstall(CoordinationSavePathFilter filter)
    {
      if (ReferenceEquals(activeFilter, filter))
      {
        activeFilter = null;
      }
    }

    public static string[] OnWillSaveAssets(string[] paths)
    {
      return activeFilter == null ? paths : activeFilter.FilterPaths(paths);
    }
  }

  public sealed class UnityCoordinationSaveInvoker : ICoordinationSaveInvoker
  {
    public bool Save(IReadOnlyList<string> paths)
    {
      var saved = true;
      foreach (var path in paths ?? Array.Empty<string>())
      {
        saved = SavePath(path) && saved;
      }

      return saved;
    }

    private static bool SavePath(string path)
    {
      if (!CoordinationPathMatcher.TryNormalize(path, out var normalizedPath))
      {
        return false;
      }

      if (normalizedPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
      {
        return SaveScene(normalizedPath);
      }

      if (normalizedPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
      {
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && SamePath(stage.assetPath, normalizedPath))
        {
          PrefabUtility.SavePrefabAsset(stage.prefabContentsRoot, out var success);
          return success;
        }
      }

      var asset = AssetDatabase.LoadMainAssetAtPath(normalizedPath);
      if (asset == null)
      {
        return false;
      }

      AssetDatabase.SaveAssetIfDirty(asset);
      return !EditorUtility.IsDirty(asset);
    }

    private static bool SaveScene(string path)
    {
      for (var index = 0; index < SceneManager.sceneCount; index += 1)
      {
        var scene = SceneManager.GetSceneAt(index);
        if (scene.IsValid() && SamePath(scene.path, path))
        {
          return EditorSceneManager.SaveScene(scene);
        }
      }

      return false;
    }

    private static bool SamePath(string first, string second)
    {
      return CoordinationPathMatcher.TryNormalize(first, out var normalizedFirst)
        && CoordinationPathMatcher.TryNormalize(second, out var normalizedSecond)
        && CoordinationPathMatcher.ToCanonicalKey(normalizedFirst)
          == CoordinationPathMatcher.ToCanonicalKey(normalizedSecond);
    }
  }

  public sealed class UnityCoordinationSaveWarningLogger
    : ICoordinationSaveWarningLogger
  {
    public void LogWarning(string message)
    {
      Debug.LogWarning(message);
    }
  }
}
