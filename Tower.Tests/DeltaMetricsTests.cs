using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Xunit;

public class DeltaMetricsTests
{
 [Fact]
 public void Delta_Metrics_Count_Messages_Replacements_Removes()
 {
 ITransport t = new LoopbackTransport();
 var server = new NetServer(t);
 var client = new NetClient(t);
 server.SetUseDeltas(true);
 server.SetInterestRadius(1000f);
 server.DebugSpawnEntity(100,0,0);
 server.DebugSpawnEntity(200,10,0);
 server.Start(); client.Connect("p1");
 for (int i=0;i<30 && client.ClientId==0;i++) { server.Poll(); client.Poll(); }
 client.SendInput("MoveRight",1);
 for (int i=0;i<5;i++) { server.Poll(); client.Poll(); }
 // mutate and remove one
 server.PublishEntityPosition(100,5,0);
 // simulate entity200 leaving interest by moving it far
 server.PublishEntityPosition(200,10_000,0);
 client.SendInput("MoveRight",2);
 for (int i=0;i<10;i++) { server.Poll(); client.Poll(); }
 var m = server.GetMetrics();
 m.DeltaMessages.Should().BeGreaterThan(0);
 m.DeltaReplacementsTotal.Should().BeGreaterThan(0);
 m.DeltaRemovesTotal.Should().BeGreaterThan(0);
 }
}
