using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Tower.Net.Clock;
using Xunit;

public class InputSnapshotFlowTests
{
 [Fact]
 public void Client_Input_Produces_SnapshotSet_With_LastProcessedTick()
 {
 ITransport transport = new LoopbackTransport();
 var server = new NetServer(transport);
 server.SetClock(new FixedTickClock(20));
 var client = new NetClient(transport);
 server.Start(); client.Connect("p1");
 for (int i=0;i<5;i++) { server.Poll(); client.Poll(); }
 client.ClientId.Should().BeGreaterThan(0);
 client.SendInput("MoveRight",1);
 for (int i=0;i<5;i++) { server.Poll(); client.Poll(); }
 client.LastProcessedInputTick.Should().BeGreaterThanOrEqualTo(1);
 client.LastSnapshot.entityId.Should().Be(client.ClientId);
 client.LastSnapshot.x.Should().BeGreaterThanOrEqualTo(1);
 client.LastSnapshot.y.Should().Be(0);
 }
}
