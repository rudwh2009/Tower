using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Tower.Net.Clock;
using Xunit;

public class ClientPredictionTests
{
 [Fact]
 public void Predicted_Position_Replays_Pending_Inputs_And_Converges_After_Ack()
 {
 ITransport transport = new LoopbackTransport();
 var server = new NetServer(transport);
 server.SetClock(new FixedTickClock(20));
 var client = new NetClient(transport);
 server.Start(); client.Connect("p1");
 for (int i=0;i<5;i++) { server.Poll(); client.Poll(); }
 // no server snapshot yet, predict after sending input
 client.SendInput("MoveRight",1);
 var predicted = client.GetPredicted();
 predicted.x.Should().BeGreaterThanOrEqualTo(1);
 // process on server and receive SnapshotSet
 for (int i=0;i<10 && client.LastProcessedInputTick <1;i++) { server.Poll(); client.Poll(); }
 client.LastProcessedInputTick.Should().BeGreaterThanOrEqualTo(1);
 client.PendingInputCount.Should().Be(0);
 var after = client.GetPredicted();
 after.x.Should().Be(client.LastSnapshot.x);
 after.y.Should().Be(client.LastSnapshot.y);
 }
}
