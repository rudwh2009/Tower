using FluentAssertions;
using Tower.Core.Engine.Assets;
using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;
using Tower.Core.Scripting;
using Tower.Core.Scripting.GameApi;
using Xunit;

public class ServerTimeApiTests
{
 [Fact]
 public void Time_Apis_Are_Server_Only_And_Deterministic()
 {
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers);
 api.SetSideGate(new Tower.Core.Engine.Prefabs.SideGate(true));
 var lua = new LuaRuntime(api);
 int tick =0; double sec =0; api.SetTimeProvider(() => tick, () => sec);
 var r1 = lua.DoString("return api:TimeTick() ==0 and api:TimeSeconds() ==0");
 r1.Boolean.Should().BeTrue();
 tick =10; sec =0.5;
 var r2 = lua.DoString("return api:TimeTick() ==10 and api:TimeSeconds() ==0.5");
 r2.Boolean.Should().BeTrue();
 // client-gated
 var apiC = new GameApi(assets, bus, sys, timers); apiC.SetSideGate(new Tower.Core.Engine.Prefabs.SideGate(false)); var luaC = new LuaRuntime(apiC);
 var a = () => luaC.DoString("api:TimeTick()"); a.Should().Throw<MoonSharp.Interpreter.ScriptRuntimeException>();
 var b = () => luaC.DoString("api:TimeSeconds()"); b.Should().Throw<MoonSharp.Interpreter.ScriptRuntimeException>();
 }
}
