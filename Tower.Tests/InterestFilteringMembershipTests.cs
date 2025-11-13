using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Tower.Net.Clock;
using Xunit;

public class InterestFilteringMembershipTests
{
 [Fact]
 public void Nearby_Entities_Appear_In_LastEntities_Far_Do_Not()
 {
 ITransport t = new LoopbackTransport();
 var server = new NetServer(t);
 server.SetClock(new FixedTickClock(20));
 server.SetInterestRadius(12f);
 var client = new NetClient(t);
 server.Start(); client.Connect("p1");
 for (int i=0;i<10 && client.ClientId==0;i++) { server.Poll(); client.Poll(); }
 var me = client.ClientId; me.Should().BeGreaterThan(0);
 // spawn near and far
 server.DebugSpawnEntity(100,5,0); // within radius
 server.DebugSpawnEntity(200,50,0); // far
 // trigger snapshot
 client.SendInput("MoveRight",1);
 for (int i=0;i<10;i++) { server.Poll(); client.Poll(); }
 // validate membership
 client.LastEntities.Should().Contain(e => e.id == me);
 client.LastEntities.Should().Contain(e => e.id ==100);
 client.LastEntities.Should().NotContain(e => e.id ==200);
 }
}
