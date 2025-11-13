using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Xunit;

public class CompressedEnvelopeSnapshotSetTests
{
 [Fact]
 public void SnapshotSet_Compressed_After_Input_When_Threshold_Lowered()
 {
 ITransport t = new LoopbackTransport();
 var server = new NetServer(t);
 var client = new NetClient(t);
 // prevent baseline compression
 server.SetCompressThreshold(1_000_000);
 server.SetInterestRadius(1000f);
 for (int i=0;i<50;i++) server.DebugSpawnEntity(1000+i, i, i);
 server.Start(); client.Connect("p1");
 for (int i=0;i<40 && client.ClientId==0;i++) { server.Poll(); client.Poll(); }
 var before = server.GetMetrics();
 before.Tx.Should().NotContainKey("Compressed");
 // lower threshold to force compression for snapshot set
 server.SetCompressThreshold(16);
 client.SendInput("MoveRight",1);
 for (int i=0;i<10;i++) { server.Poll(); client.Poll(); }
 var after = server.GetMetrics();
 after.Tx.Should().ContainKey("Compressed");
 after.TxCompressedBytes.Should().BeGreaterThan(before.TxCompressedBytes);
 }
}
