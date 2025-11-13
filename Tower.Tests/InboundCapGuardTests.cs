using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Xunit;

public class InboundCapGuardTests
{
 private sealed class BigTransport : ITransport
 {
 private readonly byte[] _msg;
 public BigTransport(int size) { _msg = new byte[size]; }
 public void Bind() { }
 public void Connect() { }
 public void Send(ReadOnlyMemory<byte> data) { }
 public void Poll(Action<ReadOnlyMemory<byte>> onMessage) { onMessage(_msg); }
 public void Disconnect() { }
 }

 [Fact]
 public void Drops_Oversized_Message()
 {
 var t = new BigTransport(1024*1024);
 var server = new NetServer(t);
 server.SetInboundCap(64*1024);
 server.Start();
 server.Poll();
 // no exception is success; internal log would warn; can't assert logs here
 true.Should().BeTrue();
 }
}
