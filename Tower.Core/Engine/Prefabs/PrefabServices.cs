using MoonSharp.Interpreter;
using Serilog;
using System.Text.RegularExpressions;
using Tower.Core.Engine.Entities;

namespace Tower.Core.Engine.Prefabs;

public interface IPrefabRegistry
{
 void Register(string id, Closure factory);
 bool TryGet(string id, out Closure factory);
}

public interface IHookBus
{
 void AddPrefabPostInit(string id, Closure hook);
 void InvokePrefabPostInits(string id, IEntityProxy ent, ILogger log);
}

public interface IEntitySpawner
{
 IEntityProxy Spawn(string id, SpawnContext ctx, Closure factory);
}

public readonly record struct SpawnContext(double X, double Y, Table? Props);

public interface ISideGate
{
 bool IsServer { get; }
 void EnsureServer(string apiName);
 void EnsureClient(string apiName);
}

public sealed class SideGate(bool isServer) : ISideGate
{
 public bool IsServer { get; private set; } = isServer;
 public void EnsureServer(string apiName)
 {
 if (!IsServer) throw new ScriptRuntimeException($"[API][{apiName}] server-only");
 }
 public void EnsureClient(string apiName)
 {
 if (IsServer) throw new ScriptRuntimeException($"[API][{apiName}] client-only");
 }
}

public sealed class PrefabRegistry : IPrefabRegistry
{
 private readonly Dictionary<string, Closure> _map = new(StringComparer.Ordinal);
 private static readonly Regex IdRx = new("^[A-Za-z0-9_./-]{1,64}$", RegexOptions.Compiled);
 private static string Normalize(string id) => id.Trim();
 public void Register(string id, Closure factory)
 {
 if (factory is null) throw new ScriptRuntimeException("factory nil");
 id = Normalize(id);
 if (!IdRx.IsMatch(id)) throw new ScriptRuntimeException("invalid id");
 _map[id] = factory; // last wins
 }
 public bool TryGet(string id, out Closure factory) => _map.TryGetValue(Normalize(id), out factory!);
}

public sealed class HookBus : IHookBus
{
 private readonly Dictionary<string, List<Closure>> _post = new(StringComparer.Ordinal);
 public void AddPrefabPostInit(string id, Closure hook)
 {
 if (hook is null) throw new ScriptRuntimeException("hook nil");
 id = id.Trim(); if (id.Length==0) throw new ScriptRuntimeException("invalid id");
 if (!_post.TryGetValue(id, out var list)) { list = []; _post[id] = list; }
 list.Add(hook);
 }
 public void InvokePrefabPostInits(string id, IEntityProxy ent, ILogger log)
 {
 if (_post.TryGetValue(id, out var list))
 {
 foreach (var h in list)
 {
 try { h.Call(UserData.Create(ent)); }
 catch (Exception ex) { log.Error(ex, "PostInit failed for {Id}", id); }
 }
 }
 }
}

public sealed class EntitySpawner : IEntitySpawner
{
 private static int _nextId;
 private sealed class Proxy : IEntityProxy
 {
 public int Id { get; } = Interlocked.Increment(ref _nextId);
 public double X { get; set; }
 public double Y { get; set; }
 public double VX { get; set; }
 public double VY { get; set; }
 private readonly HashSet<string> _tags = new(StringComparer.Ordinal);
 private readonly Dictionary<string,double> _stats = new(StringComparer.Ordinal);
 private readonly Dictionary<string,string> _sStats = new(StringComparer.Ordinal);
 private readonly Dictionary<string,bool> _bStats = new(StringComparer.Ordinal);
 private bool _destroyed;
 public void AddTag(string tag) { if (!string.IsNullOrWhiteSpace(tag)) _tags.Add(tag); }
 public void RemoveTag(string tag) { if (!string.IsNullOrWhiteSpace(tag)) _tags.Remove(tag); }
 public bool HasTag(string tag) => _tags.Contains(tag);
 public void SetStat(string name, double value) { if (!string.IsNullOrWhiteSpace(name)) _stats[name]=value; }
 public double GetStat(string name) => _stats.TryGetValue(name, out var v) ? v :0;
 public void SetString(string name, string value) { if (!string.IsNullOrWhiteSpace(name)) _sStats[name]=value; }
 public string? GetString(string name) => _sStats.TryGetValue(name, out var v) ? v : null;
 public void SetBool(string name, bool value) { if (!string.IsNullOrWhiteSpace(name)) _bStats[name]=value; }
 public bool GetBool(string name) => _bStats.TryGetValue(name, out var v) ? v : false;
 public double DistanceTo(double x, double y) { var dx = X-x; var dy = Y-y; return Math.Sqrt(dx*dx+dy*dy); }
 public void Destroy() { _destroyed = true; }
 }
 public IEntityProxy Spawn(string id, SpawnContext ctx, Closure factory)
 {
 try
 {
 var ent = new Proxy { X = ctx.X, Y = ctx.Y };
 // call Lua factory with proxy and props
 factory.Call(UserData.Create(ent), ctx.Props is null ? DynValue.Nil : DynValue.NewTable(ctx.Props));
 return ent;
 }
 catch (ScriptRuntimeException) { throw; }
 catch (Exception ex) { throw new ScriptRuntimeException($"prefab factory failed: {ex.Message}"); }
 }
}
