using FluentAssertions;
using Tower.Core.Engine.Assets;
using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;
using Tower.Core.Modding;
using Tower.Core.Scripting;
using Tower.Core.Scripting.GameApi;
using Xunit;

public class ModApiVersionTests
{
 private static string MakeMod(string root, string id, string apiVersion, string entryLua)
 {
 var dir = Path.Combine(root, "Mods", id);
 Directory.CreateDirectory(dir);
 var json = $"{{\n \"id\": \"{id}\",\n \"version\": \"1.0.0\",\n \"api_version\": \"{apiVersion}\",\n \"entry\": \"modmain.lua\"\n}}\n";
 File.WriteAllText(Path.Combine(dir, "modinfo.json"), json);
 File.WriteAllText(Path.Combine(dir, "assets.json"), "{ \"assets\": [], \"particles\": [], \"sounds\": [] }");
 File.WriteAllText(Path.Combine(dir, "modmain.lua"), entryLua);
 return dir;
 }

 [Fact]
 public void Mod_With_Higher_Major_Is_Skipped()
 {
 var tmp = Path.Combine(Path.GetTempPath(), "tower_mods_api", Guid.NewGuid().ToString("N"));
 Directory.CreateDirectory(Path.Combine(tmp, "Mods"));
 // Mod that would set flag if loaded
 MakeMod(tmp, "SkipMe", "2.0", "GLOBAL.loaded_skip = true");
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers); var lua = new LuaRuntime(api);
 var mods = new ModBootstrapper(assets, lua, api);
 mods.LoadAll(tmp);
 var g = lua.Script.Globals.Get("GLOBAL").Table;
 g.Get("loaded_skip").Type.Should().Be(MoonSharp.Interpreter.DataType.Nil);
 }

 [Fact]
 public void Mod_With_Higher_Minor_Loads_With_Warning()
 {
 var tmp = Path.Combine(Path.GetTempPath(), "tower_mods_api", Guid.NewGuid().ToString("N"));
 Directory.CreateDirectory(Path.Combine(tmp, "Mods"));
 MakeMod(tmp, "LoadMinor", "1.5", "GLOBAL.loaded_minor = true");
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers); var lua = new LuaRuntime(api);
 var mods = new ModBootstrapper(assets, lua, api);
 mods.LoadAll(tmp);
 var g = lua.Script.Globals.Get("GLOBAL").Table;
 g.Get("loaded_minor").Boolean.Should().BeTrue();
 }

 [Fact]
 public void Api_Version_Available_To_Lua()
 {
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers); var lua = new LuaRuntime(api);
 var dv = lua.DoString("return api:GetApiVersion()");
 dv.String.Should().NotBeNullOrEmpty();
 }
}
