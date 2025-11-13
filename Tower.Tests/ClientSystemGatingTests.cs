using FluentAssertions;
using MoonSharp.Interpreter;
using Tower.Core.Engine.Assets;
using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;
using Tower.Core.Scripting;
using Tower.Core.Scripting.GameApi;
using Xunit;

public class ClientSystemGatingTests
{
 [Fact]
 public void AddSystem_Throws_On_Client()
 {
 var assets = new AssetService(); var bus = new EventBus(); var systems = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, systems, timers);
 api.SetSideGate(new Tower.Core.Engine.Prefabs.SideGate(false)); // client
 var lua = new LuaRuntime(api);
 var act = () => lua.DoString("api:AddSystem('test',0, function(dt) end)");
 act.Should().Throw<ScriptRuntimeException>().Which.DecoratedMessage.Should().Contain("server-only");
 }
}
