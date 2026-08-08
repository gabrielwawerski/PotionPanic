using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PotionPanic.Editor.Coordination;

namespace PotionPanic.Tests.EditMode.Coordination
{
  public sealed class CoordinationTransportTests
  {
    [Test]
    public void AssemblesFragmentedTextAndAcceptsTheExactByteLimit()
    {
      var assembler = new CoordinationTextMessageAssembler();
      var first = Encoding.UTF8.GetBytes("{\"type\":");
      var second = Encoding.UTF8.GetBytes("\"snapshot\"}");

      Assert.That(assembler.TryAppend(new ArraySegment<byte>(first), false, out var message, out _),
        Is.True);
      Assert.That(message, Is.Null);
      Assert.That(assembler.TryAppend(new ArraySegment<byte>(second), true, out message, out _),
        Is.True);
      Assert.That(message, Is.EqualTo("{\"type\":\"snapshot\"}"));

      var exact = new byte[CoordinationProtocol.MaximumEnvelopeBytes];
      Assert.That(assembler.TryAppend(new ArraySegment<byte>(exact), true, out message, out _), Is.True);
      Assert.That(Encoding.UTF8.GetByteCount(message), Is.EqualTo(CoordinationProtocol.MaximumEnvelopeBytes));
    }

    [Test]
    public void RejectsAFragmentedMessageOverTheCumulativeLimit()
    {
      var assembler = new CoordinationTextMessageAssembler();
      var first = new byte[CoordinationProtocol.MaximumEnvelopeBytes - 1];
      var second = new byte[2];

      Assert.That(assembler.TryAppend(new ArraySegment<byte>(first), false, out _, out _), Is.True);
      Assert.That(assembler.TryAppend(new ArraySegment<byte>(second), true, out _, out var error), Is.False);
      Assert.That(error, Does.Contain("16 KiB"));
    }

    [Test]
    public async Task EmitsOneCloseForBinaryMessages()
    {
      var socket = new FakeSocket();
      socket.EnqueueReceive(WebSocketMessageType.Binary, new byte[] { 1 }, true);
      var client = new ClientWebSocketCoordinationClient(new FakeSocketFactory(socket));
      var closes = new List<int>();
      client.Closed += (code, _) => closes.Add(code);

      await client.ConnectAsync(new Uri("ws://coordination.example.test"), "session-token",
        CancellationToken.None);

      Assert.That(closes, Is.EqualTo(new[] { 1003 }));
      Assert.That(socket.CloseCodes, Is.EqualTo(new[] { 1003 }));
    }

    [Test]
    public async Task ReceivesFragmentedTextThroughTheTransport()
    {
      var socket = new FakeSocket();
      socket.EnqueueReceive(WebSocketMessageType.Text, Encoding.UTF8.GetBytes("first-"), false);
      socket.EnqueueReceive(WebSocketMessageType.Text, Encoding.UTF8.GetBytes("second"), true);
      var client = new ClientWebSocketCoordinationClient(new FakeSocketFactory(socket));
      string received = null;
      client.MessageReceived += message => received = message;

      await client.ConnectAsync(new Uri("ws://coordination.example.test"), "session-token",
        CancellationToken.None);

      Assert.That(received, Is.EqualTo("first-second"));
      await client.CloseAsync(CancellationToken.None);
    }

    [Test]
    public async Task SerializesConcurrentSendsInCallOrder()
    {
      var socket = new FakeSocket { HoldSends = true };
      var client = new ClientWebSocketCoordinationClient(new FakeSocketFactory(socket));
      await client.ConnectAsync(new Uri("ws://coordination.example.test"), "session-token",
        CancellationToken.None);

      var first = client.SendAsync("first", CancellationToken.None);
      var second = client.SendAsync("second", CancellationToken.None);
      Assert.That(socket.Sent, Is.EqualTo(new[] { "first" }));
      socket.CompleteNextSend();
      await socket.WaitForSentCountAsync(2);
      Assert.That(socket.Sent, Is.EqualTo(new[] { "first", "second" }));
      socket.CompleteNextSend();
      await Task.WhenAll(first, second);
      await client.CloseAsync(CancellationToken.None);
    }

    private sealed class FakeSocketFactory : ICoordinationClientWebSocketFactory
    {
      private readonly FakeSocket socket;
      public FakeSocketFactory(FakeSocket socket) => this.socket = socket;
      public ICoordinationClientWebSocket Create() => socket;
    }

    private sealed class FakeSocket : ICoordinationClientWebSocket
    {
      private readonly Queue<ReceiveFrame> receiveFrames = new Queue<ReceiveFrame>();
      private readonly Queue<TaskCompletionSource<bool>> pendingSends
        = new Queue<TaskCompletionSource<bool>>();
      private readonly List<SendCountWaiter> sendCountWaiters = new List<SendCountWaiter>();
      public WebSocketState State { get; private set; } = WebSocketState.None;
      public WebSocketCloseStatus? CloseStatus { get; private set; }
      public string CloseStatusDescription { get; private set; }
      public bool HoldSends;
      public List<string> Sent { get; } = new List<string>();
      public List<int> CloseCodes { get; } = new List<int>();

      public void EnqueueReceive(WebSocketMessageType type, byte[] bytes, bool end)
      {
        receiveFrames.Enqueue(new ReceiveFrame(type, bytes, end));
      }

      public void SetRequestHeader(string name, string value) { }

      public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
      {
        State = WebSocketState.Open;
        return Task.CompletedTask;
      }

      public Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer,
        CancellationToken cancellationToken)
      {
        if (receiveFrames.Count == 0)
        {
          var completion = new TaskCompletionSource<WebSocketReceiveResult>();
          cancellationToken.Register(() => completion.TrySetCanceled());
          return completion.Task;
        }

        var frame = receiveFrames.Dequeue();
        Buffer.BlockCopy(frame.Bytes, 0, buffer.Array, buffer.Offset, frame.Bytes.Length);
        return Task.FromResult(new WebSocketReceiveResult(frame.Bytes.Length, frame.Type, frame.End));
      }

      public Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType,
        bool endOfMessage, CancellationToken cancellationToken)
      {
        if (!HoldSends)
        {
          RecordSend(buffer);
          return Task.CompletedTask;
        }

        var completion = new TaskCompletionSource<bool>();
        pendingSends.Enqueue(completion);
        RecordSend(buffer);
        return completion.Task;
      }

      public Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription,
        CancellationToken cancellationToken)
      {
        CloseStatus = closeStatus;
        CloseStatusDescription = statusDescription;
        CloseCodes.Add((int)closeStatus);
        State = WebSocketState.Closed;
        return Task.CompletedTask;
      }

      public void CompleteNextSend() => pendingSends.Dequeue().TrySetResult(true);
      public void Dispose() => State = WebSocketState.Closed;

      private void RecordSend(ArraySegment<byte> buffer)
      {
        Sent.Add(Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count));
        CompleteSendCountWaiters();
      }

      public Task WaitForSentCountAsync(int count)
      {
        if (Sent.Count >= count)
        {
          return Task.CompletedTask;
        }

        var completion = new TaskCompletionSource<bool>();
        sendCountWaiters.Add(new SendCountWaiter(count, completion));
        return completion.Task;
      }

      private void CompleteSendCountWaiters()
      {
        for (var index = sendCountWaiters.Count - 1; index >= 0; index--)
        {
          var waiter = sendCountWaiters[index];
          if (Sent.Count < waiter.Count)
          {
            continue;
          }

          sendCountWaiters.RemoveAt(index);
          waiter.Completion.TrySetResult(true);
        }
      }

      private readonly struct ReceiveFrame
      {
        public readonly WebSocketMessageType Type;
        public readonly byte[] Bytes;
        public readonly bool End;
        public ReceiveFrame(WebSocketMessageType type, byte[] bytes, bool end)
        {
          Type = type;
          Bytes = bytes;
          End = end;
        }
      }

      private readonly struct SendCountWaiter
      {
        public readonly int Count;
        public readonly TaskCompletionSource<bool> Completion;

        public SendCountWaiter(int count, TaskCompletionSource<bool> completion)
        {
          Count = count;
          Completion = completion;
        }
      }
    }
  }
}
