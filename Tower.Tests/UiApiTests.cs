using FluentAssertions;
using Tower.Core.Engine.Assets;
using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;
using Tower.Core.Scripting;
using Tower.Core.Scripting.GameApi;
using Tower.Core.Engine.UI;
using Xunit;

public class UiApiTests
{
 private sealed class TestSink : IUiSink { public string? Last; public void ShowHudText(string text) => Last = text; }

 [Fact]
 public void HudText_Routed_To_Sink()
 {
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers); var lua = new LuaRuntime(api);
 var sink = new TestSink(); api.SetUiSink(sink);
 api.CreateHudText("hello");
 sink.Last.Should().Be("hello");
 }
}
