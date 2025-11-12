using MoonSharp.Interpreter;
using Tower.Core.Engine.Sound;
using Tower.Core.Engine.Prefabs;

namespace Tower.Core.Scripting.GameApi;

[MoonSharpUserData]
public sealed class GameApiSound
{
 private readonly ISoundSink sink;
 private readonly ISideGate gate;
 public GameApiSound(ISoundSink sink, ISideGate gate) { this.sink = sink; this.gate = gate; }
 public DynValue Play(string id, bool loop = false, double vol =1.0, double pitch =0.0, double pan =0.0)
 {
 gate.EnsureClient("Sound.Play");
 var handle = sink.Play(id, loop, (float)vol, (float)pitch, (float)pan);
 return DynValue.NewNumber(handle);
 }
 public void Stop(double handle)
 {
 gate.EnsureClient("Sound.Stop");
 sink.Stop((int)handle);
 }
 public void SetVolume(double handle, double vol)
 {
 gate.EnsureClient("Sound.SetVolume");
 sink.SetVolume((int)handle, (float)vol);
 }
}

public sealed partial class GameApi
{
 private ISoundSink? soundSink;
 public void SetSoundSink(ISoundSink sink)
 {
 soundSink = sink;
 Sound = new GameApiSound(sink, sideGate);
 }
 public GameApiSound? Sound { get; private set; }
}
