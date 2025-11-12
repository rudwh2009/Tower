using System.Collections.Generic;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Debugging;

namespace Tower.Core.Scripting;

internal sealed class BudgetDebugger : IDebugger
{
 private int _budget;
 private int _count;

 public void Reset(int instructions)
 {
 _budget = instructions <=0 ? int.MaxValue : instructions;
 _count =0;
 }

 public DebuggerAction GetAction(int ip, SourceRef sourceref)
 {
 _count++;
 if (_count > _budget)
 {
 throw new ScriptRuntimeException("script time budget exceeded");
 }
 return new DebuggerAction { Action = DebuggerAction.ActionType.Run };
 }

 public void SetDebugService(DebugService debugService) { }
 public void SignalExecutionEnded() { }
 public bool IsPauseRequested() => false;
 public DebuggerCaps GetDebuggerCaps() => DebuggerCaps.CanDebugSourceCode;
 public void SetSourceCode(SourceCode sourceCode) { }
 public void SetByteCode(string[] byteCode) { }
 public void RefreshBreakpoints(IEnumerable<SourceRef> refs) { }
 public bool SignalRuntimeException(ScriptRuntimeException ex) => false;
 public List<DynamicExpression> GetWatchItems() => new();
 public void Update(WatchType watchType, IEnumerable<WatchItem> items) { }
}
