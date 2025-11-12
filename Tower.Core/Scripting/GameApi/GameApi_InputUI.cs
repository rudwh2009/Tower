// Copyright (c) Tower.
// Licensed under the MIT License.

using MoonSharp.Interpreter;
using Tower.Core.Engine.UI;

namespace Tower.Core.Scripting.GameApi;

/// <summary>
/// Input and UI bridged to engine sinks.
/// </summary>
public sealed partial class GameApi
{
 private IUiSink? uiSink;
 public void SetUiSink(IUiSink sink) => uiSink = sink ?? throw new ArgumentNullException(nameof(sink));

 /// <summary>Registers an input action callback (server-only placeholder for future input binding).</summary>
 public void OnAction(string action, LuaAction fn)
 {
 sideGate.EnsureServer("Input.OnAction");
 if (string.IsNullOrWhiteSpace(action)) throw new ScriptRuntimeException("action required");
 if (fn is null) throw new ScriptRuntimeException("callback required");
 // For now we only acknowledge; real input binding can be added later
 }

 /// <summary>Creates HUD text. Client-only; on server throws.</summary>
 public void CreateHudText(string text)
 {
 if (uiSink is null) return; // no-op if no client sink
 uiSink.ShowHudText(text ?? string.Empty);
 }
}
