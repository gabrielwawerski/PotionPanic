using System;
using UnityEngine;

namespace PotionPanic.Editor.Coordination
{
  [Serializable]
  internal sealed class CoordinationSessionPayload
  {
    public string developerId;
    public string displayName;
    public string sessionToken;
    public string serverTime;
    public int leaseTtlSeconds;
    public int reservationTtlSeconds;
    public long stateVersion;
  }

  public sealed class CoordinationSessionResponse
  {
    public string DeveloperId { get; }
    public string DisplayName { get; }
    public string SessionToken { get; }
    public string ServerTime { get; }
    public int LeaseTtlSeconds { get; }
    public int ReservationTtlSeconds { get; }
    public long StateVersion { get; }

    private CoordinationSessionResponse(CoordinationSessionPayload payload)
    {
      DeveloperId = payload.developerId;
      DisplayName = payload.displayName;
      SessionToken = payload.sessionToken;
      ServerTime = payload.serverTime;
      LeaseTtlSeconds = payload.leaseTtlSeconds;
      ReservationTtlSeconds = payload.reservationTtlSeconds;
      StateVersion = payload.stateVersion;
    }

    public static bool TryParse(
      string json,
      out CoordinationSessionResponse response,
      out string error)
    {
      response = null;
      error = null;
      try
      {
        var payload = JsonUtility.FromJson<CoordinationSessionPayload>(json);
        if (payload == null || string.IsNullOrWhiteSpace(payload.developerId)
          || string.IsNullOrWhiteSpace(payload.displayName)
          || string.IsNullOrWhiteSpace(payload.sessionToken)
          || string.IsNullOrWhiteSpace(payload.serverTime) || payload.leaseTtlSeconds <= 0
          || payload.reservationTtlSeconds <= 0 || payload.stateVersion < 0)
        {
          error = "The server returned an invalid session response.";
          return false;
        }

        response = new CoordinationSessionResponse(payload);
        return true;
      }
      catch (ArgumentException)
      {
        error = "The server returned malformed session JSON.";
        return false;
      }
    }
  }

  public readonly struct CoordinationHttpResponse
  {
    public int StatusCode { get; }
    public string Body { get; }

    public CoordinationHttpResponse(int statusCode, string body)
    {
      StatusCode = statusCode;
      Body = body;
    }
  }
}
