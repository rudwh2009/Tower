using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Tower.Core.Modding;
using Tower.Net.Protocol.Messages;
using Xunit;

public class RevisionFreshnessTests
{
 private sealed class AlwaysFreshRev : IRevisionCache
 {
 public bool IsFresh(int contentRevision, ModAdvert[] mods) => true;
 public void Save(int contentRevision, ModAdvert[] mods) { }
 }

 private sealed class TestProvider : IModPackProvider
 {
 private readonly (ModMetadata meta, string root) _mod;
 private readonly ClientPackBuilder.Result _res;
 public TestProvider(ModMetadata meta, string root) { _mod = (meta, root); _res = ClientPackBuilder.Build(meta, root); }
 public ModAdvert[] GetAdvertisedMods() => new[] { new ModAdvert(_mod.meta.Id, _mod.meta.Version, _res.Sha256, _res.Size, _mod.meta.ApiVersion) };
 public byte[] GetPackBytes(string id, string sha256) => _res.Bytes;
 }

 [Fact]
 public void Fresh_Revision_Skips_Download_And_Starts_Loading()
 {
 var tmp = Path.Combine(Path.GetTempPath(), "tower_rev", Guid.NewGuid().ToString("N"));
 Directory.CreateDirectory(Path.Combine(tmp, "Mods", "UIMod", "Lua", "ui"));
 File.WriteAllText(Path.Combine(tmp, "Mods", "UIMod", "Lua", "ui", "hud.lua"), "-- ui");
 File.WriteAllText(Path.Combine(tmp, "Mods", "UIMod", "modinfo.json"), "{ \"id\": \"UIMod\", \"version\": \"1.0.0\", \"api_version\": \"1.0\", \"entry\": \"modmain.lua\", \"packs\": { \"client\": { \"ui_lua\": [\"Lua/ui/*.lua\"] } } }");
 var meta = ModMetadata.FromFile(Path.Combine(tmp, "Mods", "UIMod", "modinfo.json"))!;
 var provider = new TestProvider(meta, Path.Combine(tmp, "Mods", "UIMod"));
 var (srvT, cliT) = LoopbackDuplex.CreatePair();
 var server = new NetServer(srvT); server.SetModPackProvider(provider); server.Start();
 var client = new NetClient(cliT, consumer: null, cache: null, revCache: new AlwaysFreshRev());
 client.Connect("p1");
 for (int i=0;i<10;i++) { server.Poll(); client.Poll(); }
 client.LoadingStarted.Should().BeTrue();
 }
}
