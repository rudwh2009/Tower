using FluentAssertions;
using MoonSharp.Interpreter;
using Tower.Core.Engine.Assets;
using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;
using Tower.Core.Scripting;
using Tower.Core.Scripting.GameApi;
using Xunit;

public class SystemOrderingTests
{
 [Fact]
 public void Systems_Run_In_Deterministic_Order()
 {
 var assets = new AssetService(); var bus = new EventBus(); var systems = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, systems, timers);
 api.SetSideGate(new Tower.Core.Engine.Prefabs.SideGate(true));
 var lua = new LuaRuntime(api);
 lua.DoString(@"
 trace = ''
 api:AddSystem('B',10, function(dt) trace = trace .. 'B' end)
 api:AddSystem('A',0, function(dt) trace = trace .. 'A' end)
 ");
 systems.Update(0.05);
 var dv = lua.DoString("return trace");
 dv.String.Should().Be("AB");
 }
}
