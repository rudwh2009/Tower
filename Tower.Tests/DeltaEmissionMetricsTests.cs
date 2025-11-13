using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Xunit;

public class DeltaEmissionMetricsTests
{
 [Fact]
 public void Emits_Delta_When_UseDeltas_Enabled_And_State_Changes()
 {
 ITransport t = new LoopbackTransport();
 var server = new NetServer(t);
 var client = new NetClient(t);
 server.SetUseDeltas(true);
 server.SetInterestRadius(1000f);
 server.DebugSpawnEntity(100,0,0);
 server.Start(); client.Connect("p1");
 for (int i=0;i<30 && client.ClientId==0;i++) { server.Poll(); client.Poll(); }
 // trigger first snapshot
 client.SendInput("MoveRight",1);
 for (int i=0;i<5;i++) { server.Poll(); client.Poll(); }
 var before = server.GetMetrics();
 before.Tx.Should().NotContainKey("SnapshotDelta");
 // change entity state and trigger second snapshot
 server.PublishEntityPosition(100,5,0);
 client.SendInput("MoveRight",2);
 for (int i=0;i<10;i++) { server.Poll(); client.Poll(); }
 var after = server.GetMetrics();
 after.Tx.Should().ContainKey("SnapshotDelta");
 }
}
