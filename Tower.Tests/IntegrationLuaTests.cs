using Tower.Core.Engine.Assets;
using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;
using Tower.Core.Modding;
using Tower.Core.Scripting;
using Tower.Core.Scripting.GameApi;
using Xunit;

public class IntegrationLuaTests
{
 [Fact]
 public void BaseGame_Loads_And_System_Runs()
 {
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers); var lua = new LuaRuntime(api);
 var mods = new ModBootstrapper(assets, lua, api);
 mods.LoadAll(Path.Combine(AppContext.BaseDirectory, "Content"));
 sys.Add("noop",100, _=>{});
 sys.Update(0.016);
 }
}
