using Serilog;
using System.Numerics;
using Tower.Core.Engine.Vfx;

namespace Tower.Core.Scripting.GameApi;

public sealed partial class GameApi
{
 private IVfxSink? vfxSink;
 public GameApiVfx Vfx { get; }

 public void SetVfxSink(IVfxSink sink)
 {
 vfxSink = sink;
 }

 public GameApi()
 {
 Vfx = new GameApiVfx(() => vfxSink);
 }
}

public sealed class GameApiVfx
{
 private readonly Func<IVfxSink?> getSink;
 public GameApiVfx(Func<IVfxSink?> getSink) => this.getSink = getSink;

 public int SpawnParticles(string logicalId, double x, double y, double rot =0, double scale =1)
 {
 var sink = getSink();
 if (sink is null)
 {
 Log.Warning("VFX spawn called without client sink: {Id}", logicalId);
 return0;
 }
 var h = sink.Spawn(logicalId, new Vector2((float)x, (float)y), (float)rot, (float)scale);
 return h.Id;
 }

 public void AttachParticles(string logicalId, object entityProxy, double ox, double oy)
 {
 var sink = getSink();
 if (sink is null)
 {
 Log.Warning("VFX attach called without client sink: {Id}", logicalId);
 return;
 }
 sink.Attach(logicalId, entityProxy, new Vector2((float)ox, (float)oy));
 }

 public void StopParticles(int handle)
 {
 var sink = getSink();
 if (sink is null)
 {
 Log.Warning("VFX stop called without client sink: {Handle}", handle);
 return;
 }
 sink.Stop(new VfxHandle(handle));
 }
}
