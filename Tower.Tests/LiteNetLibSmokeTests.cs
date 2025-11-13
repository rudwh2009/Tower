#if LITENETLIB
using FluentAssertions;
using Tower.Net.Session;
using Tower.Net.Transport;
using Xunit;

public class LiteNetLibSmokeTests
{
 [Fact]
 public void Client_Connects_And_Receives_Advertise()
 {
 var serverT = new LiteNetLibTransport(isServer:true, port:9095);
 var clientT = new LiteNetLibTransport(isServer:false, port:9095);
 var server = new NetServer(serverT); server.Start();
 var client = new NetClient(clientT); client.Connect("p1");
 for (int i=0;i<120;i++) { server.Poll(); client.Poll(); System.Threading.Thread.Sleep(16); }
 client.ClientId.Should().BeGreaterThan(0);
 }
}
#endif
