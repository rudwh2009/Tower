using Tower.Core.Engine.Assets;
using Tower.Core.Modding;
using Tower.Core.Scripting;
using Tower.Core.Scripting.GameApi;
using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;
using Xunit;

public class ModBootstrapperTests
{
    [Fact]
    public void Loads_BaseGame_And_Sample()
    {
        var assets = new AssetService();
        var bus = new EventBus();
        var systems = new SystemRegistry();
        var timers = new TimerService(bus);
        var api = new GameApi(assets, bus, systems, timers);
        var lua = new LuaRuntime(api);
        var mods = new ModBootstrapper(assets, lua, api);
        mods.LoadAll(Path.Combine(AppContext.BaseDirectory, "Content"));
    }
}
