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

public class ListenIntegrationTests
{
 private sealed class Consumer : IModPackConsumer
 {
 private readonly string _content;
 public bool Start;
 public Consumer(string content) { _content = content; }
 public void OnAdvertised(ModAdvert[] mods) { }
 public void OnPackComplete(string id, string sha256, byte[] bytes)
 {
 var modPath = System.IO.Path.Combine(_content, "Mods", id);
 var meta = ModMetadata.FromFile(System.IO.Path.Combine(modPath, "modinfo.json"))!;
 PackExtractor.ExtractToCache(_content, id, meta.Version, sha256, bytes);
 }
 public void OnStartLoading(StartLoading s) { Start = true; }
 }

 [Fact]
 public void Listen_Server_And_Client_Handshake_And_Loads_Ui()
 {
 var tmp = Path.Combine(Path.GetTempPath(), "tower_listen", Guid.NewGuid().ToString("N"));
 Directory.CreateDirectory(Path.Combine(tmp, "Mods", "UIMod", "Lua", "ui"));
 File.WriteAllText(Path.Combine(tmp, "Mods", "UIMod", "Lua", "ui", "hud.lua"), "GLOBAL.synced_listen = true");
 File.WriteAllText(Path.Combine(tmp, "Mods", "UIMod", "modinfo.json"), "{ \"id\": \"UIMod\", \"version\": \"1.0.0\", \"api_version\": \"1.0\", \"entry\": \"modmain.lua\", \"packs\": { \"client\": { \"ui_lua\": [\"Lua/ui/*.lua\"] } } }");
 var meta = ModMetadata.FromFile(Path.Combine(tmp, "Mods", "UIMod", "modinfo.json"))!;
 var provider = new TestProvider(meta, Path.Combine(tmp, "Mods", "UIMod"));
 var (srvT, cliT) = LoopbackDuplex.CreatePair();
 var server = new NetServer(srvT); server.SetModPackProvider(provider); server.Start();
 var consumer = new Consumer(tmp);
 var client = new NetClient(cliT, consumer); client.Connect("p1");
 for (int i=0;i<12;i++) { server.Poll(); client.Poll(); }
 consumer.Start.Should().BeTrue();
 // Client then loads UI
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers); var lua = new LuaRuntime(api);
 var bootstrap = new ModBootstrapper(assets, lua, api);
 bootstrap.LoadAll(tmp, executeScripts:true, clientMode:true);
 lua.Script.Globals.Get("GLOBAL").Table.Get("synced_listen").Boolean.Should().BeTrue();
 }

 private sealed class TestProvider : IModPackProvider
 {
 private readonly (ModMetadata meta, string root) _mod;
 private readonly ClientPackBuilder.Result _res;
 public TestProvider(ModMetadata meta, string root) { _mod = (meta, root); _res = ClientPackBuilder.Build(meta, root); }
 public ModAdvert[] GetAdvertisedMods() => new[] { new ModAdvert(_mod.meta.Id, _mod.meta.Version, _res.Sha256, _res.Size, _mod.meta.ApiVersion) };
 public byte[] GetPackBytes(string id, string sha256) => _res.Bytes;
 }
}
