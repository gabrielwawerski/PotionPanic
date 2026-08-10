using System.Linq;
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
    public void DisabledStateRoundTripsWithoutPersistingPresentationState()
    {
      var original = new CoordinationUserSettings
      {
        schemaVersion = 1,
        serverBaseUrlOverride = "https://localhost:8787",
        taskContext = "PP-9",
        disabled = true
      };

      var json = CoordinationUserSettings.ToJson(original);

      Assert.That(CoordinationUserSettings.TryParse(json, out var reloaded, out _), Is.True);
      Assert.That(reloaded.disabled, Is.True);
      Assert.That(json, Does.Not.Contain("targetSource"));
      Assert.That(json, Does.Not.Contain("expanded"));
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
    public void RejectsTaskContextLongerThan256Utf16CodeUnits()
    {
      var accepted = string.Concat(Enumerable.Repeat("\U0001F600", 128));
      var rejected = string.Concat(Enumerable.Repeat("\U0001F600", 129));
      var acceptedJson = "{\"schemaVersion\":1,\"serverBaseUrlOverride\":\"\","
        + "\"taskContext\":\"" + accepted + "\",\"disabled\":false}";
      var json = "{\"schemaVersion\":1,\"serverBaseUrlOverride\":\"\","
        + "\"taskContext\":\"" + rejected + "\",\"disabled\":false}";

      Assert.That(CoordinationUserSettings.TryParse(acceptedJson, out _, out _), Is.True);
      Assert.That(CoordinationUserSettings.TryParse(json, out _, out _), Is.False);
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

    [Test]
    public void RejectsSerializingTaskContextLongerThan256Utf16CodeUnits()
    {
      var settings = CoordinationUserSettings.CreateDefault();
      settings.taskContext = string.Concat(Enumerable.Repeat("\U0001F600", 129));

      Assert.That(() => CoordinationUserSettings.ToJson(settings),
        Throws.ArgumentException);
    }
  }
}
