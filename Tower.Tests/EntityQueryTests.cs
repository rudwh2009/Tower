using FluentAssertions;
using Tower.Core.Engine.Assets;
using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;
using Tower.Core.Scripting;
using Tower.Core.Scripting.GameApi;
using Xunit;

public class EntityQueryTests
{
 [Fact]
 public void Proxy_Tags_Stats_And_Distance()
 {
 var assets = new AssetService(); var bus = new EventBus(); var sys = new SystemRegistry(); var timers = new TimerService(bus);
 var api = new GameApi(assets, bus, sys, timers); var lua = new LuaRuntime(api);
 api.SetSideGate(new Tower.Core.Engine.Prefabs.SideGate(true));
 lua.DoString("api:RegisterPrefab('p', function(e) e:AddTag('enemy'); e:SetStat('hp',10); e:SetString('name','a'); e:SetBool('alive', true) end)");
 var e = api.SpawnPrefab("p",10,20);
 e.HasTag("enemy").Should().BeTrue();
 e.GetStat("hp").Should().Be(10);
 e.GetString("name").Should().Be("a");
 e.GetBool("alive").Should().BeTrue();
 e.DistanceTo(10,22).Should().Be(2);
 e.RemoveTag("enemy");
 e.HasTag("enemy").Should().BeFalse();
 }
}
