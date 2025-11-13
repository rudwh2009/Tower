namespace Tower.Net.Abstractions;

public readonly record struct ConnectionId(int Value)
{
 public override string ToString() => Value.ToString();
}

public interface IServerTransport
{
 void Start();
 void Stop();
 void Send(ConnectionId to, ReadOnlyMemory<byte> data);
 void Broadcast(ReadOnlyMemory<byte> data);
 void Poll(Action<ConnectionId, ReadOnlyMemory<byte>> onMessage);
 IEnumerable<ConnectionId> Connections { get; }
}
