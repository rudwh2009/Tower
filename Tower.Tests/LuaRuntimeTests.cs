using Tower.Core.Scripting;
using Tower.Core.Engine.Assets;
using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;
using Tower.Core.Scripting.GameApi;
using FluentAssertions;
using Xunit;

public class LuaRuntimeTests
{
    [Fact]
    public void Sandbox_HasApi()
    {
        var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
        var api = new GameApi(assets, bus, sys, timers);
        var rt = new LuaRuntime(api);
    }
}
