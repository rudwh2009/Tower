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

 public ModBootstrapper(IAssetService assets, LuaRuntime lua, GameApi api)
 { _assets = assets; _lua = lua; _api = api; }

 public void LoadAll(string contentRoot)
 {
 var basePath = Path.Combine(contentRoot, "BaseGame");
 var modsPath = Path.Combine(contentRoot, "Mods");
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
 var order = TopoSort(mods);
 foreach (var (meta, root) in order)
 {
 try
 {
 Log.Information("Loading mod {Id}", meta.Id);
 _api.RegisterAssets(meta.Id, root, "assets.json");
 _lua.SetModContext(meta.Id);
 _api.SetCurrentMod(meta.Id);
 _lua.SetScriptRoot(root);
 var entry = Path.Combine(root, meta.Entry);
 if (File.Exists(entry)) _lua.DoFile(meta.Entry);
 else Log.Warning("Entry not found: {Entry}", entry);
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
