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

public class GameApiWorldPublishTests
{
 private sealed class FakePublisher : IWorldPublisher
 {
 public readonly Dictionary<int,(float x,float y)> Data = new();
 public void PublishEntityPosition(int id, float x, float y) { Data[id] = (x, y); }
 }

 [Fact]
 public void Publish_Entity_Position_Server_Only_And_Validates()
 {
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers);
 var pub = new FakePublisher();
 api.SetSideGate(new Tower.Core.Engine.Prefabs.SideGate(true));
 api.SetWorldPublisher(pub);
 var lua = new LuaRuntime(api);
 lua.DoString("api:PublishEntityPosition(10,1.5, -2.25)");
 pub.Data[10].x.Should().Be(1.5f);
 pub.Data[10].y.Should().Be(-2.25f);
 // client gating
 var apiC = new GameApi(assets, bus, sys, timers); apiC.SetSideGate(new Tower.Core.Engine.Prefabs.SideGate(false)); apiC.SetWorldPublisher(pub);
 var luaC = new LuaRuntime(apiC);
 var act = () => luaC.DoString("api:PublishEntityPosition(1,0,0)");
 act.Should().Throw<ScriptRuntimeException>().Which.DecoratedMessage.Should().Contain("server-only");
 }
}
