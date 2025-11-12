using System.Collections.Concurrent;
using Tower.Net.Abstractions;

namespace Tower.Net.Transport;

public sealed class LoopbackTransport : ITransport
{
 private readonly ConcurrentQueue<byte[]> _queue = new();
 public void Bind() { }
 public void Connect() { }
 public void Send(ReadOnlyMemory<byte> data) => _queue.Enqueue(data.ToArray());
 public void Poll(Action<ReadOnlyMemory<byte>> onMessage)
 {
 while (_queue.TryDequeue(out var msg)) onMessage(msg);
 }
 public void Disconnect() { }
}
