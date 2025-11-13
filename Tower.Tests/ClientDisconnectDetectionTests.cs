using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Xunit;

public class ClientDisconnectDetectionTests
{
 [Fact]
 public void Client_Flags_Disconnected_When_No_Heartbeats()
 {
 ITransport transport = new LoopbackTransport();
 var server = new NetServer(transport);
 server.Start();
 var client = new NetClient(transport);
 client.SetHeartbeatTimeout(300); // short
 client.Connect("p1");
 for (int i=0;i<30;i++) { server.Poll(); client.Poll(); System.Threading.Thread.Sleep(20); }
 client.Disconnected.Should().BeFalse();
 // stop server heartbeats by not polling it
 for (int i=0;i<50;i++) { client.Poll(); System.Threading.Thread.Sleep(20); }
 client.Disconnected.Should().BeTrue();
 }
}
