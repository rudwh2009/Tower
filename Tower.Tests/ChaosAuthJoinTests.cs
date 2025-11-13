using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Xunit;

public class ChaosAuthJoinTests
{
 [Theory]
 [InlineData(0.05,0,0.0,0.2)]
 [InlineData(0.1,0,0.1,0.1)]
 public void Auth_And_Join_Succeeds_Under_Chaos(double loss, int jitterMs, double dup, double reorder)
 {
 var chaos = new LoopbackDuplex.ChaosProfile(loss, jitterMs, dup, reorder);
 var (srvT, cliT) = LoopbackDuplex.CreatePair(chaos);
 var server = new NetServer(srvT);
 var client = new NetClient(cliT);
 server.Start(); client.Connect("p1");
 for (int i=0;i<200 && client.ClientId==0;i++) { server.Poll(); client.Poll(); }
 client.ClientId.Should().BeGreaterThan(0);
 }
}
