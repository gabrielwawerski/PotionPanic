using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace PotionPanic.Editor.Coordination
{
  public interface ICoordinationHttpClient
  {
    Task<CoordinationHttpResponse> CreateSessionAsync(Uri uri, string developerToken);
  }

  public interface ICoordinationWebSocketClient
  {
    event Action<string> MessageReceived;
    event Action<int, string> Closed;
    Task ConnectAsync(Uri uri, string sessionToken);
    Task SendAsync(string message);
    Task CloseAsync();
  }

  public sealed class UnityWebRequestCoordinationHttpClient : ICoordinationHttpClient
  {
    public async Task<CoordinationHttpResponse> CreateSessionAsync(Uri uri, string developerToken)
    {
      using (var request = new UnityWebRequest(uri, UnityWebRequest.kHttpVerbPOST))
      {
        request.uploadHandler = new UploadHandlerRaw(Array.Empty<byte>());
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", "Bearer " + developerToken);
        await Await(request.SendWebRequest());
        return new CoordinationHttpResponse((int)request.responseCode, request.downloadHandler.text);
      }
    }

    private static Task Await(UnityWebRequestAsyncOperation operation)
    {
      var completion = new TaskCompletionSource<bool>();
      operation.completed += _ => completion.TrySetResult(true);
      return completion.Task;
    }
  }

  public sealed class ClientWebSocketCoordinationClient : ICoordinationWebSocketClient
  {
    private ClientWebSocket socket;
    private CancellationTokenSource cancellation;

    public event Action<string> MessageReceived;
    public event Action<int, string> Closed;

    public async Task ConnectAsync(Uri uri, string sessionToken)
    {
      await CloseAsync();
      socket = new ClientWebSocket();
      socket.Options.SetRequestHeader("Authorization", "Bearer " + sessionToken);
      cancellation = new CancellationTokenSource();
      await socket.ConnectAsync(uri, cancellation.Token);
      _ = ReceiveLoopAsync(socket, cancellation.Token);
    }

    public Task SendAsync(string message)
    {
      if (socket == null || socket.State != WebSocketState.Open)
      {
        throw new InvalidOperationException("The coordination socket is not connected.");
      }

      var bytes = Encoding.UTF8.GetBytes(message);
      return socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true,
        cancellation.Token);
    }

    public async Task CloseAsync()
    {
      var currentSocket = socket;
      var currentCancellation = cancellation;
      socket = null;
      cancellation = null;
      currentCancellation?.Cancel();
      if (currentSocket != null)
      {
        try
        {
          if (currentSocket.State == WebSocketState.Open)
          {
            await currentSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing",
              CancellationToken.None);
          }
        }
        finally
        {
          currentSocket.Dispose();
          currentCancellation?.Dispose();
        }
      }
    }

    private async Task ReceiveLoopAsync(ClientWebSocket currentSocket, CancellationToken token)
    {
      var buffer = new byte[CoordinationProtocol.MaximumEnvelopeBytes];
      try
      {
        while (!token.IsCancellationRequested && currentSocket.State == WebSocketState.Open)
        {
          var result = await currentSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
          if (result.MessageType == WebSocketMessageType.Close)
          {
            Closed?.Invoke((int?)currentSocket.CloseStatus ?? 1006,
              currentSocket.CloseStatusDescription ?? string.Empty);
            return;
          }

          if (result.MessageType != WebSocketMessageType.Text || !result.EndOfMessage)
          {
            Closed?.Invoke(1002, "Unsupported WebSocket message.");
            return;
          }

          MessageReceived?.Invoke(Encoding.UTF8.GetString(buffer, 0, result.Count));
        }
      }
      catch (OperationCanceledException)
      {
      }
      catch (Exception exception)
      {
        Closed?.Invoke(1006, exception.Message);
      }
    }
  }
}
