using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Tower.Net.Clock;
using Xunit;

public class WorldQueryTests
{
 [Fact]
 public void Query_Position_And_Nearby_Entities()
 {
 ITransport t = new LoopbackTransport();
 var server = new NetServer(t);
 server.SetClock(new FixedTickClock(20));
 server.SetInterestRadius(100f);
 var client = new NetClient(t);
 server.Start(); client.Connect("p1");
 for (int i=0;i<10 && client.ClientId==0;i++) { server.Poll(); client.Poll(); }
 var me = client.ClientId; me.Should().BeGreaterThan(0);
 server.DebugSpawnEntity(100,5,0);
 server.DebugSpawnEntity(200,30,0);
 server.TryGetPosition(me, out var x, out var y).Should().BeTrue();
 var list = server.GetEntitiesNear(me,10f).ToList();
 list.Should().Contain(e => e.id == me);
 list.Should().Contain(e => e.id ==100);
 list.Should().NotContain(e => e.id ==200);
 }
}
