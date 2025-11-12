namespace Tower.Net.Abstractions;

public interface ITransport
{
 void Bind();
 void Connect();
 void Send(ReadOnlyMemory<byte> data);
 void Poll(Action<ReadOnlyMemory<byte>> onMessage);
 void Disconnect();
}
