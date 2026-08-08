using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PotionPanic.Editor.Coordination
{
  public enum CoordinationStageTransitionKind
  {
    Opened,
    Dirtied,
    Saved,
    Closed
  }

  public sealed class CoordinationLifecycleStageCandidate
  {
    public CoordinationStageKind Kind { get; }
    public ulong InstanceId { get; }
    public string Path { get; }
    public bool IsDirty { get; }

    public CoordinationLifecycleStageCandidate(
      CoordinationStageKind kind,
      ulong instanceId,
      string path,
      bool isDirty)
    {
      Kind = kind;
      InstanceId = instanceId;
      Path = path;
      IsDirty = isDirty;
    }
  }

  public sealed class CoordinationStageTransition
  {
    public CoordinationStageTransitionKind Kind { get; }
    public CoordinationStageInfo Stage { get; }

    public CoordinationStageTransition(
      CoordinationStageTransitionKind kind,
      CoordinationStageInfo stage)
    {
      Kind = kind;
      Stage = stage;
    }
  }

  public interface ICoordinationStageLifecycleSource
  {
    event Action<CoordinationLifecycleStageCandidate> SceneOpened;
    event Action<CoordinationLifecycleStageCandidate> SceneDirtied;
    event Action<CoordinationLifecycleStageCandidate> SceneSaved;
    event Action<CoordinationLifecycleStageCandidate> SceneClosed;
    event Action<CoordinationLifecycleStageCandidate> PrefabOpened;
    event Action<CoordinationLifecycleStageCandidate> PrefabDirtied;
    event Action<CoordinationLifecycleStageCandidate> PrefabSaved;
    event Action<CoordinationLifecycleStageCandidate> PrefabClosed;

    IEnumerable<CoordinationLifecycleStageCandidate> GetLoadedScenes();
    CoordinationLifecycleStageCandidate GetOpenPrefabStage();
  }

  public sealed class CoordinationStageLifecycleAdapter : IDisposable
  {
    private readonly ICoordinationStageLifecycleSource source;
    private readonly Dictionary<string, CoordinationStageInfo> activeStages
      = new Dictionary<string, CoordinationStageInfo>();
    private bool isDisposed;

    public event Action<CoordinationStageTransition> Transitioned;
    public bool IsEnabled { get; private set; }

    public CoordinationStageLifecycleAdapter(ICoordinationStageLifecycleSource source)
    {
      this.source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public void Enable()
    {
      ThrowIfDisposed();
      if (IsEnabled)
      {
        return;
      }

      IsEnabled = true;
      source.SceneOpened += HandleSceneOpened;
      source.SceneDirtied += HandleSceneDirtied;
      source.SceneSaved += HandleSceneSaved;
      source.SceneClosed += HandleSceneClosed;
      source.PrefabOpened += HandlePrefabOpened;
      source.PrefabDirtied += HandlePrefabDirtied;
      source.PrefabSaved += HandlePrefabSaved;
      source.PrefabClosed += HandlePrefabClosed;

      var loadedScenes = source.GetLoadedScenes()
        ?? Array.Empty<CoordinationLifecycleStageCandidate>();
      foreach (var scene in loadedScenes)
      {
        Open(scene);
      }

      Open(source.GetOpenPrefabStage());
    }

    public void Disable()
    {
      if (!IsEnabled)
      {
        return;
      }

      source.SceneOpened -= HandleSceneOpened;
      source.SceneDirtied -= HandleSceneDirtied;
      source.SceneSaved -= HandleSceneSaved;
      source.SceneClosed -= HandleSceneClosed;
      source.PrefabOpened -= HandlePrefabOpened;
      source.PrefabDirtied -= HandlePrefabDirtied;
      source.PrefabSaved -= HandlePrefabSaved;
      source.PrefabClosed -= HandlePrefabClosed;
      IsEnabled = false;
      activeStages.Clear();
    }

    public void Dispose()
    {
      Disable();
      isDisposed = true;
    }

    private void HandleSceneOpened(CoordinationLifecycleStageCandidate candidate)
      => Open(candidate);
    private void HandleSceneDirtied(CoordinationLifecycleStageCandidate candidate)
      => Dirty(candidate);
    private void HandleSceneSaved(CoordinationLifecycleStageCandidate candidate)
      => Save(candidate);
    private void HandleSceneClosed(CoordinationLifecycleStageCandidate candidate)
      => Close(candidate);
    private void HandlePrefabOpened(CoordinationLifecycleStageCandidate candidate)
      => Open(candidate);
    private void HandlePrefabDirtied(CoordinationLifecycleStageCandidate candidate)
      => Dirty(candidate);
    private void HandlePrefabSaved(CoordinationLifecycleStageCandidate candidate)
      => Save(candidate);
    private void HandlePrefabClosed(CoordinationLifecycleStageCandidate candidate)
      => Close(candidate);

    private void Open(CoordinationLifecycleStageCandidate candidate)
    {
      if (candidate == null)
      {
        return;
      }

      var key = InstanceKey(candidate);
      TryCreateStage(candidate, out var stage);
      if (!activeStages.TryGetValue(key, out var current))
      {
        activeStages.Add(key, stage);
        if (stage != null)
        {
          Emit(CoordinationStageTransitionKind.Opened, stage);
        }
        return;
      }

      ReconcilePathChange(key, current, stage);
    }

    private void Dirty(CoordinationLifecycleStageCandidate candidate)
    {
      if (candidate == null)
      {
        return;
      }

      var key = InstanceKey(candidate);
      if (!TryCreateStage(candidate, out var stage))
      {
        if (!activeStages.ContainsKey(key))
        {
          activeStages.Add(key, null);
        }
        return;
      }

      activeStages.TryGetValue(key, out var current);
      if (current == null)
      {
        var openedStage = new CoordinationStageInfo(stage.Kind, stage.Path, true);
        activeStages[key] = openedStage;
        Emit(CoordinationStageTransitionKind.Opened, openedStage);
        return;
      }

      if (!SameStagePath(current, stage))
      {
        ReconcilePathChange(key, current,
          new CoordinationStageInfo(stage.Kind, stage.Path, true));
        return;
      }

      if (current.IsDirty)
      {
        return;
      }

      var dirtiedStage = new CoordinationStageInfo(stage.Kind, stage.Path, true);
      activeStages[key] = dirtiedStage;
      Emit(CoordinationStageTransitionKind.Dirtied, dirtiedStage);
    }

    private void Save(CoordinationLifecycleStageCandidate candidate)
    {
      if (candidate == null)
      {
        return;
      }

      var key = InstanceKey(candidate);
      if (!TryCreateStage(candidate, out var stage))
      {
        if (!activeStages.ContainsKey(key))
        {
          activeStages.Add(key, null);
        }
        return;
      }

      var savedStage = new CoordinationStageInfo(stage.Kind, stage.Path, false);
      activeStages.TryGetValue(key, out var current);
      if (current == null)
      {
        activeStages[key] = savedStage;
        Emit(CoordinationStageTransitionKind.Opened, savedStage);
        return;
      }

      if (!SameStagePath(current, savedStage))
      {
        ReconcilePathChange(key, current, savedStage);
        return;
      }

      if (!current.IsDirty)
      {
        return;
      }

      activeStages[key] = savedStage;
      Emit(CoordinationStageTransitionKind.Saved, savedStage);
    }

    private void Close(CoordinationLifecycleStageCandidate candidate)
    {
      if (candidate == null)
      {
        return;
      }

      var key = InstanceKey(candidate);
      if (activeStages.TryGetValue(key, out var current))
      {
        activeStages.Remove(key);
        if (current != null)
        {
          EmitClosed(current);
        }
      }
    }

    private void ReconcilePathChange(
      string key,
      CoordinationStageInfo current,
      CoordinationStageInfo next)
    {
      if (current != null && next != null && SameStagePath(current, next))
      {
        return;
      }

      if (current != null)
      {
        EmitClosed(current);
      }

      activeStages[key] = next;
      if (next != null)
      {
        Emit(CoordinationStageTransitionKind.Opened, next);
      }
    }

    private void EmitClosed(CoordinationStageInfo stage)
    {
      Emit(CoordinationStageTransitionKind.Closed,
        new CoordinationStageInfo(stage.Kind, stage.Path, false));
    }

    private void Emit(CoordinationStageTransitionKind kind, CoordinationStageInfo stage)
    {
      Transitioned?.Invoke(new CoordinationStageTransition(kind, stage));
    }

    private static bool TryCreateStage(
      CoordinationLifecycleStageCandidate candidate,
      out CoordinationStageInfo stage)
    {
      stage = null;
      if (candidate == null)
      {
        return false;
      }

      try
      {
        stage = new CoordinationStageInfo(candidate.Kind, candidate.Path, candidate.IsDirty);
        return true;
      }
      catch (ArgumentException)
      {
        return false;
      }
    }

    private static bool SameStagePath(
      CoordinationStageInfo first,
      CoordinationStageInfo second)
    {
      return first.Kind == second.Kind
        && CoordinationPathMatcher.ToCanonicalKey(first.Path)
          == CoordinationPathMatcher.ToCanonicalKey(second.Path);
    }

    private static string InstanceKey(CoordinationLifecycleStageCandidate candidate)
      => candidate.Kind + ":" + candidate.InstanceId;

    private void ThrowIfDisposed()
    {
      if (isDisposed)
      {
        throw new ObjectDisposedException(nameof(CoordinationStageLifecycleAdapter));
      }
    }
  }

  public sealed class UnityCoordinationStageLifecycleSource : ICoordinationStageLifecycleSource
  {
    private Action<CoordinationLifecycleStageCandidate> sceneOpened;
    private Action<CoordinationLifecycleStageCandidate> sceneDirtied;
    private Action<CoordinationLifecycleStageCandidate> sceneSaved;
    private Action<CoordinationLifecycleStageCandidate> sceneClosed;
    private Action<CoordinationLifecycleStageCandidate> prefabOpened;
    private Action<CoordinationLifecycleStageCandidate> prefabDirtied;
    private Action<CoordinationLifecycleStageCandidate> prefabSaved;
    private Action<CoordinationLifecycleStageCandidate> prefabClosed;

    public event Action<CoordinationLifecycleStageCandidate> SceneOpened
    {
      add => Add(ref sceneOpened, value, SubscribeSceneOpened);
      remove => Remove(ref sceneOpened, value, UnsubscribeSceneOpened);
    }

    public event Action<CoordinationLifecycleStageCandidate> SceneDirtied
    {
      add => Add(ref sceneDirtied, value, SubscribeSceneDirtied);
      remove => Remove(ref sceneDirtied, value, UnsubscribeSceneDirtied);
    }

    public event Action<CoordinationLifecycleStageCandidate> SceneSaved
    {
      add => Add(ref sceneSaved, value, SubscribeSceneSaved);
      remove => Remove(ref sceneSaved, value, UnsubscribeSceneSaved);
    }

    public event Action<CoordinationLifecycleStageCandidate> SceneClosed
    {
      add => Add(ref sceneClosed, value, SubscribeSceneClosed);
      remove => Remove(ref sceneClosed, value, UnsubscribeSceneClosed);
    }

    public event Action<CoordinationLifecycleStageCandidate> PrefabOpened
    {
      add => Add(ref prefabOpened, value, SubscribePrefabOpened);
      remove => Remove(ref prefabOpened, value, UnsubscribePrefabOpened);
    }

    public event Action<CoordinationLifecycleStageCandidate> PrefabDirtied
    {
      add => Add(ref prefabDirtied, value, SubscribePrefabDirtied);
      remove => Remove(ref prefabDirtied, value, UnsubscribePrefabDirtied);
    }

    public event Action<CoordinationLifecycleStageCandidate> PrefabSaved
    {
      add => Add(ref prefabSaved, value, SubscribePrefabSaved);
      remove => Remove(ref prefabSaved, value, UnsubscribePrefabSaved);
    }

    public event Action<CoordinationLifecycleStageCandidate> PrefabClosed
    {
      add => Add(ref prefabClosed, value, SubscribePrefabClosed);
      remove => Remove(ref prefabClosed, value, UnsubscribePrefabClosed);
    }

    public IEnumerable<CoordinationLifecycleStageCandidate> GetLoadedScenes()
    {
      for (var index = 0; index < SceneManager.sceneCount; index++)
      {
        yield return ToSceneCandidate(SceneManager.GetSceneAt(index));
      }
    }

    public CoordinationLifecycleStageCandidate GetOpenPrefabStage()
    {
      return ToPrefabCandidate(PrefabStageUtility.GetCurrentPrefabStage());
    }

    private static void Add(
      ref Action<CoordinationLifecycleStageCandidate> handlers,
      Action<CoordinationLifecycleStageCandidate> handler,
      Action subscribe)
    {
      if (handler == null)
      {
        return;
      }

      var wasEmpty = handlers == null;
      handlers += handler;
      if (wasEmpty)
      {
        subscribe();
      }
    }

    private static void Remove(
      ref Action<CoordinationLifecycleStageCandidate> handlers,
      Action<CoordinationLifecycleStageCandidate> handler,
      Action unsubscribe)
    {
      if (handler == null)
      {
        return;
      }

      handlers -= handler;
      if (handlers == null)
      {
        unsubscribe();
      }
    }

    private void SubscribeSceneOpened() => EditorSceneManager.sceneOpened += OnSceneOpened;
    private void UnsubscribeSceneOpened() => EditorSceneManager.sceneOpened -= OnSceneOpened;
    private void SubscribeSceneDirtied() => EditorSceneManager.sceneDirtied += OnSceneDirtied;
    private void UnsubscribeSceneDirtied() => EditorSceneManager.sceneDirtied -= OnSceneDirtied;
    private void SubscribeSceneSaved() => EditorSceneManager.sceneSaved += OnSceneSaved;
    private void UnsubscribeSceneSaved() => EditorSceneManager.sceneSaved -= OnSceneSaved;
    private void SubscribeSceneClosed() => EditorSceneManager.sceneClosed += OnSceneClosed;
    private void UnsubscribeSceneClosed() => EditorSceneManager.sceneClosed -= OnSceneClosed;
    private void SubscribePrefabOpened() => PrefabStage.prefabStageOpened += OnPrefabOpened;
    private void UnsubscribePrefabOpened() => PrefabStage.prefabStageOpened -= OnPrefabOpened;
    private void SubscribePrefabDirtied() => PrefabStage.prefabStageDirtied += OnPrefabDirtied;
    private void UnsubscribePrefabDirtied() => PrefabStage.prefabStageDirtied -= OnPrefabDirtied;
    private void SubscribePrefabSaved() => PrefabStage.prefabSaved += OnPrefabSaved;
    private void UnsubscribePrefabSaved() => PrefabStage.prefabSaved -= OnPrefabSaved;
    private void SubscribePrefabClosed() => PrefabStage.prefabStageClosing += OnPrefabClosed;
    private void UnsubscribePrefabClosed() => PrefabStage.prefabStageClosing -= OnPrefabClosed;

    private void OnSceneOpened(Scene scene, OpenSceneMode mode)
      => sceneOpened?.Invoke(ToSceneCandidate(scene));
    private void OnSceneDirtied(Scene scene) => sceneDirtied?.Invoke(ToSceneCandidate(scene));
    private void OnSceneSaved(Scene scene) => sceneSaved?.Invoke(ToSceneCandidate(scene));
    private void OnSceneClosed(Scene scene) => sceneClosed?.Invoke(ToSceneCandidate(scene));
    private void OnPrefabOpened(PrefabStage stage)
      => prefabOpened?.Invoke(ToPrefabCandidate(stage));
    private void OnPrefabDirtied(PrefabStage stage)
      => prefabDirtied?.Invoke(ToPrefabCandidate(stage));
    private void OnPrefabSaved(GameObject root) => prefabSaved?.Invoke(GetOpenPrefabStage());
    private void OnPrefabClosed(PrefabStage stage)
      => prefabClosed?.Invoke(ToPrefabCandidate(stage));

    private static CoordinationLifecycleStageCandidate ToSceneCandidate(Scene scene)
    {
      return new CoordinationLifecycleStageCandidate(
        CoordinationStageKind.Scene, scene.handle.GetRawData(), scene.path, scene.isDirty);
    }

    private static CoordinationLifecycleStageCandidate ToPrefabCandidate(PrefabStage stage)
    {
      if (stage == null)
      {
        return null;
      }

      var prefab = AssetDatabase.LoadMainAssetAtPath(stage.assetPath);
      var path = prefab == null ? null : AssetDatabase.GetAssetPath(prefab);
      return new CoordinationLifecycleStageCandidate(
        CoordinationStageKind.Prefab, stage.scene.handle.GetRawData(), path, stage.scene.isDirty);
    }
  }
}
