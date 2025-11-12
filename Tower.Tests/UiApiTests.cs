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
 private sealed class TestSink : IUiSink
 {
 public string? Last;
 public void ShowHudText(string text) => Last = text;
 public void Clear() { }
 public void AddText(string id, string text, float x, float y, string? fontId = null) { Last = text; }
 public void SetText(string id, string text) { Last = text; }
 public void Remove(string id) { }
 public void AddButton(string id, string text, float x, float y, System.Action onClick) { }
 }

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
