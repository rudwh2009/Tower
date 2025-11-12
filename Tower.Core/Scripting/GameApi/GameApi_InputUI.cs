// Copyright (c) Tower.
// Licensed under the MIT License.

using MoonSharp.Interpreter;
using Tower.Core.Engine.UI;
using Tower.Core.Engine.Input;

namespace Tower.Core.Scripting.GameApi;

/// <summary>
/// Input and UI bridged to engine sinks.
/// </summary>
public sealed partial class GameApi
{
 private IUiSink? uiSink;
 private IInputSink? inputSink;
 public void SetUiSink(IUiSink sink) => uiSink = sink ?? throw new ArgumentNullException(nameof(sink));
 public void SetInputSink(IInputSink sink) => inputSink = sink ?? throw new ArgumentNullException(nameof(sink));

 // Nested UI + Input namespaces for client-only
 public GameApiUi Ui => _ui ??= new GameApiUi(this);
 private GameApiUi? _ui;
 public GameApiInput Input => _input ??= new GameApiInput(this);
 private GameApiInput? _input;

 [MoonSharpUserData]
 public sealed class GameApiUi
 {
 private readonly GameApi _api;
 internal GameApiUi(GameApi api) { _api = api; }
 public void Clear() { _api.sideGate.EnsureClient("UI.Clear"); _api.uiSink?.Clear(); }
 public void AddText(string id, string text, double x, double y, string? fontId = null) { _api.sideGate.EnsureClient("UI.AddText"); _api.uiSink?.AddText(id, text, (float)x, (float)y, fontId); }
 public void SetText(string id, string text) { _api.sideGate.EnsureClient("UI.SetText"); _api.uiSink?.SetText(id, text); }
 public void Remove(string id) { _api.sideGate.EnsureClient("UI.Remove"); _api.uiSink?.Remove(id); }
 public void AddButton(string id, string text, double x, double y, Closure onClick) { _api.sideGate.EnsureClient("UI.AddButton"); _api.uiSink?.AddButton(id, text, (float)x, (float)y, () => onClick.Call()); }
 public void AddSystem(string name, int order, LuaUpdate update) { _api.sideGate.EnsureClient("UI.AddSystem"); _api.systems.Add(name, order, dt => update(dt)); }
 }

 [MoonSharpUserData]
 public sealed class GameApiInput
 {
 private readonly GameApi _api;
 internal GameApiInput(GameApi api) { _api = api; }
 public void Bind(string action, string key) { _api.sideGate.EnsureClient("Input.Bind"); _api.inputSink?.Bind(action, key); }
 public void Subscribe(string action, Closure fn) { _api.sideGate.EnsureClient("Input.Subscribe"); _api.inputSink?.Subscribe(action, () => fn.Call()); }
 }

 /// <summary>Legacy HUD text forwarder (client-only).</summary>
 public void CreateHudText(string text)
 {
 sideGate.EnsureClient("UI.HudText");
 uiSink?.ShowHudText(text ?? string.Empty);
 }
}
