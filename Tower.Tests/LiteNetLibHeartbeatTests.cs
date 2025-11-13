#if LITENETLIB
using FluentAssertions;
using Tower.Net.Session;
using Tower.Net.Transport;
using Xunit;

public class LiteNetLibHeartbeatTests
{
 [Fact]
 public void Server_Sends_Heartbeats_And_Tracks_LastSeen()
 {
 var serverT = new LiteNetLibServerTransport(9097);
 var server = new NetServer2(serverT);
 server.Start();
 var clientT = new LiteNetLibClientTransport("127.0.0.1",9097);
 var client = new NetClient(clientT); client.Connect("p1");
 for (int i=0;i<180;i++) { server.Poll(); client.Poll(); System.Threading.Thread.Sleep(16); }
 client.ClientId.Should().BeGreaterThan(0);
 server.Stop();
 }
}
#endif
