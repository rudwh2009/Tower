using MoonSharp.Interpreter;
using Tower.Net.Session;

namespace Tower.Core.Scripting.Net;

/// <summary>
/// Bridges NetServer inputs to Lua gameplay. Server-only.
/// </summary>
public sealed class LuaNetBridge : INetGameplaySink
{
 private readonly Script _script;
 private readonly DynValue _onInput;
 private readonly DynValue _onJoin;
 private readonly DynValue _onLeave;
 private readonly DynValue _onAoiEnter;
 private readonly DynValue _onAoiLeave;
 public LuaNetBridge(Script script, DynValue onInput)
 {
 _script = script;
 if (onInput.Type != DataType.Function) throw new ScriptRuntimeException("onInput must be a function");
 _onInput = onInput;
 _onJoin = script.Globals.Get("on_client_join");
 _onLeave = script.Globals.Get("on_client_leave");
 _onAoiEnter = script.Globals.Get("on_aoi_enter");
 _onAoiLeave = script.Globals.Get("on_aoi_leave");
 }
 public void OnInput(int clientId, int tick, string action)
 {
 _onInput.Function.Call(DynValue.NewNumber(clientId), DynValue.NewNumber(tick), DynValue.NewString(action));
 }
 public void OnClientJoin(int clientId)
 {
 if (_onJoin.Type == DataType.Function) _onJoin.Function.Call(DynValue.NewNumber(clientId));
 }
 public void OnClientLeave(int clientId)
 {
 if (_onLeave.Type == DataType.Function) _onLeave.Function.Call(DynValue.NewNumber(clientId));
 }
 public void OnAoiEnter(int clientId, int entityId)
 {
 if (_onAoiEnter.Type == DataType.Function) _onAoiEnter.Function.Call(DynValue.NewNumber(clientId), DynValue.NewNumber(entityId));
 }
 public void OnAoiLeave(int clientId, int entityId)
 {
 if (_onAoiLeave.Type == DataType.Function) _onAoiLeave.Function.Call(DynValue.NewNumber(clientId), DynValue.NewNumber(entityId));
 }
}
