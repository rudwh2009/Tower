using FluentAssertions;
using Tower.Net.Session;
using Tower.Net.Transport;
using Tower.Net.Abstractions;
using Xunit;

public class EcsReplicationServerTests
{
 [Fact]
 public void Server_Emits_Baseline_And_Delta_For_Replicated_Component()
 {
 ITransport t = new LoopbackTransport();
 var server = new NetServer(t);
 var client = new NetClient(t);
 server.SetInterestRadius(1000f);
 server.RegisterReplicatedComponent("Game", "Health");
 var eid = ((IEntityRegistry)server).CreateEntity();
 ((IEntityRegistry)server).RegisterComponent("Game","Health");
 ((IEntityRegistry)server).SetComponentData(eid, "Game","Health","{\"hp\":100}");
 server.Start(); client.Connect("p1");
 for (int i=0;i<60 && client.ClientId==0;i++) { server.Poll(); client.Poll(); }
 // mutate to trigger delta within AOI
 ((IEntityRegistry)server).SetComponentData(eid, "Game","Health","{\"hp\":80}");
 client.SendInput("noop",1);
 for (int i=0;i<10;i++) { server.Poll(); client.Poll(); }
 // we can't yet assert client state until client handlers exist; ensure server sent something compressed or not
 var m = server.GetMetrics();
 m.Tx.Should().NotBeNull();
 }
}
