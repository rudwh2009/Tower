using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Tower.Net.Clock;
using Xunit;

public class ClientInterpolationTests
{
 [Fact]
 public void Interpolates_Between_Snapshots()
 {
 ITransport transport = new LoopbackTransport();
 var server = new NetServer(transport);
 server.SetClock(new FixedTickClock(20));
 var client = new NetClient(transport);
 server.Start(); client.Connect("p1");
 for (int i=0;i<3;i++) { server.Poll(); client.Poll(); }
 client.SendInput("MoveRight",1);
 for (int i=0;i<3;i++) { server.Poll(); client.Poll(); }
 client.SendInput("MoveRight",2);
 for (int i=0;i<3;i++) { server.Poll(); client.Poll(); }
 // Now we have two snapshots at x=1 and x=2
 var p0 = client.GetInterpolated(0f);
 var p1 = client.GetInterpolated(0.5f);
 var p2 = client.GetInterpolated(1f);
 p0.x.Should().Be(1f);
 p1.x.Should().Be(1.5f);
 p2.x.Should().Be(2f);
 }
}
