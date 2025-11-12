using Microsoft.Xna.Framework.Audio;
using Serilog;
using Tower.Core.Engine.Assets;
using Tower.Core.Engine.Sound;

namespace Tower.Client.Audio;

public sealed class SoundManager : ISoundSink, IDisposable
{
 private readonly AssetService _assets;
 private readonly Dictionary<int, SoundEffectInstance> _active = new();
 private int _nextHandle =1;

 public SoundManager(AssetService assets) => _assets = assets;

 private static string? GetPathFromAsset(object obj)
 {
 var prop = obj.GetType().GetProperty("Path");
 return prop?.GetValue(obj) as string;
 }

 public int Play(string id, bool loop = false, float volume =1f, float pitch =0f, float pan =0f)
 {
 try
 {
 var obj = _assets.GetSound(id);
 var path = GetPathFromAsset(obj) ?? throw new InvalidOperationException("sound path missing");
 using var fs = File.OpenRead(path);
 var sfx = SoundEffect.FromStream(fs);
 var inst = sfx.CreateInstance();
 inst.IsLooped = loop;
 inst.Volume = Math.Clamp(volume,0f,1f);
 inst.Pitch = Math.Clamp(pitch, -1f,1f);
 inst.Pan = Math.Clamp(pan, -1f,1f);
 inst.Play();
 var handle = _nextHandle++;
 _active[handle] = inst;
 return handle;
 }
 catch (Exception ex)
 {
 Log.Warning(ex, "Play failed for sound {Id}", id);
 return 0;
 }
 }

 public void Stop(int handle)
 {
 if (_active.TryGetValue(handle, out var inst))
 {
 inst.Stop();
 inst.Dispose();
 _active.Remove(handle);
 }
 }

 public void SetVolume(int handle, float vol)
 {
 if (_active.TryGetValue(handle, out var inst))
 inst.Volume = Math.Clamp(vol,0f,1f);
 }

 public void Dispose()
 {
 foreach (var kv in _active)
 kv.Value.Dispose();
 _active.Clear();
 }
}
