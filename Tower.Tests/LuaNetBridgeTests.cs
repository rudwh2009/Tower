using FluentAssertions;
using MoonSharp.Interpreter;
using Tower.Core.Scripting.Net;
using Xunit;

public class LuaNetBridgeTests
{
 [Fact]
 public void Bridge_Invokes_Lua_OnInput()
 {
 var script = new Script();
 script.Globals["store"] = DynValue.NewTable(script);
 script.DoString(@"function on_input(cid, tick, action)
 store.cid = cid; store.tick = tick; store.action = action
 end");
 var fn = script.Globals.Get("on_input");
 var bridge = new LuaNetBridge(script, fn);
 bridge.OnInput(7,42, "MoveRight");
 var store = script.Globals.Get("store");
 store.Table.Get("cid").Number.Should().Be(7);
 store.Table.Get("tick").Number.Should().Be(42);
 store.Table.Get("action").String.Should().Be("MoveRight");
 }
}
