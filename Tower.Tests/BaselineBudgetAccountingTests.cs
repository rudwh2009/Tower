using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Xunit;

public class BaselineBudgetAccountingTests
{
 [Fact]
 public void Baseline_Compressed_Uses_OnWire_Budget()
 {
 ITransport t = new LoopbackTransport();
 var server = new NetServer(t);
 var client = new NetClient(t);
 server.SetSnapshotBudget(100); // small budget
 server.SetCompressThreshold(16); // ensure baseline compression
 for (int i=0;i<200;i++) server.DebugSpawnEntity(1000+i, i, i);
 server.Start(); client.Connect("p1");
 // baseline should be dropped because compressed envelope still exceeds budget
 for (int i=0;i<20;i++) { server.Poll(); client.Poll(); }
 var m = server.GetMetrics();
 m.DroppedSnapshotBudget.Should().BeGreaterThanOrEqualTo(1);
 }
}
