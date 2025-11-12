using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;
using MoonSharp.Interpreter.Debugging;
using Serilog;

namespace Tower.Core.Scripting;

public sealed class LuaRuntime
{
 private readonly Script _script;
 public Script Script => _script;
 private readonly object _api;
 private readonly ModScriptLoader _loader = new();
 private readonly BudgetDebugger _budget = new();
 private int _instructionBudget =200000; // default per call

 public LuaRuntime(object gameApi)
 {
 _api = gameApi;
 _script = new Script(CoreModules.Basic | CoreModules.Table | CoreModules.String | CoreModules.ErrorHandling | CoreModules.Math);
 _script.Options.ScriptLoader = _loader;
 _script.AttachDebugger(_budget);
 var g = _script.Globals;
 g["io"] = DynValue.Nil;
 g["os"] = DynValue.Nil;
 g["debug"] = DynValue.Nil;
 g["api"] = UserData.Create(gameApi);
 g["GLOBAL"] = DynValue.NewTable(_script);
 LoadCompat();
 }

 public void SetScriptRoot(string root) => _loader.SetRoot(root);
 public void SetInstructionBudget(int instructions) => _instructionBudget = instructions;
 private void ResetBudget() => _budget.Reset(_instructionBudget);

 private void LoadCompat()
 {
 try
 {
 using var stream = typeof(LuaRuntime).Assembly.GetManifestResourceStream("Tower.Core.Scripting.LuaCompat.compat.lua");
 if (stream is null) return;
 using var reader = new StreamReader(stream);
 var code = reader.ReadToEnd();
 _script.DoString(code);
 }
 catch (Exception ex)
 {
 Serilog.Log.Warning(ex, "Failed to load compat.lua");
 }
 }

 public void SetModContext(string modId) => _script.Globals["MOD_ID"] = DynValue.NewString(modId);

 public DynValue DoString(string code, string? chunkName = null)
 {
 try { ResetBudget(); return _script.DoString(code, null, chunkName); }
 catch (ScriptRuntimeException sre) { Serilog.Log.Error("Lua runtime error: {Message}", sre.DecoratedMessage); throw; }
 }

 public DynValue DoFile(string path)
 {
 try { ResetBudget(); return _script.DoFile(path); }
 catch (ScriptRuntimeException sre) { Serilog.Log.Error("Lua runtime error: {Message}", sre.DecoratedMessage); throw; }
 }
}
