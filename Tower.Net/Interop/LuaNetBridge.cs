using Serilog;

namespace Tower.Net.Interop;

public sealed class LuaNetBridge
{
 private readonly GameNetApi _api;
 public LuaNetBridge(GameNetApi api) => _api = api;
 public void Connect() => _api.Connect();
 public void SendEvent(string name, string payload) => _api.SendEvent(name, payload);
 public void SubscribeEvent(string name, Action<string> handler) { Log.Information("LuaNetBridge.SubscribeEvent {Name}", name); }
}
