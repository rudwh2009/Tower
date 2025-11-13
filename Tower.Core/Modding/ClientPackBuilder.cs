using System.IO.Compression;
using System.Security.Cryptography;

namespace Tower.Core.Modding;

public static class ClientPackBuilder
{
 public sealed record Result(byte[] Bytes, string Sha256, int Size);
 private const int MaxFiles =4000;
 private static readonly HashSet<string> AllowedExt = new(StringComparer.OrdinalIgnoreCase)
 { ".png", ".json", ".ogg", ".wav", ".mp3", ".ttf", ".lua", ".ember" };

 public static Result Build(ModMetadata meta, string root)
 {
 if (meta is null) throw new ArgumentNullException(nameof(meta));
 if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException("root");
 var files = EnumerateFiles(root, meta.ClientUiLua);
 using var ms = new MemoryStream();
 using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen:true))
 {
 int count =0;
 foreach (var rel in files)
 {
 // whitelist
 var accept = rel.EndsWith(".particles.json", StringComparison.OrdinalIgnoreCase) || AllowedExt.Contains(Path.GetExtension(rel));
 if (!accept) continue;
 count++; if (count > MaxFiles) throw new InvalidDataException("pack has too many files");
 var full = Path.GetFullPath(Path.Combine(root, rel));
 var entry = zip.CreateEntry(rel.Replace('\\','/'), CompressionLevel.Optimal);
 using var src = File.OpenRead(full);
 using var dst = entry.Open();
 src.CopyTo(dst);
 }
 }
 var bytes = ms.ToArray();
 using var sha = SHA256.Create();
 var hash = sha.ComputeHash(bytes);
 var shaHex = Convert.ToHexString(hash).ToLowerInvariant();
 return new Result(bytes, shaHex, bytes.Length);
 }

 private static IEnumerable<string> EnumerateFiles(string root, string[] patterns)
 {
 var set = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
 foreach (var pat in patterns ?? Array.Empty<string>())
 {
 var rel = pat.Replace('/', Path.DirectorySeparatorChar);
 var dir = Path.GetDirectoryName(rel) ?? string.Empty;
 var filePattern = Path.GetFileName(rel);
 var fullDir = Path.GetFullPath(Path.Combine(root, dir));
 if (!Directory.Exists(fullDir)) continue;
 foreach (var file in Directory.EnumerateFiles(fullDir, filePattern))
 {
 var full = Path.GetFullPath(file);
 if (!full.StartsWith(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase)) continue;
 var relPath = Path.GetRelativePath(root, full);
 set.Add(relPath);
 }
 }
 return set;
 }
}
