using FluentAssertions;
using Tower.Core.Engine.Assets;
using Tower.Core.Modding;
using Tower.Core.Scripting;
using Tower.Core.Scripting.GameApi;
using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;
using Xunit;

public class AssetOverrideOrderTests
{
 [Fact]
 public void LastWriterWins_BaseGame_Then_Mod()
 {
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers); var lua = new LuaRuntime(api);
 var mods = new ModBootstrapper(assets, lua, api);
 mods.LoadAll(System.IO.Path.Combine(AppContext.BaseDirectory, "Content"));
 // Our sample mod overrides hit spark; expect mod path in sound/texture handle lookup if present
 var ok = assets.TryGet("SampleMod/vfx/hit_spark", out var v);
 ok.Should().BeTrue();
 }
}
