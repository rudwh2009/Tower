using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Tower.Core.Modding;
using Tower.Core.Engine.Assets;
using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;
using Tower.Core.Scripting;
using Tower.Core.Scripting.GameApi;
using Tower.Net.Protocol.Messages;
using Xunit;

public class ModSyncExtractAndLoadTests
{
 private sealed class TestProvider : IModPackProvider
 {
 private readonly (ModMetadata meta, string root) _mod;
 public TestProvider(ModMetadata meta, string root) { _mod = (meta, root); }
 public ModAdvert[] GetAdvertisedMods()
 {
 var pack = ClientPackBuilder.Build(_mod.meta, _mod.root);
 return new[] { new ModAdvert(_mod.meta.Id, _mod.meta.Version, pack.Sha256, pack.Size, _mod.meta.ApiVersion) };
 }
 public byte[] GetPackBytes(string id, string sha256)
 {
 var pack = ClientPackBuilder.Build(_mod.meta, _mod.root); return pack.Bytes;
 }
 }

 private sealed class TestConsumer : IModPackConsumer
 {
 private readonly string _contentRoot;
 public bool Advertised; public bool PackDone; public bool Started;
 public TestConsumer(string contentRoot) { _contentRoot = contentRoot; }
 public void OnAdvertised(ModAdvert[] mods) => Advertised = true;
 public void OnPackComplete(string id, string sha256, byte[] bytes)
 {
 PackDone = true;
 var modPath = System.IO.Path.Combine(_contentRoot, "Mods", id);
 var meta = ModMetadata.FromFile(System.IO.Path.Combine(modPath, "modinfo.json"))!;
 PackExtractor.ExtractToCache(_contentRoot, id, meta.Version, sha256, bytes);
 }
 public void OnStartLoading(StartLoading start) { Started = true; }
 }

 [Fact]
 public void Handshake_Extracts_To_Cache_And_Client_Loads_Ui()
 {
 var tmp = Path.Combine(Path.GetTempPath(), "tower_sync", Guid.NewGuid().ToString("N"));
 Directory.CreateDirectory(Path.Combine(tmp, "Mods", "UIMod", "Lua", "ui"));
 File.WriteAllText(Path.Combine(tmp, "Mods", "UIMod", "Lua", "ui", "hud.lua"), "GLOBAL.synced = true");
 File.WriteAllText(Path.Combine(tmp, "Mods", "UIMod", "modinfo.json"), "{ \"id\": \"UIMod\", \"version\": \"1.0.0\", \"api_version\": \"1.0\", \"entry\": \"modmain.lua\", \"packs\": { \"client\": { \"ui_lua\": [\"Lua/ui/*.lua\"] } } }");
 var meta = ModMetadata.FromFile(Path.Combine(tmp, "Mods", "UIMod", "modinfo.json"))!;
 ITransport transport = new LoopbackTransport();
 var server = new Tower.Net.Session.NetServer(transport); server.SetModPackProvider(new TestProvider(meta, Path.Combine(tmp, "Mods", "UIMod"))); server.Start();
 var consumer = new TestConsumer(tmp);
 var client = new NetClient(transport, consumer);
 client.Connect("p1");
 for (int i=0;i<8;i++) { server.Poll(); client.Poll(); }
 consumer.Advertised.Should().BeTrue();
 consumer.PackDone.Should().BeTrue();
 consumer.Started.Should().BeTrue();
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers); var lua = new LuaRuntime(api);
 var bootstrap = new ModBootstrapper(assets, lua, api);
 bootstrap.LoadAll(tmp, executeScripts:true, clientMode:true);
 lua.Script.Globals.Get("GLOBAL").Table.Get("synced").Boolean.Should().BeTrue();
 }
}
