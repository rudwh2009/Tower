using FluentAssertions;
using Tower.Core.Engine.Assets;
using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;
using Tower.Core.Scripting;
using Tower.Core.Scripting.GameApi;
using Xunit;

public class SaveHooksTests
{
 [Fact]
 public void Register_Save_And_Load_Hooks_ServerOnly()
 {
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers); var lua = new LuaRuntime(api);
 // server mode by default; set current mod to simulate bootstrapper
 api.SetCurrentMod("TestMod");
 lua.DoString("api:OnSave(function() return { a =1 } end)");
 lua.DoString("api:OnLoad(function(_) end)");
 api.ListModsWithSaveHooks().Should().Contain("TestMod");
 api.ListModsWithLoadHooks().Should().Contain("TestMod");
 }

 [Fact]
 public void Client_Mode_Guard_Fails()
 {
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers); var lua = new LuaRuntime(api);
 api.SetSideGate(new Tower.Core.Engine.Prefabs.SideGate(false)); // client
 api.SetCurrentMod("TestMod");
 var act = () => lua.DoString("api:OnSave(function() end)");
 act.Should().Throw<MoonSharp.Interpreter.ScriptRuntimeException>();
 }
}
