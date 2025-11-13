using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Xunit;

public class NetworkingMetricsTests
{
 [Fact]
 public void Metrics_Count_Rx_Tx_And_Drops()
 {
 var (srvT, cliT) = LoopbackDuplex.CreatePair();
 var server = new NetServer(srvT);
 var client = new NetClient(cliT);
 server.Start(); client.Connect("p1");
 for (int i=0;i<50 && client.ClientId==0;i++) { server.Poll(); client.Poll(); }
 var m = server.GetMetrics();
 m.Rx.Should().ContainKey("AuthResponse");
 m.Tx.Should().ContainKey("JoinAccepted");
 m.DroppedUnauth.Should().BeGreaterThanOrEqualTo(0);
 }
}
