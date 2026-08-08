using System;
using System.IO;
using NUnit.Framework;
using PotionPanic.Editor.Coordination;
using UnityEngine;

namespace PotionPanic.Tests.EditMode.Coordination
{
  public sealed class CoordinationPathMatcherTests
  {
    [Serializable]
    private sealed class CanonicalPathVector
    {
      public string input;
      public string normalized;
      public string canonical;
    }

    [Serializable]
    private sealed class CanonicalPathVectorDocument
    {
      public CanonicalPathVector[] vectors;
    }

    [Test]
    public void MatchesTheSharedCanonicalPathVectors()
    {
      var projectDirectory = Directory.GetParent(Application.dataPath).FullName;
      var fixturePath = Path.Combine(projectDirectory, "Tools", "CoordinationServer", "test",
        "fixtures", "canonical-path-vectors.json");
      var document = JsonUtility.FromJson<CanonicalPathVectorDocument>(
        "{\"vectors\":" + File.ReadAllText(fixturePath) + "}");

      Assert.That(document?.vectors, Is.Not.Null);
      foreach (var vector in document.vectors)
      {
        Assert.That(CoordinationPathMatcher.TryNormalize(vector.input, out var normalized), Is.True,
          vector.input);
        Assert.That(normalized, Is.EqualTo(vector.normalized), vector.input);
        Assert.That(CoordinationPathMatcher.ToCanonicalKey(normalized), Is.EqualTo(vector.canonical),
          vector.input);
      }
    }

    [TestCase("Assets/Scenes/SampleScene.unity")]
    [TestCase("Assets/Scenes/Nested/Combat.unity")]
    public void MatchesSceneRuleAtRootAndInNestedDirectories(string path)
    {
      var rule = new CoordinatedPathRule
      {
        pattern = "Assets/Scenes/**/*.unity",
        enabled = true,
        exclusive = true
      };

      Assert.That(CoordinationPathMatcher.Matches(rule, path), Is.True);
    }

    [Test]
    public void DoesNotMatchDisabledRule()
    {
      var rule = new CoordinatedPathRule
      {
        pattern = "Assets/Scenes/**/*.unity",
        enabled = false,
        exclusive = true
      };

      Assert.That(
        CoordinationPathMatcher.Matches(rule, "Assets/Scenes/SampleScene.unity"),
        Is.False);
    }

    [TestCase("../Assets/Scenes/SampleScene.unity")]
    [TestCase("C:/Assets/Scenes/SampleScene.unity")]
    [TestCase("/Assets/Scenes/SampleScene.unity")]
    public void RejectsInvalidCoordinationPaths(string path)
    {
      Assert.That(CoordinationPathMatcher.TryNormalize(path, out _), Is.False);
    }
  }
}
