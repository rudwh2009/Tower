using FluentAssertions;
using Tower.Core.Engine.Assets;
using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;
using Tower.Core.Modding;
using Tower.Core.Scripting;
using Tower.Core.Scripting.GameApi;
using Xunit;

public class PackSplitTests
{
 private static string MakeModWithPacks(string root)
 {
 var dir = Path.Combine(root, "Mods", "PackSplit");
 Directory.CreateDirectory(dir);
 Directory.CreateDirectory(Path.Combine(dir, "Lua", "systems"));
 Directory.CreateDirectory(Path.Combine(dir, "Lua", "ui"));
 File.WriteAllText(Path.Combine(dir, "Lua", "systems", "a.lua"), "GLOBAL.server_ran = true");
 File.WriteAllText(Path.Combine(dir, "Lua", "ui", "b.lua"), "GLOBAL.client_ran = true");
 var modinfo = "{\n"+
 " \"id\": \"PackSplit\",\n"+
 " \"version\": \"1.0.0\",\n"+
 " \"api_version\": \"1.0\",\n"+
 " \"entry\": \"modmain.lua\",\n"+
 " \"packs\": {\n"+
 " \"server\": { \"lua\": [\"Lua/systems/*.lua\"] },\n"+
 " \"client\": { \"ui_lua\": [\"Lua/ui/*.lua\"] }\n"+
 " }\n"+
 "}\n";
 File.WriteAllText(Path.Combine(dir, "modinfo.json"), modinfo);
 File.WriteAllText(Path.Combine(dir, "assets.json"), "{ \"assets\": [], \"particles\": [], \"sounds\": [] }");
 File.WriteAllText(Path.Combine(dir, "modmain.lua"), "GLOBAL.entry_ran = true");
 return dir;
 }

 [Fact]
 public void Server_Loads_Server_Lua_Only()
 {
 var tmp = Path.Combine(Path.GetTempPath(), "tower_mods_packs", Guid.NewGuid().ToString("N"));
 Directory.CreateDirectory(Path.Combine(tmp, "Mods"));
 MakeModWithPacks(tmp);
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers); var lua = new LuaRuntime(api);
 var mods = new ModBootstrapper(assets, lua, api);
 mods.LoadAll(tmp, executeScripts: true, clientMode: false);
 var g = lua.Script.Globals.Get("GLOBAL").Table;
 g.Get("server_ran").Boolean.Should().BeTrue();
 g.Get("client_ran").Type.Should().Be(MoonSharp.Interpreter.DataType.Nil);
 // entry should be ignored in presence of server pack
 g.Get("entry_ran").Type.Should().Be(MoonSharp.Interpreter.DataType.Nil);
 }

 [Fact]
 public void Client_Loads_Client_Ui_Lua_Only()
 {
 var tmp = Path.Combine(Path.GetTempPath(), "tower_mods_packs", Guid.NewGuid().ToString("N"));
 Directory.CreateDirectory(Path.Combine(tmp, "Mods"));
 MakeModWithPacks(tmp);
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers); var lua = new LuaRuntime(api);
 var mods = new ModBootstrapper(assets, lua, api);
 mods.LoadAll(tmp, executeScripts: true, clientMode: true);
 var g = lua.Script.Globals.Get("GLOBAL").Table;
 g.Get("client_ran").Boolean.Should().BeTrue();
 g.Get("server_ran").Type.Should().Be(MoonSharp.Interpreter.DataType.Nil);
 g.Get("entry_ran").Type.Should().Be(MoonSharp.Interpreter.DataType.Nil);
 }
}
