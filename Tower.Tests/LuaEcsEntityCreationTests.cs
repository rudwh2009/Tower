using FluentAssertions;
using MoonSharp.Interpreter;
using Tower.Core.Engine.Assets;
using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;
using Tower.Core.Scripting;
using Tower.Core.Scripting.GameApi;
using Tower.Net.Session;
using Xunit;

public class LuaEcsEntityCreationTests
{
 [Fact]
 public void Lua_Can_Create_Entity_And_Register_Component()
 {
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers);
 api.SetSideGate(new Tower.Core.Engine.Prefabs.SideGate(true));
 var server = new NetServer(new Tower.Net.Transport.LoopbackTransport());
 api.SetEntityRegistry(server);
 var lua = new LuaRuntime(api);
 var code = @"
 local id = api:CreateEntity()
 api:RegisterComponent('mod.sample', 'Transform')
 api:SetComponentData(id, 'mod.sample', 'Transform', '{""x"":1,""y"":2}')
 return id
 ";
 var dv = lua.DoString(code);
 var id = (int)dv.Number;
 id.Should().BeGreaterThan(0);
 }
}
