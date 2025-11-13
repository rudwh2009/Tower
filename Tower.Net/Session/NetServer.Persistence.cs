using System.Text.Json;
using Tower.Net.Protocol;
using Tower.Net.Protocol.Messages;

namespace Tower.Net.Session;

public sealed partial class NetServer
{
 private const int SaveVersion =1;
 private static readonly JsonSerializerOptions SaveJsonOptions = new(JsonSerializerDefaults.General) { WriteIndented = false };
 private sealed class SaveMod
 {
 public string id { get; set; } = string.Empty;
 public string ver { get; set; } = string.Empty;
 public string sha { get; set; } = string.Empty;
 }
 private sealed class SaveEntity
 {
 public int id { get; set; }
 public float x { get; set; }
 public float y { get; set; }
 public Dictionary<string, string> comps { get; set; } = new(StringComparer.Ordinal);
 public Dictionary<string, int>? compVersions { get; set; }
 }
 private sealed class SaveFile
 {
 public int saveVersion { get; set; } = SaveVersion;
 public int contentRevision { get; set; }
 public int worldSeed { get; set; }
 public int tick { get; set; }
 public List<SaveMod> mods { get; set; } = new();
 public List<SaveEntity> entities { get; set; } = new();
 public Dictionary<string, object?>? modState { get; set; }
 }
 public bool SaveWorld(string path, out string error)
 {
 error = string.Empty;
 try
 {
 var dir = Path.GetDirectoryName(path);
 if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
 if (_contentRevision ==0) _contentRevision = ComputeRevision(_mods);
 var file = new SaveFile
 {
 contentRevision = _contentRevision,
 worldSeed = _worldSeed,
 tick = _tick,
 mods = _mods.Select(m => new SaveMod { id = m.Id, ver = m.Version, sha = m.Sha256 }).ToList(),
 entities = _world.Select(kv => new SaveEntity
 {
 id = kv.Key,
 x = kv.Value.x,
 y = kv.Value.y,
 comps = _components.TryGetValue(kv.Key, out var cmap)
 ? cmap.ToDictionary(p => p.Key.modNs + ":" + p.Key.typeName, p => p.Value, StringComparer.Ordinal)
 : new Dictionary<string, string>(StringComparer.Ordinal),
 compVersions = _compVersion.Where(kv2 => kv2.Key.entityId == kv.Key)
 .ToDictionary(kv2 => kv2.Key.modNs + ":" + kv2.Key.typeName, kv2 => kv2.Value, StringComparer.Ordinal)
 }).ToList(),
 modState = _modStateProvider?.CollectSaveState()
 };
 var json = JsonSerializer.Serialize(file, SaveJsonOptions);
 var tmp = path + ".tmp";
 File.WriteAllText(tmp, json);
 if (File.Exists(path)) File.Replace(tmp, path, null);
 else File.Move(tmp, path);
 return true;
 }
 catch (Exception ex)
 {
 error = ex.Message;
 return false;
 }
 }
 public bool LoadWorld(string path, out string error)
 {
 error = string.Empty;
 try
 {
 var json = File.ReadAllText(path);
 var file = JsonSerializer.Deserialize<SaveFile>(json, SaveJsonOptions) ?? new SaveFile();
 // validate modset pin
 var currentRev = ComputeRevision(_mods);
 if (file.contentRevision != currentRev || file.mods.Count != _mods.Length || !file.mods.SequenceEqual(_mods.Select(m => new SaveMod { id = m.Id, ver = m.Version, sha = m.Sha256 }), new SaveModEq()))
 {
 error = "Mod set mismatch; cannot load save."; return false;
 }
 // reset world
 foreach (var kv in _world.ToArray()) { RemoveIndex(kv.Key, kv.Value.x, kv.Value.y); }
 _world.Clear(); _components.Clear(); _cells.Clear();
 _worldSeed = file.worldSeed; _tick = file.tick;
 foreach (var e in file.entities)
 {
 _world[e.id] = (e.x, e.y); IndexEntity(e.id, e.x, e.y);
 if (e.comps.Count >0)
 {
 var map = new Dictionary<(string, string), string>();
 foreach (var ck in e.comps)
 {
 var parts = ck.Key.Split(':',2);
 if (parts.Length ==2)
 {
 var compJson = ck.Value;
 // migrate if schema advanced
 var curVer = e.compVersions != null && e.compVersions.TryGetValue(ck.Key, out var fromVer) ? fromVer :0;
 if (_schemas.TryGetValue((parts[0], parts[1]), out var sch) && sch.Version > curVer)
 {
 try
 {
 using var doc = JsonDocument.Parse(compJson);
 int v = curVer;
 JsonElement elem = doc.RootElement;
 while (v < sch.Version)
 {
 var next = v +1;
 if (_migrations.TryGetValue((parts[0], parts[1], v, next), out var mig))
 {
 elem = mig(elem);
 v = next;
 }
 else { break; }
 }
 compJson = elem.GetRawText();
 }
 catch { /* leave as-is on failure */ }
 }
 map[(parts[0], parts[1])] = compJson;
 // record new version
 if (_schemas.TryGetValue((parts[0], parts[1]), out var sch2)) _compVersion[(e.id, parts[0], parts[1])] = sch2.Version;
 }
 }
 if (map.Count >0) _components[e.id] = map;
 }
 }
 // apply per-mod state if present
 if (file.modState is not null && _modStateProvider is not null)
 {
 _modStateProvider.ApplyLoadState(file.modState);
 }
 return true;
 }
 catch (Exception ex)
 {
 error = ex.Message;
 return false;
 }
 }
 private sealed class SaveModEq : IEqualityComparer<SaveMod>
 {
 public bool Equals(SaveMod? a, SaveMod? b)
 {
 if (a is null || b is null) return false; return string.Equals(a.id, b.id, StringComparison.Ordinal) && string.Equals(a.ver, b.ver, StringComparison.Ordinal) && string.Equals(a.sha, b.sha, StringComparison.OrdinalIgnoreCase);
 }
 public int GetHashCode(SaveMod obj) => HashCode.Combine(obj.id, obj.ver, obj.sha?.ToLowerInvariant());
 }
 public bool TryGetComponentJson(int entityId, string modNs, string typeName, out string json)
 {
 json = string.Empty;
 if (_components.TryGetValue(entityId, out var map) && map.TryGetValue((modNs, typeName), out var v)) { json = v; return true; }
 return false;
 }
}
