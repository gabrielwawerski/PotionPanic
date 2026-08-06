using NUnit.Framework;
using PotionPanic.Editor.Coordination;

namespace PotionPanic.Tests.EditMode.Coordination
{
  public sealed class CoordinationPathMatcherTests
  {
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
