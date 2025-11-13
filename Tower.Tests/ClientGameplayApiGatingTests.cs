using FluentAssertions;
using MoonSharp.Interpreter;
using Tower.Core.Engine.Assets;
using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;
using Tower.Core.Scripting;
using Tower.Core.Scripting.GameApi;
using Xunit;

public class ClientGameplayApiGatingTests
{
 private static (GameApi api, LuaRuntime lua) MakeClient()
 {
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers);
 api.SetSideGate(new Tower.Core.Engine.Prefabs.SideGate(false)); // client gate
 var lua = new LuaRuntime(api);
 return (api, lua);
 }
 private static (GameApi api, LuaRuntime lua) MakeServer()
 {
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers);
 api.SetSideGate(new Tower.Core.Engine.Prefabs.SideGate(true)); // server gate
 var lua = new LuaRuntime(api);
 return (api, lua);
 }

 [Fact]
 public void RegisterPrefab_Throws_On_Client()
 {
 var (_, lua) = MakeClient();
 var act = () => lua.DoString("api:RegisterPrefab('Test/ent', function(e, props) end)");
 act.Should().Throw<ScriptRuntimeException>().Which.DecoratedMessage.Should().Contain("server-only");
 }

 [Fact]
 public void SpawnPrefab_Throws_On_Client()
 {
 var (_, lua) = MakeClient();
 var act = () => lua.DoString("api:SpawnPrefab('Test/ent',0,0)");
 act.Should().Throw<ScriptRuntimeException>().Which.DecoratedMessage.Should().Contain("server-only");
 }

 [Fact]
 public void Timers_Throw_On_Client()
 {
 var (_, lua) = MakeClient();
 var a1 = () => lua.DoString("api:Interval(0.1, function() end)");
 a1.Should().Throw<ScriptRuntimeException>().Which.DecoratedMessage.Should().Contain("server-only");
 var a2 = () => lua.DoString("api:ScheduleTimer(0.1, function() end)");
 a2.Should().Throw<ScriptRuntimeException>().Which.DecoratedMessage.Should().Contain("server-only");
 var a3 = () => lua.DoString("api:Cancel('id')");
 a3.Should().Throw<ScriptRuntimeException>().Which.DecoratedMessage.Should().Contain("server-only");
 }

 [Fact]
 public void Server_Allows_Register_And_Spawn()
 {
 var (_, lua) = MakeServer();
 lua.DoString("api:RegisterPrefab('BaseGame/prefab/test', function(e, props) e:AddTag('ok') end)");
 var dv = lua.DoString("local ent = api:SpawnPrefab('BaseGame/prefab/test',1,2); return ent:GetBool('alive') or true");
 dv.Type.Should().Be(MoonSharp.Interpreter.DataType.Boolean);
 }
}
