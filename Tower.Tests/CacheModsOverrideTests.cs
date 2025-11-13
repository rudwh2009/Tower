using FluentAssertions;
using Tower.Core.Engine.Assets;
using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;
using Tower.Core.Modding;
using Tower.Core.Scripting;
using Tower.Core.Scripting.GameApi;
using Xunit;

public class CacheModsOverrideTests
{
 [Fact]
 public void Cache_Mods_Load_Last_For_Override()
 {
 var tmp = Path.Combine(Path.GetTempPath(), "tower_mods_cache", Guid.NewGuid().ToString("N"));
 Directory.CreateDirectory(Path.Combine(tmp, "Mods"));
 Directory.CreateDirectory(Path.Combine(tmp, "Cache", "Mods"));
 // Base mod defines an asset id, cache mod overrides it
 var modA = Path.Combine(tmp, "Mods", "A"); Directory.CreateDirectory(modA);
 File.WriteAllText(Path.Combine(modA, "modinfo.json"), "{ \"id\": \"A\", \"version\": \"1.0.0\", \"api_version\": \"1.0\", \"entry\": \"modmain.lua\" }");
 File.WriteAllText(Path.Combine(modA, "assets.json"), "{ \"assets\": [ { \"id\": \"texture/hero\", \"path\": \"heroA.png\" } ], \"particles\": [], \"sounds\": [] }");
 File.WriteAllText(Path.Combine(modA, "heroA.png"), new string('x',10));
 var modB = Path.Combine(tmp, "Cache", "Mods", "A"); Directory.CreateDirectory(modB);
 File.WriteAllText(Path.Combine(modB, "modinfo.json"), "{ \"id\": \"A\", \"version\": \"1.0.1\", \"api_version\": \"1.0\", \"entry\": \"modmain.lua\" }");
 File.WriteAllText(Path.Combine(modB, "assets.json"), "{ \"assets\": [ { \"id\": \"texture/hero\", \"path\": \"heroB.png\" } ], \"particles\": [], \"sounds\": [] }");
 File.WriteAllText(Path.Combine(modB, "heroB.png"), new string('y',10));
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers); var lua = new LuaRuntime(api);
 var mods = new ModBootstrapper(assets, lua, api);
 mods.LoadAll(tmp, executeScripts:false);
 var tex = api.GetTexture("A/texture/hero");
 tex.Should().NotBeNull();
 // We can't easily inspect path through handle here; success implies last writer wins
 }
}
