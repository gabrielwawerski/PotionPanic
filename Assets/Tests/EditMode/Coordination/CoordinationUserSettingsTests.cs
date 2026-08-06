using NUnit.Framework;
using PotionPanic.Editor.Coordination;

namespace PotionPanic.Tests.EditMode.Coordination
{
  public sealed class CoordinationUserSettingsTests
  {
    [Test]
    public void DefaultsHaveNoEndpointOverrideTaskOrDisabledState()
    {
      var settings = CoordinationUserSettings.CreateDefault();

      Assert.That(settings.schemaVersion, Is.EqualTo(1));
      Assert.That(settings.serverBaseUrlOverride, Is.EqualTo(string.Empty));
      Assert.That(settings.taskContext, Is.EqualTo(string.Empty));
      Assert.That(settings.disabled, Is.False);
    }

    [Test]
    public void ParsesEndpointOverrideTaskAndDisabledState()
    {
      const string json =
        "{\"schemaVersion\":1,\"serverBaseUrlOverride\":\"https://localhost:8787/\","
        + "\"taskContext\":\"PP-7\",\"disabled\":true}";

      Assert.That(CoordinationUserSettings.TryParse(json, out var settings, out _), Is.True);
      Assert.That(settings.serverBaseUrlOverride, Is.EqualTo("https://localhost:8787"));
      Assert.That(settings.taskContext, Is.EqualTo("PP-7"));
      Assert.That(settings.disabled, Is.True);
    }

    [Test]
    public void RejectsMalformedOrTokenBearingSettings()
    {
      Assert.That(CoordinationUserSettings.TryParse("{", out _, out _), Is.False);
      Assert.That(
        CoordinationUserSettings.TryParse(
          "{\"schemaVersion\":1,\"developerToken\":\"secret\"}", out _, out _),
        Is.False);
      Assert.That(
        CoordinationUserSettings.TryParse(
          "{\"schemaVersion\":1,\"DeveloperToken\":\"secret\"}", out _, out _),
        Is.False);
    }

    [Test]
    public void PersistsOnlyTheApprovedLocalSettingsShape()
    {
      var json = CoordinationUserSettings.ToJson(new CoordinationUserSettings
      {
        schemaVersion = 1,
        serverBaseUrlOverride = "https://localhost:8787",
        taskContext = "PP-7",
        disabled = true
      });

      Assert.That(json, Does.Not.Contain("Token"));
      Assert.That(json, Does.Not.Contain("Session"));
      Assert.That(json, Does.Contain("serverBaseUrlOverride"));
    }
  }
}
