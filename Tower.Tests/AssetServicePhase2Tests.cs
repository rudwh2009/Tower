using FluentAssertions;
using Tower.Core.Engine.Assets;
using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;
using Tower.Core.Modding;
using Tower.Core.Scripting;
using Tower.Core.Scripting.GameApi;
using Xunit;

public class AssetServicePhase2Tests
{
 [Fact]
 public void RegisterAssets_And_Get_Typed_Handles()
 {
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers); var lua = new LuaRuntime(api);
 var mods = new ModBootstrapper(assets, lua, api);
 mods.LoadAll(System.IO.Path.Combine(AppContext.BaseDirectory, "Content"));
 // particles exposed
 var p = api.GetParticle("BaseGame/vfx/muzzle"); p.Should().NotBeNull();
 }
}
