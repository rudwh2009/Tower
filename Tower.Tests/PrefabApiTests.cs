using FluentAssertions;
using MoonSharp.Interpreter;
using Tower.Core.Engine.Assets;
using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;
using Tower.Core.Modding;
using Tower.Core.Scripting;
using Tower.Core.Scripting.GameApi;
using Tower.Core.Engine.Prefabs;
using Xunit;

public class PrefabApiTests
{
 private (GameApi api, LuaRuntime lua) Create(bool server=true)
 {
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers);
 api.SetSideGate(new SideGate(server));
 var lua = new LuaRuntime(api);
 return (api, lua);
 }

 [Fact]
 public void Register_And_Spawn_Works()
 {
 var (api, lua) = Create();
 lua.DoString(@"api:RegisterPrefab('test/thing', function(e, props) e:AddTag('t'); e:SetStat('hp',7) end)");
 var ent = api.SpawnPrefab("test/thing",10,20, null);
 ent.X.Should().Be(10);
 ent.Y.Should().Be(20);
 ent.GetStat("hp").Should().Be(7);
 }

 [Fact]
 public void Duplicate_Register_Last_Wins()
 {
 var (api, lua) = Create();
 lua.DoString(@"api:RegisterPrefab('dup', function(e) e:SetStat('x',1) end)");
 lua.DoString(@"api:RegisterPrefab('dup', function(e) e:SetStat('x',2) end)");
 var ent = api.SpawnPrefab("dup");
 ent.GetStat("x").Should().Be(2);
 }

 [Fact]
 public void Unknown_Spawn_Throws()
 {
 var (api, _) = Create();
 var act = () => api.SpawnPrefab("missing");
 act.Should().Throw<ScriptRuntimeException>();
 }

 [Fact]
 public void Client_Call_Throws()
 {
 var (api, lua) = Create(server:false);
 var spawn = () => api.SpawnPrefab("any");
 spawn.Should().Throw<ScriptRuntimeException>();
 Action reg = () => api.RegisterPrefab("id", DynValue.NewNumber(1));
 reg.Should().Throw<ScriptRuntimeException>();
 Action post = () => api.AddPrefabPostInit("id", DynValue.NewNumber(1));
 post.Should().Throw<ScriptRuntimeException>();
 }

 [Fact]
 public void PostInit_Called()
 {
 var (api, lua) = Create();
 lua.DoString(@"api:RegisterPrefab('pi', function(e) end)");
 int count=0;
 api.AddPrefabPostInit("pi", DynValue.NewCallback((c, a) => { count++; return DynValue.Nil; }));
 api.SpawnPrefab("pi");
 count.Should().Be(1);
 }
}
