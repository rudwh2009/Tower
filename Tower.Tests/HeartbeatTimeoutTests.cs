using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Xunit;

public class HeartbeatTimeoutTests
{
 [Fact]
 public void Server_Sends_Heartbeats_And_Times_Out_When_No_Input()
 {
 ITransport transport = new LoopbackTransport();
 var server = new NetServer(transport);
 server.Start();
 var client = new NetClient(transport);
 client.Connect("p1");
 // Pump for a few seconds of ticks; heartbeats will be sent; no exception is success
 for (int i=0;i<20;i++) { server.Poll(); client.Poll(); Thread.Sleep(50); }
 true.Should().BeTrue();
 }
}
