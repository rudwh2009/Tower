using FluentAssertions;
using Tower.Core.Engine.Assets;
using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;
using Tower.Core.Engine.Save;
using Tower.Core.Scripting;
using Tower.Core.Scripting.GameApi;
using Xunit;

public class SaveSystemFileTests
{
 [Fact]
 public void Save_And_Load_File_RoundTrip()
 {
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers); var lua = new LuaRuntime(api);
 api.SetCurrentMod("M1"); lua.DoString("api:OnSave(function() return {score=99} end)"); lua.DoString("api:OnLoad(function(s) GLOBAL.round=s and s.score end)");
 var tmp = Path.Combine(Path.GetTempPath(), "tower_saves", Guid.NewGuid()+".json");
 var ss = new SaveSystem();
 ss.Save(api, tmp,12345,777);
 var res = ss.Load(api, tmp);
 res.saveVersion.Should().Be(1);
 res.worldSeed.Should().Be(12345);
 res.tick.Should().Be(777);
 var v = lua.Script.Globals.Get("GLOBAL").Table.Get("round").Number;
 v.Should().Be(99);
 }
}
