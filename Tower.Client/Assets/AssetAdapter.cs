extern alias nvorbis;
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

 private static SoundEffect DecodeOggToSoundEffect(string path)
 {
 using var vorbis = new nvorbis::NVorbis.VorbisReader(path);
 int channels = vorbis.Channels;
 int sampleRate = vorbis.SampleRate;
 var ms = new MemoryStream();
 var floatBuf = new float[4096 * channels];
 var bytes = new byte[floatBuf.Length *2];
 int read;
 while ((read = vorbis.ReadSamples(floatBuf,0, floatBuf.Length)) >0)
 {
 int byteCount = read *2; //16-bit
 for (int i =0; i < read; i++)
 {
 var f = Math.Clamp(floatBuf[i], -1f,1f);
 short s = (short)(f * short.MaxValue);
 bytes[i *2] = (byte)(s &0xFF);
 bytes[i *2 +1] = (byte)((s >>8) &0xFF);
 }
 ms.Write(bytes,0, byteCount);
 }
 var pcm = ms.ToArray();
 var chan = channels ==1 ? AudioChannels.Mono : AudioChannels.Stereo;
 return new SoundEffect(pcm, sampleRate, chan);
 }

 public SoundEffect GetSound(SoundHandle handle)
 {
 var key = handle.Path;
 if (_sounds.TryGetValue(key, out var s)) return s;
 try
 {
 var ext = Path.GetExtension(handle.Path);
 SoundEffect se;
 if (ext.Equals(".ogg", StringComparison.OrdinalIgnoreCase))
 {
 se = DecodeOggToSoundEffect(handle.Path);
 }
 else
 {
 using var fs = File.OpenRead(handle.Path);
 se = SoundEffect.FromStream(fs);
 }
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
