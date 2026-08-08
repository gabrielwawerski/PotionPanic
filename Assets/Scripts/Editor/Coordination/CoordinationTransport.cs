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
    Task ConnectAsync(Uri uri, string sessionToken, CancellationToken cancellationToken);
    Task SendAsync(string message, CancellationToken cancellationToken);
    Task CloseAsync(CancellationToken cancellationToken);
  }

  public interface ICoordinationClientWebSocket
  {
    WebSocketState State { get; }
    WebSocketCloseStatus? CloseStatus { get; }
    string CloseStatusDescription { get; }
    void SetRequestHeader(string name, string value);
    Task ConnectAsync(Uri uri, CancellationToken cancellationToken);
    Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer,
      CancellationToken cancellationToken);
    Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType,
      bool endOfMessage, CancellationToken cancellationToken);
    Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription,
      CancellationToken cancellationToken);
    void Dispose();
  }

  public interface ICoordinationClientWebSocketFactory
  {
    ICoordinationClientWebSocket Create();
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

  public sealed class CoordinationTextMessageAssembler
  {
    private readonly byte[] buffer = new byte[CoordinationProtocol.MaximumEnvelopeBytes];
    private int count;

    public bool TryAppend(
      ArraySegment<byte> fragment,
      bool endOfMessage,
      out string message,
      out string error)
    {
      message = null;
      error = null;
      if (fragment.Array == null || fragment.Count < 0
        || fragment.Count > CoordinationProtocol.MaximumEnvelopeBytes - count)
      {
        Reset();
        error = "The coordination message exceeds 16 KiB.";
        return false;
      }

      Buffer.BlockCopy(fragment.Array, fragment.Offset, buffer, count, fragment.Count);
      count += fragment.Count;
      if (!endOfMessage)
      {
        return true;
      }

      message = Encoding.UTF8.GetString(buffer, 0, count);
      Reset();
      return true;
    }

    public void Reset()
    {
      count = 0;
    }
  }

  public sealed class ClientWebSocketCoordinationClient : ICoordinationWebSocketClient
  {
    private const int AbnormalClosureCode = 1006;
    private const int UnsupportedDataCode = 1003;
    private const int MessageTooBigCode = 1009;

    private readonly ICoordinationClientWebSocketFactory socketFactory;
    private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);
    private readonly object lifecycleLock = new object();
    private SocketConnection connection;

    public event Action<string> MessageReceived;
    public event Action<int, string> Closed;

    public ClientWebSocketCoordinationClient(ICoordinationClientWebSocketFactory socketFactory = null)
    {
      this.socketFactory = socketFactory ?? new ClientWebSocketFactory();
    }

    public async Task ConnectAsync(Uri uri, string sessionToken, CancellationToken cancellationToken)
    {
      if (uri == null)
      {
        throw new ArgumentNullException(nameof(uri));
      }

      if (string.IsNullOrWhiteSpace(sessionToken))
      {
        throw new ArgumentException("A coordination session token is required.", nameof(sessionToken));
      }

      await CloseAsync(CancellationToken.None);
      var current = new SocketConnection(socketFactory.Create(), cancellationToken);
      current.Socket.SetRequestHeader("Authorization", "Bearer " + sessionToken);
      lock (lifecycleLock)
      {
        connection = current;
      }

      try
      {
        await current.Socket.ConnectAsync(uri, current.Cancellation.Token);
        if (IsCurrent(current))
        {
          _ = ReceiveLoopAsync(current);
        }
      }
      catch
      {
        var detached = Detach(current, notify: false);
        detached?.Dispose();
        throw;
      }
    }

    public async Task SendAsync(string message, CancellationToken cancellationToken)
    {
      if (message == null)
      {
        throw new ArgumentNullException(nameof(message));
      }

      var bytes = Encoding.UTF8.GetBytes(message);
      if (bytes.Length > CoordinationProtocol.MaximumEnvelopeBytes)
      {
        throw new ArgumentException("The coordination message exceeds 16 KiB.", nameof(message));
      }

      await sendLock.WaitAsync(cancellationToken);
      try
      {
        var current = CurrentConnection();
        if (current == null || current.Socket.State != WebSocketState.Open)
        {
          throw new InvalidOperationException("The coordination socket is not connected.");
        }

        using (var sendCancellation = CancellationTokenSource.CreateLinkedTokenSource(
          cancellationToken, current.Cancellation.Token))
        {
          await current.Socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text,
            true, sendCancellation.Token);
        }
      }
      finally
      {
        sendLock.Release();
      }
    }

    public async Task CloseAsync(CancellationToken cancellationToken)
    {
      var current = Detach(CurrentConnection(), notify: false);
      if (current == null)
      {
        return;
      }

      try
      {
        if (current.Socket.State == WebSocketState.Open
          || current.Socket.State == WebSocketState.CloseReceived)
        {
          await current.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing",
            cancellationToken);
        }
      }
      finally
      {
        current.Dispose();
      }
    }

    private async Task ReceiveLoopAsync(SocketConnection current)
    {
      var message = new CoordinationTextMessageAssembler();
      var buffer = new byte[4096];
      try
      {
        while (!current.Cancellation.IsCancellationRequested && IsCurrent(current)
          && current.Socket.State == WebSocketState.Open)
        {
          var result = await current.Socket.ReceiveAsync(new ArraySegment<byte>(buffer),
            current.Cancellation.Token);
          if (result.MessageType == WebSocketMessageType.Close)
          {
            NotifyClosed(current, (int?)current.Socket.CloseStatus ?? AbnormalClosureCode,
              current.Socket.CloseStatusDescription ?? string.Empty);
            return;
          }

          if (result.MessageType != WebSocketMessageType.Text)
          {
            await CloseForProtocolViolationAsync(current, UnsupportedDataCode,
              "Only text coordination messages are supported.");
            return;
          }

          if (!message.TryAppend(new ArraySegment<byte>(buffer, 0, result.Count), result.EndOfMessage,
            out var assembled, out var error))
          {
            await CloseForProtocolViolationAsync(current, MessageTooBigCode, error);
            return;
          }

          if (assembled != null && IsCurrent(current))
          {
            MessageReceived?.Invoke(assembled);
          }
        }
      }
      catch (OperationCanceledException) when (current.Cancellation.IsCancellationRequested
        || !IsCurrent(current))
      {
      }
      catch (Exception exception)
      {
        NotifyClosed(current, AbnormalClosureCode, exception.Message);
      }
    }

    private async Task CloseForProtocolViolationAsync(
      SocketConnection current,
      int closeCode,
      string reason)
    {
      try
      {
        await current.Socket.CloseAsync((WebSocketCloseStatus)closeCode, reason,
          CancellationToken.None);
      }
      catch
      {
      }

      NotifyClosed(current, closeCode, reason);
    }

    private void NotifyClosed(SocketConnection current, int closeCode, string reason)
    {
      if (Interlocked.Exchange(ref current.closedNotification, 1) != 0)
      {
        return;
      }

      if (Detach(current, notify: true) == null)
      {
        return;
      }

      Closed?.Invoke(closeCode, reason ?? string.Empty);
    }

    private SocketConnection CurrentConnection()
    {
      lock (lifecycleLock)
      {
        return connection;
      }
    }

    private bool IsCurrent(SocketConnection current)
    {
      lock (lifecycleLock)
      {
        return ReferenceEquals(connection, current);
      }
    }

    private SocketConnection Detach(SocketConnection current, bool notify)
    {
      if (current == null)
      {
        return null;
      }

      lock (lifecycleLock)
      {
        if (!ReferenceEquals(connection, current))
        {
          return null;
        }

        connection = null;
      }

      current.Cancellation.Cancel();
      if (!notify)
      {
        return current;
      }

      current.Dispose();
      return current;
    }

    private sealed class SocketConnection : IDisposable
    {
      public readonly ICoordinationClientWebSocket Socket;
      public readonly CancellationTokenSource Cancellation;
      public int closedNotification;

      public SocketConnection(ICoordinationClientWebSocket socket, CancellationToken cancellationToken)
      {
        Socket = socket ?? throw new InvalidOperationException("The WebSocket factory returned no socket.");
        Cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
      }

      public void Dispose()
      {
        Socket.Dispose();
        Cancellation.Dispose();
      }
    }

    private sealed class ClientWebSocketFactory : ICoordinationClientWebSocketFactory
    {
      public ICoordinationClientWebSocket Create()
      {
        return new ClientWebSocketAdapter(new ClientWebSocket());
      }
    }

    private sealed class ClientWebSocketAdapter : ICoordinationClientWebSocket
    {
      private readonly ClientWebSocket socket;

      public ClientWebSocketAdapter(ClientWebSocket socket)
      {
        this.socket = socket;
      }

      public WebSocketState State => socket.State;
      public WebSocketCloseStatus? CloseStatus => socket.CloseStatus;
      public string CloseStatusDescription => socket.CloseStatusDescription;
      public void SetRequestHeader(string name, string value) => socket.Options.SetRequestHeader(name, value);
      public Task ConnectAsync(Uri uri, CancellationToken cancellationToken) => socket.ConnectAsync(uri, cancellationToken);
      public Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer,
        CancellationToken cancellationToken) => socket.ReceiveAsync(buffer, cancellationToken);
      public Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType,
        bool endOfMessage, CancellationToken cancellationToken) => socket.SendAsync(buffer, messageType,
          endOfMessage, cancellationToken);
      public Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription,
        CancellationToken cancellationToken) => socket.CloseAsync(closeStatus, statusDescription,
          cancellationToken);
      public void Dispose() => socket.Dispose();
    }
  }
}
