using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Tower.Net.Clock;
using Xunit;

public class AoiMetricsTests
{
 [Fact]
 public void Snapshot_Aoi_Metrics_Accumulate()
 {
 ITransport t = new LoopbackTransport();
 var server = new NetServer(t);
 var client = new NetClient(t);
 server.SetClock(new FixedTickClock(20));
 server.SetInterestRadius(12f);
 server.SetAoiCellSize(8);
 // Seed a couple of entities
 server.DebugSpawnEntity(100,5,0); // near
 server.DebugSpawnEntity(200,50,0); // far
 server.Start();
 client.Connect("p1");
 for (int i =0; i <20 && client.ClientId ==0; i++) { server.Poll(); client.Poll(); }
 // Trigger a snapshot send
 client.SendInput("MoveRight",1);
 for (int i =0; i <10; i++) { server.Poll(); client.Poll(); }
 var m = server.GetMetrics();
 m.SnapshotMessages.Should().BeGreaterThan(0);
 m.SnapshotEntitiesTotal.Should().BeGreaterThan(0);
 m.SnapshotCandidatesTotal.Should().BeGreaterThan(0);
 m.SnapshotCandidatesTotal.Should().BeGreaterOrEqualTo(m.SnapshotEntitiesTotal);
 }
}
