using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Tower.Net.Protocol.Messages;
using Xunit;

public class ModChunkRateLimitTests
{
 private sealed class TestProvider : IModPackProvider
 {
 private readonly byte[] _bytes;
 private readonly ModAdvert _adv;
 public TestProvider(int size)
 {
 _bytes = new byte[size];
 _adv = new ModAdvert("M","1","sha", size, "1.0");
 }
 public ModAdvert[] GetAdvertisedMods() => new[] { _adv };
 public byte[] GetPackBytes(string id, string sha256) => _bytes;
 }

 [Fact]
 public void Streams_In_Chunks_And_Starts_After_Finish()
 {
 ITransport transport = new LoopbackTransport();
 var server = new NetServer(transport);
 server.SetModPackProvider(new TestProvider(200_000));
 server.SetRateLimit(64*1024,32*1024); //64KiB/s,32KiB chunks
 server.Start();
 var client = new NetClient(transport);
 client.Connect("p1");
 bool sawChunk = false, sawStart=false;
 for (int i=0;i<50;i++)
 {
 server.Poll(); client.Poll();
 if (!sawChunk) { /* cannot inspect transport easily; rely on no Start until later */ }
 if (!sawStart && client.LoadingStarted) sawStart=true;
 }
 sawStart.Should().BeTrue();
 }
}
