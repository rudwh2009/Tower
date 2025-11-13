#if LITENETLIB
using FluentAssertions;
using Tower.Net.Session;
using Tower.Net.Transport;
using Tower.Net.Abstractions;
using Tower.Net.Protocol.Messages;
using Xunit;

public class LiteNetLibServerClientTests
{
 [Fact]
 public void NetServer2_Accepts_Join_And_Advertises()
 {
 var serverT = new LiteNetLibServerTransport(port:9096);
 var clientT = new LiteNetLibClientTransport("127.0.0.1",9096);
 var mods = new[] { new ModAdvert("UIMod","1.0","deadbeef",10, "1.0") };
 var server = new NetServer2(serverT, mods);
 server.Start();
 var client = new NetClient(clientT);
 client.Connect("p1");
 for (int i=0;i<180;i++) { server.Poll(); client.Poll(); System.Threading.Thread.Sleep(16); }
 client.ClientId.Should().BeGreaterThan(0);
 server.Stop();
 }

 [Fact]
 public void NetServer2_Streams_Pack_And_Starts_Loading()
 {
 var serverT = new LiteNetLibServerTransport(port:9096);
 var clientT = new LiteNetLibClientTransport("127.0.0.1",9096);
 var packBytes = System.Text.Encoding.UTF8.GetBytes("PACK");
 var mods = new[] { new ModAdvert("UIMod","1.0","deadbeef", packBytes.Length, "1.0") };
 var server = new NetServer2(serverT, mods);
 server.SetModPackProvider(new TestProvider(packBytes));
 server.Start();
 var client = new NetClient(clientT);
 client.Connect("p1");
 for (int i=0;i<240;i++) { server.Poll(); client.Poll(); System.Threading.Thread.Sleep(16); if (client.LoadingStarted) break; }
 client.LoadingStarted.Should().BeTrue();
 server.Stop();
 }
 private sealed class TestProvider : IModPackProvider
 {
 private readonly byte[] _bytes; public TestProvider(byte[] b) { _bytes = b; }
 public ModAdvert[] GetAdvertisedMods() => new[] { new ModAdvert("UIMod","1.0","deadbeef", _bytes.Length, "1.0") };
 public byte[] GetPackBytes(string id, string sha256) => _bytes;
 }
}
#endif
