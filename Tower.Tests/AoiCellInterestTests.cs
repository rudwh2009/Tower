using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Tower.Net.Clock;
using Xunit;

public class AoiCellInterestTests
{
 [Fact]
 public void Cell_Filtering_Limits_Candidates_And_Still_Covers_Radius()
 {
 ITransport t = new LoopbackTransport();
 var server = new NetServer(t);
 server.SetClock(new FixedTickClock(20));
 server.SetInterestRadius(10f);
 server.SetAoiCellSize(8);
 var client = new NetClient(t);
 server.Start(); client.Connect("p1");
 for (int i=0;i<10 && client.ClientId==0;i++) { server.Poll(); client.Poll(); }
 var me = client.ClientId; me.Should().BeGreaterThan(0);
 // place3 entities: one near, one far but in neighboring cell, one far in distant cell
 server.DebugSpawnEntity(100,5,0);
 server.DebugSpawnEntity(200,20,0);
 server.DebugSpawnEntity(300,100,0);
 // trigger snapshot
 client.SendInput("MoveRight",1);
 for (int i=0;i<10;i++) { server.Poll(); client.Poll(); }
 client.LastEntities.Should().Contain(e => e.id == me);
 client.LastEntities.Should().Contain(e => e.id ==100);
 client.LastEntities.Should().NotContain(e => e.id ==300);
 }
}
