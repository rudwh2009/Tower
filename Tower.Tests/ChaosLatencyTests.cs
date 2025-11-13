using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Xunit;

public class ChaosLatencyTests
{
 [Theory]
 [InlineData(0.0,50,0.0,0.0)]
 [InlineData(0.05,100,0.1,0.1)]
 public void Auth_Join_Survives_Latency_And_Loss(double loss, int jitterMs, double dup, double reorder)
 {
 var chaos = new LoopbackDuplex.ChaosProfile(loss, jitterMs, dup, reorder);
 var (srvT, cliT) = LoopbackDuplex.CreatePair(chaos);
 var server = new NetServer(srvT);
 var client = new NetClient(cliT);
 server.Start(); client.Connect("p1");
 for (int i=0;i<400 && client.ClientId==0;i++) { server.Poll(); client.Poll(); Thread.Sleep(1); }
 client.ClientId.Should().BeGreaterThan(0);
 }
}
