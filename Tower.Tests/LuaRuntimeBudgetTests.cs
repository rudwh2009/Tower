using FluentAssertions;
using Tower.Core.Scripting;
using Xunit;

public class LuaRuntimeBudgetTests
{
 [Fact]
 public void Infinite_Loop_Is_Aborted_By_Budget()
 {
 var api = new object();
 var lua = new LuaRuntime(api);
 lua.SetInstructionBudget(1000);
 lua.SetScriptRoot(System.IO.Path.GetTempPath());
 var act = () => lua.DoString("while true do end");
 act.Should().Throw<MoonSharp.Interpreter.ScriptRuntimeException>();
 }
}
