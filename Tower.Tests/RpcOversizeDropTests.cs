using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Xunit;

public class RpcOversizeDropTests
{
 [Fact]
 public void Oversize_Rpc_Is_Dropped_And_Metric_Increments()
 {
 ITransport t = new LoopbackTransport();
 var server = new NetServer(t);
 var client = new NetClient(t);
 server.SetMaxRpcBytes(32); // very small cap
 server.Start(); client.Connect("p1");
 for (int i=0;i<50 && client.ClientId==0;i++) { server.Poll(); client.Poll(); }
 // send oversize rpc
 client.SendRpc("Evt", new string('x',1024));
 for (int i=0;i<10;i++) { server.Poll(); client.Poll(); }
 var m = server.GetMetrics();
 m.DroppedOversize.Should().BeGreaterThanOrEqualTo(1);
 }
}
