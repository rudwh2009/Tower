using FluentAssertions;
using Tower.Core.Engine.Assets;
using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;
using Tower.Core.Scripting;
using Tower.Core.Scripting.GameApi;
using Xunit;

public class DeterministicRandomTests
{
 private static LuaRuntime MakeServerLua()
 {
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers);
 api.SetSideGate(new Tower.Core.Engine.Prefabs.SideGate(true));
 var lua = new LuaRuntime(api);
 return lua;
 }

 [Fact]
 public void Random_Is_Deterministic_With_Seed()
 {
 var lua = MakeServerLua();
 var code = @"
 api:SetRandomSeed(42)
 local a = api:Random()
 local b = api:Random()
 api:SetRandomSeed(42)
 local a2 = api:Random()
 local b2 = api:Random()
 return math.abs(a-a2) <1e-9 and math.abs(b-b2) <1e-9
 ";
 var dv = lua.DoString(code);
 dv.Boolean.Should().BeTrue();
 }

 [Fact]
 public void Random_Throws_On_Client()
 {
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers); api.SetSideGate(new Tower.Core.Engine.Prefabs.SideGate(false));
 var lua = new LuaRuntime(api);
 var act = () => lua.DoString("api:Random()");
 act.Should().Throw<MoonSharp.Interpreter.ScriptRuntimeException>().Which.DecoratedMessage.Should().Contain("server-only");
 }
}
