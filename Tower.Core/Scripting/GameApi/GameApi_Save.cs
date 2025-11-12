using MoonSharp.Interpreter;
using System.Text.Json;

namespace Tower.Core.Scripting.GameApi;

public sealed partial class GameApi
{
 private readonly Dictionary<string, Closure> _saveHooks = new(StringComparer.Ordinal);
 private readonly Dictionary<string, Closure> _loadHooks = new(StringComparer.Ordinal);

 /// <summary>Registers a server-only save hook for the current mod. The closure should return a table or simple value.</summary>
 public void OnSave(DynValue fn)
 {
 sideGate.EnsureServer("Save.OnSave");
 if (fn.Type != DataType.Function) throw new ScriptRuntimeException("OnSave expects a function");
 var mod = currentModId ?? "";
 if (string.IsNullOrWhiteSpace(mod)) throw new ScriptRuntimeException("no current mod context");
 _saveHooks[mod] = fn.Function;
 }

 /// <summary>Registers a server-only load hook for the current mod. The closure will receive a payload.</summary>
 public void OnLoad(DynValue fn)
 {
 sideGate.EnsureServer("Save.OnLoad");
 if (fn.Type != DataType.Function) throw new ScriptRuntimeException("OnLoad expects a function");
 var mod = currentModId ?? "";
 if (string.IsNullOrWhiteSpace(mod)) throw new ScriptRuntimeException("no current mod context");
 _loadHooks[mod] = fn.Function;
 }

 /// <summary>Lists mods which registered save hooks (for diagnostics or SaveSystem integration).</summary>
 public IReadOnlyCollection<string> ListModsWithSaveHooks() => _saveHooks.Keys.ToArray();
 /// <summary>Lists mods which registered load hooks (for diagnostics or SaveSystem integration).</summary>
 public IReadOnlyCollection<string> ListModsWithLoadHooks() => _loadHooks.Keys.ToArray();

 /// <summary>Collects per-mod save states by invoking OnSave hooks. Server-only.</summary>
 public Dictionary<string, object?> CollectSaveState()
 {
 sideGate.EnsureServer("Save.Collect");
 var result = new Dictionary<string, object?>(StringComparer.Ordinal);
 foreach (var kv in _saveHooks)
 {
 var dv = kv.Value.Call();
 result[kv.Key] = ToPlainObject(dv);
 }
 return result;
 }

 /// <summary>Applies per-mod states by invoking OnLoad hooks with the provided object model. Server-only.</summary>
 public void ApplyLoadState(Dictionary<string, object?> modState)
 {
 sideGate.EnsureServer("Save.Apply");
 foreach (var kv in modState)
 {
 if (_loadHooks.TryGetValue(kv.Key, out var hook))
 {
 var arg = FromPlainObject(kv.Value, hook.OwnerScript);
 hook.Call(arg);
 }
 }
 }

 private static object? ToPlainObject(DynValue v)
 {
 switch (v.Type)
 {
 case DataType.Nil:
 case DataType.Void: return null;
 case DataType.Boolean: return v.Boolean;
 case DataType.Number: return v.Number;
 case DataType.String: return v.String;
 case DataType.Table:
 {
 var t = v.Table;
 // detect array vs map
 var dict = new Dictionary<string, object?>();
 var list = new List<object?>();
 var isArray = true;
 int count =0;
 foreach (var pair in t.Pairs)
 {
 count++;
 if (pair.Key.Type == DataType.Number)
 {
 list.Add(ToPlainObject(pair.Value));
 }
 else
 {
 isArray = false; break;
 }
 }
 if (isArray) return list;
 dict.Clear();
 foreach (var pair in t.Pairs)
 {
 var key = pair.Key.ToString();
 dict[key] = ToPlainObject(pair.Value);
 }
 return dict;
 }
 default:
 return v.ToString();
 }
 }

 private static DynValue FromPlainObject(object? obj, Script script)
 {
 if (obj is null) return DynValue.Nil;
 if (obj is bool b) return DynValue.NewBoolean(b);
 if (obj is int i) return DynValue.NewNumber(i);
 if (obj is long l) return DynValue.NewNumber(l);
 if (obj is double d) return DynValue.NewNumber(d);
 if (obj is float f) return DynValue.NewNumber(f);
 if (obj is string s) return DynValue.NewString(s);
 if (obj is IList<object?> list)
 {
 var t = new Table(script);
 int idx =1; foreach (var it in list) { t[idx] = FromPlainObject(it, script); idx++; }
 return DynValue.NewTable(t);
 }
 if (obj is IDictionary<string, object?> dict)
 {
 var t = new Table(script);
 foreach (var kv in dict) { t[kv.Key] = FromPlainObject(kv.Value, script); }
 return DynValue.NewTable(t);
 }
 return DynValue.NewString(obj.ToString() ?? string.Empty);
 }
}
