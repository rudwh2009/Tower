using FluentAssertions;
using Tower.Net.Transport;
using Tower.Net.Session;
using Tower.Net.Abstractions;
using Tower.Net.Clock;
using Tower.Core.Engine.Assets;
using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;
using Tower.Core.Scripting;
using Tower.Core.Scripting.GameApi;
using Tower.Core.Scripting.Net;
using MoonSharp.Interpreter;
using Xunit;

public class LuaDrivenMovementIntegrationTests
{
 [Fact]
 public void Lua_OnInput_And_System_Publish_Positions_To_Server_World()
 {
 // Arrange server + client
 ITransport t = new LoopbackTransport();
 var server = new NetServer(t);
 server.SetClock(new FixedTickClock(20));
 server.SetInterestRadius(1000f);
 var client = new NetClient(t);
 server.Start(); client.Connect("p1");
 for (int i=0;i<20 && client.ClientId==0;i++) { server.Poll(); client.Poll(); }
 client.ClientId.Should().BeGreaterThan(0);

 // Arrange Lua gameplay runtime and API (server-only)
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers);
 api.SetSideGate(new Tower.Core.Engine.Prefabs.SideGate(true));
 api.SetWorldPublisher(server);
 var lua = new LuaRuntime(api);
 // Lua: on_input updates pos[cid]; system publishes positions each update
 var code = @"
 pos = {}
 function on_input(cid, tick, action)
 local p = pos[cid] or { x =0, y =0 }
 if action == 'MoveRight' then p.x = p.x +1 end
 if action == 'MoveLeft' then p.x = p.x -1 end
 if action == 'MoveUp' then p.y = p.y -1 end
 if action == 'MoveDown' then p.y = p.y +1 end
 pos[cid] = p
 end
 api:AddSystem('push',0, function(dt)
 for cid, p in pairs(pos) do api:PublishEntityPosition(cid, p.x, p.y) end
 end)
 ";
 lua.DoString(code);
 var onInput = lua.Script.Globals.Get("on_input");
 var bridge = new LuaNetBridge(lua.Script, onInput);
 server.SetGameplaySink(bridge);

 // Act: send two inputs and run a few ticks of systems and networking
 client.SendInput("MoveRight",1);
 for (int i=0;i<5;i++) { sys.Update(1.0/20); server.Poll(); client.Poll(); }
 client.LastSnapshot.x.Should().BeGreaterThanOrEqualTo(1);
 client.LastSnapshot.y.Should().Be(0);
 client.LastSnapshot.entityId.Should().Be(client.ClientId);
 client.SendInput("MoveRight",2);
 for (int i=0;i<5;i++) { sys.Update(1.0/20); server.Poll(); client.Poll(); }
 client.LastSnapshot.x.Should().BeGreaterThanOrEqualTo(2);
 }
}
