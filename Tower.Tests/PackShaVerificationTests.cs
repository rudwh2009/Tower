using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Tower.Net.Protocol.Messages;
using Xunit;

public class PackShaVerificationTests
{
 private sealed class BadProvider : IModPackProvider
 {
 private readonly byte[] _bytes;
 private readonly ModAdvert _adv;
 public BadProvider() { _bytes = System.Text.Encoding.UTF8.GetBytes("tampered"); _adv = new ModAdvert("M","1","deadbeef", _bytes.Length, "1.0"); }
 public ModAdvert[] GetAdvertisedMods() => new[] { _adv };
 public byte[] GetPackBytes(string id, string sha256) => _bytes; // wrong hash
 }
 private sealed class CountConsumer : IModPackConsumer
 {
 public int PacksOk; public void OnAdvertised(ModAdvert[] mods) { } public void OnPackComplete(string id, string sha, byte[] bytes) { PacksOk++; } public void OnStartLoading(StartLoading s) { }
 }

 [Fact]
 public void Drops_Pack_On_Sha_Mismatch()
 {
 ITransport transport = new LoopbackTransport();
 var server = new NetServer(transport); server.SetModPackProvider(new BadProvider()); server.Start();
 var consumer = new CountConsumer();
 var client = new NetClient(transport, consumer);
 client.Connect("p1");
 for (int i=0;i<10;i++) { server.Poll(); client.Poll(); }
 consumer.PacksOk.Should().Be(0);
 }
}
