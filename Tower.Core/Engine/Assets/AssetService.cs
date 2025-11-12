using System.Text.Json;
using Serilog;
using Tower.Core.Engine.Assets.Particles;

namespace Tower.Core.Engine.Assets;

public sealed class AssetService : IAssetService
{
 private static readonly HashSet<string> AllowedExt = new(StringComparer.OrdinalIgnoreCase)
 { ".png", ".json", ".ogg", ".wav", ".ttf", ".mgfxo", ".particles.json", ".ember", ".mp3" };

 private readonly Dictionary<string, Func<object?>> _loaders = new(StringComparer.Ordinal);
 private readonly Dictionary<string, object?> _cache = new(StringComparer.Ordinal);
 private readonly Dictionary<string, Func<object?>> _particleLoaders = new(StringComparer.Ordinal);
 private readonly Dictionary<string, object?> _particleCache = new(StringComparer.Ordinal);
 private readonly Dictionary<string, Func<object>> _soundLoaders = new(StringComparer.Ordinal);
 private readonly Dictionary<string, object> _soundCache = new(StringComparer.Ordinal);

 public void RegisterFromManifest(string modId, string rootDir, string manifestPath)
 {
 if (string.IsNullOrWhiteSpace(modId)) throw new ArgumentException("modId");
 var manifestFull = Path.Combine(rootDir, manifestPath);
 if (!File.Exists(manifestFull)) { Log.Warning("Manifest missing: {Path}", manifestFull); return; }
 using var stream = File.OpenRead(manifestFull);
 var doc = JsonDocument.Parse(stream);
 var root = doc.RootElement;
 if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
 {
 foreach (var item in assets.EnumerateArray())
 {
 if (!item.TryGetProperty("id", out var idProp) || !item.TryGetProperty("path", out var pathProp)) continue;
 var id = idProp.GetString();
 var rel = pathProp.GetString();
 if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(rel)) continue;
 if (!IsSafePath(rel)) { Log.Warning("Unsafe path rejected: {Path}", rel); continue; }
 var full = Path.GetFullPath(Path.Combine(rootDir, rel));
 var ext = Path.GetExtension(full);
 if (!AllowedExt.Contains(ext)) { Log.Warning("Extension not allowed: {Ext}", ext); continue; }
 _loaders[$"{modId}/{id}"] = () => LoadAsset(full);
 }
 }
 if (root.TryGetProperty("particles", out var particles) && particles.ValueKind == JsonValueKind.Array)
 {
 foreach (var item in particles.EnumerateArray())
 {
 if (!item.TryGetProperty("id", out var idProp) || !item.TryGetProperty("path", out var pathProp)) continue;
 var id = idProp.GetString();
 var rel = pathProp.GetString();
 if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(rel)) continue;
 if (!IsSafePath(rel)) { Log.Warning("Unsafe path rejected: {Path}", rel); continue; }
 var full = Path.GetFullPath(Path.Combine(rootDir, rel));
 var ext = Path.GetExtension(full);
 if (!AllowedExt.Contains(ext) && !full.EndsWith(".particles.json", StringComparison.OrdinalIgnoreCase)) { Log.Warning("Extension not allowed: {Ext}", ext); continue; }
 var logicalId = $"{modId}/{id}";
 if (full.EndsWith(".ember", StringComparison.OrdinalIgnoreCase))
 {
 _particleLoaders[logicalId] = () => new ParticleEffectDef { SourcePath = full, SourceKind = ParticleSourceKind.Ember };
 }
 else
 {
 _particleLoaders[logicalId] = () => LoadParticleJson(full);
 }
 }
 }
 if (root.TryGetProperty("sounds", out var sounds) && sounds.ValueKind == JsonValueKind.Array)
 {
 foreach (var item in sounds.EnumerateArray())
 {
 if (!item.TryGetProperty("id", out var idProp) || !item.TryGetProperty("path", out var pathProp)) continue;
 var id = idProp.GetString();
 var rel = pathProp.GetString();
 if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(rel)) continue;
 if (!IsSafePath(rel)) { Log.Warning("Unsafe path rejected: {Path}", rel); continue; }
 var full = Path.GetFullPath(Path.Combine(rootDir, rel));
 var ext = Path.GetExtension(full);
 if (!AllowedExt.Contains(ext)) { Log.Warning("Extension not allowed: {Ext}", ext); continue; }
 var logicalId = $"{modId}/{id}";
 _soundLoaders[logicalId] = () => LoadSound(full);
 }
 }
 }

 public bool TryGet(string logicalId, out object? asset)
 {
 if (_cache.TryGetValue(logicalId, out asset)) return true;
 if (_loaders.TryGetValue(logicalId, out var loader))
 {
 asset = loader();
 _cache[logicalId] = asset;
 return true;
 }
 asset = null; return false;
 }

 public bool TryGetParticle(string logicalId, out object? particleDef)
 {
 if (_particleCache.TryGetValue(logicalId, out particleDef)) return true;
 if (_particleLoaders.TryGetValue(logicalId, out var loader))
 {
 particleDef = loader();
 _particleCache[logicalId] = particleDef;
 return true;
 }
 particleDef = null; return false;
 }

 public void RegisterSound(string logicalId, Func<object> loader)
 {
 if (string.IsNullOrWhiteSpace(logicalId)) throw new ArgumentException("logicalId");
 if (loader is null) throw new ArgumentNullException(nameof(loader));
 _soundLoaders[logicalId] = loader;
 }

 public bool TryGetSound(string logicalId, out object? sound)
 {
 if (_soundCache.TryGetValue(logicalId, out var s)) { sound = s; return true; }
 if (_soundLoaders.TryGetValue(logicalId, out var loader))
 {
 try { var obj = loader(); _soundCache[logicalId] = obj; sound = obj; return true; }
 catch (Exception ex) { Log.Error(ex, "Failed to load sound {Id}", logicalId); }
 }
 sound = null; return false;
 }

 public object GetSound(string logicalId)
 {
 if (TryGetSound(logicalId, out var s) && s is not null) return s;
 throw new FileNotFoundException($"Sound not found: {logicalId}");
 }

 private static bool IsSafePath(string rel)
 {
 if (Path.IsPathRooted(rel)) return false;
 if (rel.Contains("..")) return false;
 return true;
 }

 private static object? LoadAsset(string full)
 {
 try
 {
 return new { Path = full };
 }
 catch (Exception ex)
 {
 Log.Error(ex, "Failed to load asset {Path}", full);
 return null;
 }
 }

 private static object? LoadParticleJson(string full)
 {
 try
 {
 var json = File.ReadAllText(full);
 return ParticleEffectDef.FromJson(json, full);
 }
 catch (Exception ex)
 {
 Log.Error(ex, "Failed to load particle json {Path}", full);
 return null;
 }
 }

 private static object LoadSound(string full)
 {
 return new { Path = full };
 }
}
