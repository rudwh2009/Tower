using FluentAssertions;
using MoonSharp.Interpreter;
using Tower.Core.Engine.Assets;
using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;
using Tower.Core.Scripting;
using Tower.Core.Scripting.GameApi;
using Xunit;

public class ClientMoreGatingTests
{
 private static LuaRuntime MakeClientLua()
 {
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers);
 api.SetSideGate(new Tower.Core.Engine.Prefabs.SideGate(false));
 return new LuaRuntime(api);
 }

 [Fact]
 public void Systems_Add_Throws_On_Client()
 {
 var lua = MakeClientLua();
 var act = () => lua.DoString("api:AddSystem('tick',0, function(dt) end)");
 act.Should().Throw<ScriptRuntimeException>().Which.DecoratedMessage.Should().Contain("server-only");
 }

 [Fact]
 public void Save_OnSave_OnLoad_Throw_On_Client()
 {
 var lua = MakeClientLua();
 var a = () => lua.DoString("api:OnSave(function() return {} end)");
 a.Should().Throw<ScriptRuntimeException>().Which.DecoratedMessage.Should().Contain("server-only");
 var b = () => lua.DoString("api:OnLoad(function(state) end)");
 b.Should().Throw<ScriptRuntimeException>().Which.DecoratedMessage.Should().Contain("server-only");
 var c = () => lua.DoString("api:CollectSaveState()");
 c.Should().Throw<ScriptRuntimeException>().Which.DecoratedMessage.Should().Contain("server-only");
 var d = () => lua.DoString("api:ApplyLoadState({})");
 d.Should().Throw<ScriptRuntimeException>().Which.DecoratedMessage.Should().Contain("server-only");
 }
}
