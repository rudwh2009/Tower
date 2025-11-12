using Serilog;

namespace Tower.Net.Interop;

public sealed class GameNetApi
{
 public void Connect() { Log.Information("GameNetApi.Connect() called"); }
 public void SendEvent(string name, string payload) { Log.Information("GameNetApi.SendEvent {Name} {Payload}", name, payload); }
}
