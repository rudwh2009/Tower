using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Tower.Core.Engine.Assets;
using Serilog;

namespace Tower.Client.Assets;

public sealed class AssetAdapter : IDisposable
{
 private readonly GraphicsDevice _gd;
 private readonly Dictionary<string, Texture2D> _textures = new(StringComparer.Ordinal);
 private readonly Dictionary<string, SoundEffect> _sounds = new(StringComparer.Ordinal);
 private Texture2D? _fallbackTex;
 private SoundEffect? _fallbackSfx;

 private const int MaxTextureSize =4096;
 private const double MaxSoundSeconds =120.0;

 public AssetAdapter(GraphicsDevice gd) { _gd = gd; }

 private Texture2D FallbackTexture()
 {
 if (_fallbackTex is null)
 {
 _fallbackTex = new Texture2D(_gd,1,1);
 _fallbackTex.SetData(new[] { new Microsoft.Xna.Framework.Color(255,0,255,255) });
 }
 return _fallbackTex;
 }

 private SoundEffect FallbackSound()
 {
 if (_fallbackSfx is null)
 {
 var samples = new byte[200]; // short silence
 _fallbackSfx = new SoundEffect(samples,8000, AudioChannels.Mono);
 }
 return _fallbackSfx;
 }

 public Texture2D GetTexture(TextureHandle handle)
 {
 var key = handle.Path;
 if (_textures.TryGetValue(key, out var t)) return t;
 try
 {
 using var fs = File.OpenRead(handle.Path);
 var tex = Texture2D.FromStream(_gd, fs);
 if (tex.Width > MaxTextureSize || tex.Height > MaxTextureSize)
 {
 Log.Warning("Texture exceeds max size {W}x{H}: {Path}", tex.Width, tex.Height, handle.Path);
 tex.Dispose();
 tex = FallbackTexture();
 }
 _textures[key] = tex;
 return tex;
 }
 catch (Exception ex)
 {
 Log.Warning(ex, "Texture load failed: {Path}", handle.Path);
 var fb = FallbackTexture();
 _textures[key] = fb;
 return fb;
 }
 }

 public SoundEffect GetSound(SoundHandle handle)
 {
 var key = handle.Path;
 if (_sounds.TryGetValue(key, out var s)) return s;
 try
 {
 using var fs = File.OpenRead(handle.Path);
 var se = SoundEffect.FromStream(fs);
 if (se.Duration.TotalSeconds > MaxSoundSeconds)
 {
 Log.Warning("Sound too long ({Sec:F1}s): {Path}", se.Duration.TotalSeconds, handle.Path);
 se.Dispose();
 se = FallbackSound();
 }
 _sounds[key] = se;
 return se;
 }
 catch (Exception ex)
 {
 Log.Warning(ex, "Sound load failed: {Path}", handle.Path);
 var fb = FallbackSound();
 _sounds[key] = fb;
 return fb;
 }
 }

 public void Dispose()
 {
 foreach (var kv in _textures) if (!ReferenceEquals(kv.Value, _fallbackTex)) kv.Value.Dispose();
 foreach (var kv in _sounds) if (!ReferenceEquals(kv.Value, _fallbackSfx)) kv.Value.Dispose();
 _textures.Clear();
 _sounds.Clear();
 _fallbackTex?.Dispose(); _fallbackTex = null;
 _fallbackSfx?.Dispose(); _fallbackSfx = null;
 }
}
