using FluentAssertions;
using Tower.Core.Engine.Assets;
using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;
using Tower.Core.Scripting;
using Tower.Core.Scripting.GameApi;
using Xunit;

public class SaveSystemRoundTripTests
{
 [Fact]
 public void Collect_And_Apply_State_Per_Mod()
 {
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers); var lua = new LuaRuntime(api);
 api.SetCurrentMod("M1"); lua.DoString("api:OnSave(function() return {score=42} end)"); lua.DoString("api:OnLoad(function(s) GLOBAL.after=s and s.score end)");
 api.SetCurrentMod("M2"); lua.DoString("api:OnSave(function() return7 end)"); lua.DoString("api:OnLoad(function(s) GLOBAL.after2=s end)");
 var state = api.CollectSaveState();
 state.Should().ContainKey("M1"); state.Should().ContainKey("M2");
 state["M1"].Should().NotBeNull();
 // simulate load
 api.ApplyLoadState(state);
 var v1 = lua.Script.Globals.Get("GLOBAL").Table.Get("after").Number;
 var v2 = lua.Script.Globals.Get("GLOBAL").Table.Get("after2").Number;
 v1.Should().Be(42);
 v2.Should().Be(7);
 }
}
