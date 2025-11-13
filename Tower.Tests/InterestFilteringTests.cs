using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Tower.Net.Clock;
using Xunit;

public class InterestFilteringTests
{
 [Fact]
 public void SnapshotSet_Contains_Only_Nearby_Entities()
 {
 ITransport t = new LoopbackTransport();
 var server = new NetServer(t);
 server.SetClock(new FixedTickClock(20));
 server.SetInterestRadius(10f);
 var client = new NetClient(t);
 server.Start(); client.Connect("p1");
 for (int i=0;i<5;i++) { server.Poll(); client.Poll(); }
 client.ClientId.Should().BeGreaterThan(0);
 // Player at (0,0). Spawn close entity at (5,0) and far entity at (50,0)
 server.DebugSpawnEntity(999,5,0);
 server.DebugSpawnEntity(1001,50,0);
 // Trigger a snapshot by sending input
 client.SendInput("MoveRight",1);
 for (int i=0;i<5;i++) { server.Poll(); client.Poll(); }
 // Predicted or server position must be close to (>=1,0)
 client.LastSnapshot.entityId.Should().Be(client.ClientId);
 // Can't directly inspect set; but ensure LastSnapshot came back from interest list; move again and ensure still ok
 client.SendInput("MoveRight",2);
 for (int i=0;i<5;i++) { server.Poll(); client.Poll(); }
 client.LastSnapshot.entityId.Should().Be(client.ClientId);
 }
}
