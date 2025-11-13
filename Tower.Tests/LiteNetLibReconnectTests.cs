#if LITENETLIB
using FluentAssertions;
using Tower.Net.Session;
using Tower.Net.Transport;
using Xunit;

public class LiteNetLibReconnectTests
{
 [Fact]
 public void Client_Reconnects_After_Server_Restart()
 {
 var port =9101;
 var serverT = new LiteNetLibServerTransport(port);
 var server = new NetServer(serverT);
 server.Start();
 var clientT = new LiteNetLibClientTransport("127.0.0.1", port);
 var client = new NetClient(clientT);
 client.EnableAutoReconnect(true, initialBackoffMs:200, maxBackoffMs:1000);
 client.SetHeartbeatTimeout(300);
 client.Connect("p1");
 // wait for initial join
 for (int i=0;i<180 && client.ClientId==0;i++) { server.Poll(); client.Poll(); System.Threading.Thread.Sleep(16); }
 client.ClientId.Should().BeGreaterThan(0);
 // stop server and wait for client to detect disconnect
 server.Stop();
 for (int i=0;i<100 && !client.Disconnected;i++) { client.Poll(); System.Threading.Thread.Sleep(20); }
 client.Disconnected.Should().BeTrue();
 // restart server and allow reconnect
 server.Start();
 for (int i=0;i<360 && client.ClientId==0;i++) { server.Poll(); client.Poll(); System.Threading.Thread.Sleep(16); }
 client.ClientId.Should().BeGreaterThan(0);
 client.Disconnected.Should().BeFalse();
 server.Stop();
 }
}
#endif
