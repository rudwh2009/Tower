using Tower.Net.Session;

namespace Tower.Client;

public sealed class CacheIndex : IModPackCache
{
 private readonly string _contentRoot;
 public CacheIndex(string contentRoot) { _contentRoot = contentRoot; }
 public bool Has(string id, string sha256)
 {
 var dir = Path.Combine(_contentRoot, "Cache", "Mods");
 if (!Directory.Exists(dir)) return false;
 foreach (var sub in Directory.GetDirectories(dir, id + "@*" + "@*"))
 {
 var name = Path.GetFileName(sub);
 var parts = name.Split('@');
 if (parts.Length ==3 && string.Equals(parts[0], id, StringComparison.Ordinal) && string.Equals(parts[2], sha256, StringComparison.OrdinalIgnoreCase))
 return true;
 }
 return false;
 }
}
