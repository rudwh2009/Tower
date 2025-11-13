using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Xunit;

public class SnapshotBudgetTests
{
 [Fact]
 public void Snapshot_Dropped_When_Over_Bandwidth_Budget()
 {
 ITransport t = new LoopbackTransport();
 var server = new NetServer(t);
 var client = new NetClient(t);
 server.SetSnapshotBudget(1); // tiny budget
 server.Start(); client.Connect("p1");
 for (int i=0;i<30 && client.ClientId==0;i++) { server.Poll(); client.Poll(); }
 // send input to trigger snapshot; it should be dropped due to budget
 client.SendInput("MoveRight",1);
 for (int i=0;i<10;i++) { server.Poll(); client.Poll(); }
 var m = server.GetMetrics();
 m.DroppedSnapshotBudget.Should().BeGreaterThanOrEqualTo(1);
 }
}
