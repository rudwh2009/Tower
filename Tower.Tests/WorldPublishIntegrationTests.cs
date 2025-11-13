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
using Xunit;

public class WorldPublishIntegrationTests
{
 [Fact]
 public void Lua_Publish_Position_Appears_In_Client_Snapshot()
 {
 ITransport t = new LoopbackTransport();
 var server = new NetServer(t);
 server.SetClock(new FixedTickClock(20));
 server.SetInterestRadius(1000f);
 var client = new NetClient(t);
 server.Start(); client.Connect("p1");
 for (int i=0;i<20 && client.ClientId==0;i++) { server.Poll(); client.Poll(); }
 client.ClientId.Should().BeGreaterThan(0);
 // Create a server-only GameApi and publish player's position via Lua
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers);
 api.SetSideGate(new Tower.Core.Engine.Prefabs.SideGate(true));
 api.SetWorldPublisher(server);
 var lua = new LuaRuntime(api);
 var id = client.ClientId;
 lua.DoString($"api:PublishEntityPosition({id},3, -4)");
 // Trigger a snapshot by sending a noop input
 client.SendInput("MoveRight",1);
 for (int i=0;i<10;i++) { server.Poll(); client.Poll(); }
 client.LastSnapshot.entityId.Should().Be(id);
 client.LastSnapshot.x.Should().Be(3);
 client.LastSnapshot.y.Should().Be(-4);
 }
}
