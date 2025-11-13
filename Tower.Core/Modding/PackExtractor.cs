using System.IO.Compression;

namespace Tower.Core.Modding;

public static class PackExtractor
{
 private const int MaxFiles =4000;
 private const long MaxTotalUncompressed =512L *1024 *1024; //512 MB
 private const long MaxEntryUncompressed =64L *1024 *1024; //64 MB
 private const int MaxCompressionRatio =100; // uncompressed/compressed

 public static string ExtractToCache(string contentRoot, string modId, string version, string sha, byte[] zipBytes)
 {
 var cacheRoot = Path.Combine(contentRoot, "Cache", "Mods", modId + "@" + version + "@" + sha);
 var tmp = cacheRoot + ".tmp";
 if (Directory.Exists(cacheRoot)) return cacheRoot;
 if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
 Directory.CreateDirectory(tmp);
 long totalUncompressed =0;
 int fileCount =0;
 using (var ms = new MemoryStream(zipBytes))
 using (var zip = new ZipArchive(ms, ZipArchiveMode.Read))
 {
 foreach (var entry in zip.Entries)
 {
 if (string.IsNullOrEmpty(entry.FullName)) continue;
 var rel = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
 if (rel.StartsWith("..") || Path.IsPathRooted(rel)) throw new InvalidDataException("invalid path in zip");
 var dest = Path.GetFullPath(Path.Combine(tmp, rel));
 if (!dest.StartsWith(Path.GetFullPath(tmp), StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("zip path escape");
 // stats
 var uncompressedLen = entry.Length; // may be -1 for directories
 if (uncompressedLen >0)
 {
 fileCount++;
 if (fileCount > MaxFiles) throw new InvalidDataException("zip too many files");
 totalUncompressed += uncompressedLen;
 if (totalUncompressed > MaxTotalUncompressed) throw new InvalidDataException("zip too large");
 if (uncompressedLen > MaxEntryUncompressed) throw new InvalidDataException("zip entry too large");
 var comp = Math.Max(1L, entry.CompressedLength);
 var ratio = (double)uncompressedLen / comp;
 if (ratio > MaxCompressionRatio) throw new InvalidDataException("zip compression ratio too high");
 }
 Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
 using var src = entry.Open();
 using var dst = File.Create(dest);
 src.CopyTo(dst);
 }
 }
 // post: ensure no reparse points
 foreach (var d in Directory.EnumerateDirectories(tmp, "*", SearchOption.AllDirectories))
 {
 var di = new DirectoryInfo(d);
 if ((di.Attributes & FileAttributes.ReparsePoint) !=0) throw new IOException("reparse point detected");
 }
 Directory.Move(tmp, cacheRoot);
 return cacheRoot;
 }
}
