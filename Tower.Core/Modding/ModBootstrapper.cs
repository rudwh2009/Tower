using Tower.Core.Engine.Assets;
using Tower.Core.Scripting;
using Tower.Core.Scripting.GameApi;
using Serilog;

namespace Tower.Core.Modding;

public sealed class ModBootstrapper
{
 private readonly IAssetService _assets;
 private readonly LuaRuntime _lua;
 private readonly GameApi _api;
 private const int EngineApiMajor =1;
 private const int EngineApiMinor =0;

 public ModBootstrapper(IAssetService assets, LuaRuntime lua, GameApi api)
 { _assets = assets; _lua = lua; _api = api; }

 public void LoadAll(string contentRoot) => LoadAll(contentRoot, executeScripts: true, clientMode:false);
 public void LoadAll(string contentRoot, bool executeScripts) => LoadAll(contentRoot, executeScripts, clientMode:false);
 public void LoadAll(string contentRoot, bool executeScripts, bool clientMode)
 {
 var basePath = Path.Combine(contentRoot, "BaseGame");
 var modsPath = Path.Combine(contentRoot, "Mods");
 var cacheModsPath = Path.Combine(contentRoot, "Cache", "Mods");
 var mods = new List<(ModMetadata meta, string root)>();
 if (Directory.Exists(basePath))
 {
 var baseInfo = Path.Combine(basePath, "modinfo.json");
 var meta = ModMetadata.FromFile(baseInfo);
 if (meta is not null) mods.Add((meta, basePath));
 }
 if (Directory.Exists(modsPath))
 {
 foreach (var dir in Directory.GetDirectories(modsPath))
 {
 var info = Path.Combine(dir, "modinfo.json");
 var meta = File.Exists(info) ? ModMetadata.FromFile(info) : null;
 if (meta is not null) mods.Add((meta, dir));
 }
 }
 if (Directory.Exists(cacheModsPath))
 {
 foreach (var dir in Directory.GetDirectories(cacheModsPath))
 {
 var info = Path.Combine(dir, "modinfo.json");
 var meta = File.Exists(info) ? ModMetadata.FromFile(info) : null;
 if (meta is not null) mods.Add((meta, dir));
 }
 }
 var order = TopoSort(mods);
 foreach (var (meta, root) in order)
 {
 try
 {
 // API version check
 var parts = (meta.ApiVersion ?? "0").Split('.');
 int major = parts.Length>0 && int.TryParse(parts[0], out var mj) ? mj :0;
 int minor = parts.Length>1 && int.TryParse(parts[1], out var mn) ? mn :0;
 if (major > EngineApiMajor) { Log.Error("Mod {Id} requires newer API {Req}", meta.Id, meta.ApiVersion); continue; }
 if (major == EngineApiMajor && minor > EngineApiMinor) { Log.Warning("Mod {Id} targets newer minor API {Req}", meta.Id, meta.ApiVersion); }
 Log.Information("Loading mod {Id} (api {Api})", meta.Id, meta.ApiVersion);
 _api.RegisterAssets(meta.Id, root, "assets.json");
 _lua.SetModContext(meta.Id);
 _api.SetCurrentMod(meta.Id);
 _lua.SetScriptRoot(root);
 if (!executeScripts) continue; // assets only
 // Select scripts based on mode
 string[] scripts;
 if (clientMode)
 scripts = meta.ClientUiLua.Length>0 ? meta.ClientUiLua : Array.Empty<string>();
 else
 scripts = meta.ServerLua.Length>0 ? meta.ServerLua : new[] { meta.Entry };
 foreach (var pattern in scripts)
 {
 // Support simple glob patterns using Directory.EnumerateFiles
 var dir = root;
 var rel = pattern.Replace('/', Path.DirectorySeparatorChar);
 var fullDir = Path.GetDirectoryName(Path.Combine(root, rel)) ?? root;
 var filePattern = Path.GetFileName(rel);
 var files = Directory.Exists(fullDir) ? Directory.EnumerateFiles(fullDir, filePattern) : Array.Empty<string>();
 foreach (var f in files)
 {
 var logical = Path.GetRelativePath(root, f).Replace(Path.DirectorySeparatorChar, '/');
 Log.Information("Executing {Mode} script {File}", clientMode ? "client" : "server", logical);
 _lua.DoFile(logical);
 }
 }
 }
 catch (Exception ex)
 {
 Log.Error(ex, "Mod failed, disabling: {Id}", meta.Id);
 }
 }
 }

 private static IEnumerable<(ModMetadata meta, string root)> TopoSort(List<(ModMetadata meta, string root)> mods)
 {
 var dict = mods.ToDictionary(m => m.meta.Id, StringComparer.Ordinal);
 var visited = new HashSet<string>(StringComparer.Ordinal);
 var temp = new HashSet<string>(StringComparer.Ordinal);
 var result = new List<(ModMetadata, string)>();
 void Visit(string id)
 {
 if (visited.Contains(id)) return;
 if (temp.Contains(id)) { Log.Warning("Cycle involving {Id}", id); return; }
 temp.Add(id);
 if (dict.TryGetValue(id, out var node))
 {
 foreach (var d in node.meta.Dependencies) if (dict.ContainsKey(d)) Visit(d); else Log.Warning("Missing dep {Dep} for {Id}", d, id);
 result.Add(node);
 }
 temp.Remove(id);
 visited.Add(id);
 }
 foreach (var m in mods) Visit(m.meta.Id);
 return result;
 }
}
