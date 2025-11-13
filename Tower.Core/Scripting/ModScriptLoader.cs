using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;

namespace Tower.Core.Scripting;

public sealed class ModScriptLoader : ScriptLoaderBase
{
 private readonly List<string> _roots = new();
 public ModScriptLoader() { }
 public ModScriptLoader(IEnumerable<string> roots) { _roots.AddRange(roots); }
 public void SetRoot(string root)
 {
 if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException("root");
 var full = Path.GetFullPath(root);
 if (!_roots.Contains(full, StringComparer.OrdinalIgnoreCase)) _roots.Add(full);
 }

 public override object LoadFile(string file, Table globalContext)
 {
 var path = ResolvePath(file);
 return File.ReadAllText(path);
 }

 public override bool ScriptFileExists(string name)
 {
 try { var _ = ResolvePath(name); return true; } catch { return false; }
 }

 private static bool HasReparsePointInPath(string baseDir, string full)
 {
 try
 {
 var baseFull = Path.GetFullPath(baseDir);
 var dirPath = Path.GetDirectoryName(full);
 if (string.IsNullOrEmpty(dirPath)) return false;
 var dir = new DirectoryInfo(dirPath);
 while (dir != null && dir.FullName.StartsWith(baseFull, StringComparison.OrdinalIgnoreCase))
 {
 if ((dir.Attributes & FileAttributes.ReparsePoint) !=0) return true;
 dir = dir.Parent;
 }
 return false;
 }
 catch { return true; }
 }

 private string ResolvePath(string name)
 {
 if (_roots.Count ==0) throw new ScriptRuntimeException("script root not set");
 if (Path.IsPathRooted(name)) throw new ScriptRuntimeException("absolute path not allowed");
 if (name.Contains("..")) throw new ScriptRuntimeException("parent paths not allowed");
 var rel = name.Replace('/', Path.DirectorySeparatorChar);
 foreach (var root in _roots)
 {
 var combined = Path.Combine(root, rel);
 var full = Path.GetFullPath(combined);
 if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(full))
 {
 if (HasReparsePointInPath(root, full)) throw new ScriptRuntimeException("script path contains reparse point");
 return full;
 }
 }
 throw new ScriptRuntimeException($"script not found: {name}");
 }
}
