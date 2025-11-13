using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Xunit;

public class ModSyncHandshakeTests
{
 [Fact]
 public void Client_Receives_Advertise_Acks_Chunks_And_Starts_Loading()
 {
 ITransport transport = new LoopbackTransport();
 var server = new NetServer(transport);
 var client = new NetClient(transport);
 server.Start(); client.Connect("p1");
 for (int i=0;i<5;i++) { server.Poll(); client.Poll(); }
 client.LoadingStarted.Should().BeTrue();
 }
}
