using FluentAssertions;
using MoonSharp.Interpreter;
using Tower.Core.Engine.Assets;
using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;
using Tower.Core.Scripting;
using Tower.Core.Scripting.GameApi;
using Xunit;

public class EntityQueryGatingTests
{
 private static (GameApi api, LuaRuntime lua) MakeClient()
 {
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers);
 api.SetSideGate(new Tower.Core.Engine.Prefabs.SideGate(false));
 var lua = new LuaRuntime(api);
 return (api, lua);
 }
 private static (GameApi api, LuaRuntime lua) MakeServer()
 {
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers);
 api.SetSideGate(new Tower.Core.Engine.Prefabs.SideGate(true));
 var lua = new LuaRuntime(api);
 return (api, lua);
 }

 [Fact]
 public void FindQueries_Throw_On_Client()
 {
 var (_, lua) = MakeClient();
 var a = () => lua.DoString("api:FindWithTag('enemy')");
 a.Should().Throw<ScriptRuntimeException>().Which.DecoratedMessage.Should().Contain("server-only");
 var b = () => lua.DoString("api:FindInRadius(0,0,10,'enemy')");
 b.Should().Throw<ScriptRuntimeException>().Which.DecoratedMessage.Should().Contain("server-only");
 }

 [Fact]
 public void FindQueries_Work_On_Server()
 {
 var (_, lua) = MakeServer();
 lua.DoString("api:RegisterPrefab('BaseGame/prefab/test', function(e, props) e:AddTag('enemy') end)");
 lua.DoString("api:SpawnPrefab('BaseGame/prefab/test',0,0)");
 var r1 = lua.DoString("local list = api:FindWithTag('enemy'); return type(list) == 'table'");
 r1.Type.Should().Be(DataType.Boolean); r1.Boolean.Should().BeTrue();
 var r2 = lua.DoString("local list = api:FindInRadius(0,0,100, 'enemy'); return type(list) == 'table'");
 r2.Type.Should().Be(DataType.Boolean); r2.Boolean.Should().BeTrue();
 }
}
