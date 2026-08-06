using NUnit.Framework;
using PotionPanic.Editor.Coordination;

namespace PotionPanic.Tests.EditMode.Coordination
{
  public sealed class CoordinationConfigTests
  {
    [Test]
    public void LoadsRequiredConfigurationAndNormalizesTheServerBaseUrl()
    {
      const string json = "{\"schemaVersion\":1,\"projectId\":\"potion-panic\","
        + "\"serverBaseUrl\":\"https://coordination.example/\",\"heartbeatSeconds\":30,"
        + "\"rules\":[{\"pattern\":\"Assets/Scenes/**/*.unity\",\"enabled\":true,"
        + "\"exclusive\":true}]}";

      Assert.That(CoordinationConfig.TryParse(json, out var config, out _), Is.True);
      Assert.That(config.serverBaseUrl, Is.EqualTo("https://coordination.example"));
      Assert.That(config.rules, Has.Length.EqualTo(1));
    }

    [TestCase("{}")]
    [TestCase("{\"schemaVersion\":2,\"projectId\":\"potion-panic\",\"serverBaseUrl\":\"https://a.example\",\"heartbeatSeconds\":30,\"rules\":[]}")]
    [TestCase("{\"schemaVersion\":1,\"projectId\":\"\",\"serverBaseUrl\":\"https://a.example\",\"heartbeatSeconds\":30,\"rules\":[]}")]
    public void RejectsMissingOrMalformedRequiredFields(string json)
    {
      Assert.That(CoordinationConfig.TryParse(json, out _, out _), Is.False);
    }

    [Test]
    public void LocalEndpointOverrideTakesPrecedenceOverCommittedConfiguration()
    {
      var config = new CoordinationConfig { serverBaseUrl = "https://prod.example" };
      var settings = new CoordinationUserSettings
      {
        serverBaseUrlOverride = "https://localhost:8787/"
      };

      Assert.That(CoordinationConfig.GetEffectiveServerBaseUrl(config, settings),
        Is.EqualTo("https://localhost:8787"));
    }

    [Test]
    public void DerivesTheWebSocketBaseUrlFromTheEffectiveHttpBaseUrl()
    {
      var config = new CoordinationConfig { serverBaseUrl = "https://coordination.example" };
      var settings = new CoordinationUserSettings
      {
        serverBaseUrlOverride = "http://localhost:8787"
      };

      Assert.That(CoordinationConfig.GetWebSocketBaseUrl(config, settings),
        Is.EqualTo("ws://localhost:8787"));
    }
  }
}
