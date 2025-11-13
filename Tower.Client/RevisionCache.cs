using Tower.Net.Session;
using Tower.Net.Protocol.Messages;
using System.Text.Json;

namespace Tower.Client;

public sealed class RevisionCache : IRevisionCache
{
    private readonly string _path;
    private readonly string _contentRoot;
    public RevisionCache(string contentRoot) { _contentRoot = contentRoot; _path = Path.Combine(contentRoot, "Cache", "mods.rev.json"); }
    public bool IsFresh(int contentRevision, ModAdvert[] mods)
    {
        try
        {
            if (!File.Exists(_path)) return false;
            var txt = File.ReadAllText(_path);
            using var doc = System.Text.Json.JsonDocument.Parse(txt);
            var root = doc.RootElement;
            var rev = root.GetProperty("rev").GetInt32();
            if (rev != contentRevision) return false;
            // verify manifest packs exist
            var hasManifest = root.TryGetProperty("mods", out var arr) && arr.ValueKind == System.Text.Json.JsonValueKind.Array;
            if (hasManifest)
            {
                var cache = new CacheIndex(_contentRoot);
                foreach (var el in arr.EnumerateArray())
                {
                    var id = el.GetProperty("id").GetString() ?? string.Empty;
                    var sha = el.GetProperty("sha").GetString() ?? string.Empty;
                    if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(sha)) return false;
                    if (!cache.Has(id, sha)) return false;
                }
            }
            else
            {
                // fallback: check against advertised mods
                var cache = new CacheIndex(_contentRoot);
                foreach (var m in mods) if (!cache.Has(m.Id, m.Sha256)) return false;
            }
            return true;
        }
        catch { return false; }
    }
    public void Save(int contentRevision, ModAdvert[] mods)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var payload = new
        {
            rev = contentRevision,
            mods = mods.Select(m => new { id = m.Id, sha = m.Sha256 }).ToArray()
        };
        var json = JsonSerializer.Serialize(payload);
        File.WriteAllText(_path, json);
    }
}
