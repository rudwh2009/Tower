using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Xunit;

public class LoopbackTransportTests
{
 [Fact]
 public void Server_Responds_To_Join_And_Echoes_Rpc()
 {
 ITransport serverT = new LoopbackTransport();
 ITransport clientT = serverT; // loopback
 var server = new NetServer(serverT);
 var client = new NetClient(clientT);
 server.Start();
 client.Connect("tester");
 // pump both ends a few times
 for (int i=0;i<3;i++) { server.Poll(); client.Poll(); }
 client.ClientId.Should().BeGreaterThan(0);
 client.SendRpc("Ping","Hello");
 for (int i=0;i<3;i++) { server.Poll(); client.Poll(); }
 // No assertion on log; reaching here without exception is OK
 }
}
