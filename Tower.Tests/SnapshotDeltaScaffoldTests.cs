using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Xunit;

public class SnapshotDeltaScaffoldTests
{
 [Fact]
 public void Client_Applies_Delta_After_Baseline()
 {
 ITransport t = new LoopbackTransport();
 var server = new NetServer(t);
 var client = new NetClient(t);
 server.DebugSpawnEntity(100,7,9);
 server.Start(); client.Connect("p1");
 for (int i=0;i<50 && client.ClientId==0;i++) { server.Poll(); client.Poll(); }
 client.Entities.Should().ContainKey(100);
 }
}
