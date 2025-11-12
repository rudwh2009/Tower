// Copyright (c) Tower.
// Licensed under the MIT License.

using Serilog;

namespace Tower.Core.Scripting.GameApi;

/// <summary>
/// Input and UI no-op stubs for logging.
/// </summary>
public sealed partial class GameApi
{
    /// <summary>Registers an input action callback (no-op stub).</summary>
    public void OnAction(string action, LuaAction fn)
    {
        Log.Information("OnAction registered: {Action}", action);
    }

    /// <summary>Creates HUD text (no-op stub).</summary>
    public void CreateHudText(string text)
    {
        Log.Information("HUD: {Text}", text);
    }
}
