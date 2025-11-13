using MoonSharp.Interpreter;
using Tower.Net.Session;

namespace Tower.Core.Scripting.GameApi;

public sealed partial class GameApi
{
 private IWorldPublisher? _publisher;
 /// <summary>Engine-only: Sets the world publisher. Allows the server to publish entity positions.</summary>
 public void SetWorldPublisher(IWorldPublisher publisher) { _publisher = publisher; }
 /// <summary>Publishes an entity's position to the server world. Server-only.</summary>
 public void PublishEntityPosition(double id, double x, double y)
 {
 sideGate.EnsureServer("World.PublishEntityPosition");
 var iid = (int)id; if (iid <=0) throw new ScriptRuntimeException("invalid entity id");
 if (_publisher is null) throw new ScriptRuntimeException("world publisher not configured");
 _publisher.PublishEntityPosition(iid, (float)x, (float)y);
 }
}
