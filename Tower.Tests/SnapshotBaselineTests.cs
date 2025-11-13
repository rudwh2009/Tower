using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Xunit;

public class SnapshotBaselineTests
{
 [Fact]
 public void Client_Receives_Baseline_After_StartLoading()
 {
 ITransport t = new LoopbackTransport();
 var server = new NetServer(t);
 var client = new NetClient(t);
 server.DebugSpawnEntity(100,1,2);
 server.DebugSpawnEntity(200, -3,4);
 server.Start(); client.Connect("p1");
 for (int i=0;i<50 && client.ClientId==0;i++) { server.Poll(); client.Poll(); }
 client.Entities.Should().ContainKey(100);
 client.Entities.Should().ContainKey(200);
 }
}
