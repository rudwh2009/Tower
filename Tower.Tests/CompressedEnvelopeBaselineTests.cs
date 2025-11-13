using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Xunit;

public class CompressedEnvelopeBaselineTests
{
 [Fact]
 public void Baseline_Compressed_And_Client_Seeds_State()
 {
 ITransport t = new LoopbackTransport();
 var server = new NetServer(t);
 var client = new NetClient(t);
 server.SetCompressThreshold(16); // force compression for modest payloads
 // create enough entities to exceed threshold
 for (int i =0; i <50; i++) server.DebugSpawnEntity(1000 + i, i, i);
 server.Start();
 client.Connect("p1");
 for (int i =0; i <60 && client.ClientId ==0; i++) { server.Poll(); client.Poll(); }
 // Ensure baseline seeded entities
 client.Entities.Count.Should().BeGreaterThan(0);
 // Ensure server recorded compressed send
 var m = server.GetMetrics();
 m.Tx.Should().ContainKey("Compressed");
 m.TxCompressedBytes.Should().BeGreaterThan(0);
 }
}
