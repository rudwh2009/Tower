namespace Tower.Core.Engine.Vfx;

public interface IVfxSink
{
 VfxHandle Spawn(string effectId, System.Numerics.Vector2 pos, float rotation =0f, float scale =1f);
 VfxHandle Attach(string effectId, object entityProxy, System.Numerics.Vector2 localOffset);
 void Stop(VfxHandle handle);
}

public readonly record struct VfxHandle(int Id);
