using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PotionPanic.Editor.Coordination;

namespace PotionPanic.Tests.EditMode.Coordination
{
  public sealed class UnityCoordinationStageLifecycleTests
  {
    [Test]
    public void ExposesTheInjectableLifecycleAdapterContract()
    {
      var adapterType = Type.GetType(
        "PotionPanic.Editor.Coordination.CoordinationStageLifecycleAdapter, PotionPanic.Editor");

      Assert.That(adapterType, Is.Not.Null);
    }

    [Test]
    public void InventoriesLoadedAndAdditiveScenesAlongsideTheSelectedPrefabStage()
    {
      var source = new FakeLifecycleSource
      {
        LoadedScenes = new[]
        {
          Scene(1, "Assets/Scenes/Laboratory.unity", false),
          Scene(2, "Assets/Scenes/Arena.unity", true)
        },
        OpenPrefabStage = Prefab(1, "Assets/Prefabs/Potion.prefab", true)
      };
      var transitions = new List<CoordinationStageTransition>();
      using var adapter = new CoordinationStageLifecycleAdapter(source);
      adapter.Transitioned += transitions.Add;

      adapter.Enable();

      Assert.That(transitions.Select(TransitionSummary), Is.EquivalentTo(new[]
      {
        "Opened:Scene:Assets/Scenes/Laboratory.unity:False",
        "Opened:Scene:Assets/Scenes/Arena.unity:True",
        "Opened:Prefab:Assets/Prefabs/Potion.prefab:True"
      }));
    }

    [Test]
    public void ExcludesUntitledAndInvalidCandidatesButObservesAValidPrefabWithoutRules()
    {
      var source = new FakeLifecycleSource
      {
        LoadedScenes = new[]
        {
          Scene(1, string.Empty, false),
          Scene(2, "Packages/Scenes/Preview.unity", false)
        },
        OpenPrefabStage = Prefab(1, "Assets/Prefabs/Potion.prefab", false)
      };
      var transitions = new List<CoordinationStageTransition>();
      using var adapter = new CoordinationStageLifecycleAdapter(source);
      adapter.Transitioned += transitions.Add;

      adapter.Enable();
      source.RaisePrefabOpened(Prefab(1, "Assets/Prefabs/Potion.prefab", false));
      source.RaisePrefabOpened(Prefab(2, "Assets/Prefabs/Potion.unity", false));

      Assert.That(transitions.Select(TransitionSummary), Is.EquivalentTo(new[]
      {
        "Opened:Prefab:Assets/Prefabs/Potion.prefab:False"
      }));
    }

    [Test]
    public void SubscribesAndUnsubscribesOnlyOnceAcrossRepeatedEnableAndDisable()
    {
      var source = new FakeLifecycleSource();
      using var adapter = new CoordinationStageLifecycleAdapter(source);

      adapter.Enable();
      adapter.Enable();

      Assert.That(source.SubscriptionCount, Is.EqualTo(8));

      adapter.Disable();
      adapter.Disable();

      Assert.That(source.SubscriptionCount, Is.Zero);
    }

    [Test]
    public void MapsSceneCallbacksToDistinctStageTransitions()
    {
      var source = new FakeLifecycleSource();
      var transitions = new List<CoordinationStageTransition>();
      using var adapter = new CoordinationStageLifecycleAdapter(source);
      adapter.Transitioned += transitions.Add;
      adapter.Enable();

      source.RaiseSceneOpened(Scene(1, "Assets/Scenes/Laboratory.unity", false));
      source.RaiseSceneOpened(Scene(1, "Assets/Scenes/Laboratory.unity", false));
      source.RaiseSceneDirtied(Scene(1, "Assets/Scenes/Laboratory.unity", true));
      source.RaiseSceneDirtied(Scene(1, "Assets/Scenes/Laboratory.unity", true));
      source.RaiseSceneSaved(Scene(1, "Assets/Scenes/Laboratory.unity", false));
      source.RaiseSceneSaved(Scene(1, "Assets/Scenes/Laboratory.unity", false));
      source.RaiseSceneClosed(Scene(1, "Assets/Scenes/Laboratory.unity", false));
      source.RaiseSceneClosed(Scene(1, "Assets/Scenes/Laboratory.unity", false));

      Assert.That(transitions.Select(TransitionSummary), Is.EqualTo(new[]
      {
        "Opened:Scene:Assets/Scenes/Laboratory.unity:False",
        "Dirtied:Scene:Assets/Scenes/Laboratory.unity:True",
        "Saved:Scene:Assets/Scenes/Laboratory.unity:False",
        "Closed:Scene:Assets/Scenes/Laboratory.unity:False"
      }));
    }

    [Test]
    public void MapsSelectedPrefabCallbacksToDistinctStageTransitions()
    {
      var source = new FakeLifecycleSource();
      var transitions = new List<CoordinationStageTransition>();
      using var adapter = new CoordinationStageLifecycleAdapter(source);
      adapter.Transitioned += transitions.Add;
      adapter.Enable();

      source.RaisePrefabOpened(Prefab(1, "Assets/Prefabs/Potion.prefab", false));
      source.RaisePrefabDirtied(Prefab(1, "Assets/Prefabs/Potion.prefab", true));
      source.RaisePrefabDirtied(Prefab(1, "Assets/Prefabs/Potion.prefab", true));
      source.RaisePrefabSaved(Prefab(1, "Assets/Prefabs/Potion.prefab", false));
      source.RaisePrefabClosed(Prefab(1, "Assets/Prefabs/Potion.prefab", false));

      Assert.That(transitions.Select(TransitionSummary), Is.EqualTo(new[]
      {
        "Opened:Prefab:Assets/Prefabs/Potion.prefab:False",
        "Dirtied:Prefab:Assets/Prefabs/Potion.prefab:True",
        "Saved:Prefab:Assets/Prefabs/Potion.prefab:False",
        "Closed:Prefab:Assets/Prefabs/Potion.prefab:False"
      }));
    }

    [Test]
    public void RemovesSubscriptionsBeforeDomainReloadStyleReinstantiation()
    {
      var source = new FakeLifecycleSource();
      var firstTransitions = new List<CoordinationStageTransition>();
      var first = new CoordinationStageLifecycleAdapter(source);
      first.Transitioned += firstTransitions.Add;
      first.Enable();
      first.Dispose();

      var secondTransitions = new List<CoordinationStageTransition>();
      using var second = new CoordinationStageLifecycleAdapter(source);
      second.Transitioned += secondTransitions.Add;
      second.Enable();
      source.RaiseSceneOpened(Scene(1, "Assets/Scenes/Laboratory.unity", false));

      Assert.That(source.SubscriptionCount, Is.EqualTo(8));
      Assert.That(firstTransitions, Is.Empty);
      Assert.That(secondTransitions.Select(TransitionSummary), Is.EqualTo(new[]
      {
        "Opened:Scene:Assets/Scenes/Laboratory.unity:False"
      }));
    }

    [Test]
    public void OpensAnUntitledSceneWhenItsFirstSaveCreatesAValidAssetPath()
    {
      var source = new FakeLifecycleSource();
      var transitions = new List<CoordinationStageTransition>();
      using var adapter = new CoordinationStageLifecycleAdapter(source);
      adapter.Transitioned += transitions.Add;
      adapter.Enable();

      source.RaiseSceneOpened(Scene(7, string.Empty, false));
      source.RaiseSceneSaved(Scene(7, "Assets/Scenes/FirstSave.unity", false));

      Assert.That(transitions.Select(TransitionSummary), Is.EqualTo(new[]
      {
        "Opened:Scene:Assets/Scenes/FirstSave.unity:False"
      }));
    }

    [Test]
    public void ClosesTheOldPathAndOpensTheNewPathWhenSaveAsChangesAScenePath()
    {
      var source = new FakeLifecycleSource();
      var transitions = new List<CoordinationStageTransition>();
      using var adapter = new CoordinationStageLifecycleAdapter(source);
      adapter.Transitioned += transitions.Add;
      adapter.Enable();

      source.RaiseSceneOpened(Scene(7, "Assets/Scenes/Original.unity", true));
      source.RaiseSceneSaved(Scene(7, "Assets/Scenes/Copy.unity", false));

      Assert.That(transitions.Select(TransitionSummary), Is.EqualTo(new[]
      {
        "Opened:Scene:Assets/Scenes/Original.unity:True",
        "Closed:Scene:Assets/Scenes/Original.unity:False",
        "Opened:Scene:Assets/Scenes/Copy.unity:False"
      }));
    }

    private static CoordinationLifecycleStageCandidate Scene(
      ulong instanceId,
      string path,
      bool isDirty)
    {
      return new CoordinationLifecycleStageCandidate(
        CoordinationStageKind.Scene, instanceId, path, isDirty);
    }

    private static CoordinationLifecycleStageCandidate Prefab(
      ulong instanceId,
      string path,
      bool isDirty)
    {
      return new CoordinationLifecycleStageCandidate(
        CoordinationStageKind.Prefab, instanceId, path, isDirty);
    }

    private static string TransitionSummary(CoordinationStageTransition transition)
    {
      return $"{transition.Kind}:{transition.Stage.Kind}:{transition.Stage.Path}:"
        + transition.Stage.IsDirty;
    }

    private sealed class FakeLifecycleSource : ICoordinationStageLifecycleSource
    {
      public event Action<CoordinationLifecycleStageCandidate> SceneOpened;
      public event Action<CoordinationLifecycleStageCandidate> SceneDirtied;
      public event Action<CoordinationLifecycleStageCandidate> SceneSaved;
      public event Action<CoordinationLifecycleStageCandidate> SceneClosed;
      public event Action<CoordinationLifecycleStageCandidate> PrefabOpened;
      public event Action<CoordinationLifecycleStageCandidate> PrefabDirtied;
      public event Action<CoordinationLifecycleStageCandidate> PrefabSaved;
      public event Action<CoordinationLifecycleStageCandidate> PrefabClosed;

      public IEnumerable<CoordinationLifecycleStageCandidate> LoadedScenes { get; set; }
        = Array.Empty<CoordinationLifecycleStageCandidate>();
      public CoordinationLifecycleStageCandidate OpenPrefabStage { get; set; }

      public int SubscriptionCount => Count(SceneOpened) + Count(SceneDirtied)
        + Count(SceneSaved) + Count(SceneClosed) + Count(PrefabOpened) + Count(PrefabDirtied)
        + Count(PrefabSaved) + Count(PrefabClosed);

      public IEnumerable<CoordinationLifecycleStageCandidate> GetLoadedScenes() => LoadedScenes;
      public CoordinationLifecycleStageCandidate GetOpenPrefabStage() => OpenPrefabStage;

      public void RaiseSceneOpened(CoordinationLifecycleStageCandidate candidate)
        => SceneOpened?.Invoke(candidate);
      public void RaiseSceneDirtied(CoordinationLifecycleStageCandidate candidate)
        => SceneDirtied?.Invoke(candidate);
      public void RaiseSceneSaved(CoordinationLifecycleStageCandidate candidate)
        => SceneSaved?.Invoke(candidate);
      public void RaiseSceneClosed(CoordinationLifecycleStageCandidate candidate)
        => SceneClosed?.Invoke(candidate);
      public void RaisePrefabOpened(CoordinationLifecycleStageCandidate candidate)
        => PrefabOpened?.Invoke(candidate);
      public void RaisePrefabDirtied(CoordinationLifecycleStageCandidate candidate)
        => PrefabDirtied?.Invoke(candidate);
      public void RaisePrefabSaved(CoordinationLifecycleStageCandidate candidate)
        => PrefabSaved?.Invoke(candidate);
      public void RaisePrefabClosed(CoordinationLifecycleStageCandidate candidate)
        => PrefabClosed?.Invoke(candidate);

      private static int Count(Delegate handlers) => handlers?.GetInvocationList().Length ?? 0;
    }
  }
}
