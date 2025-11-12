// Copyright (c) Tower.
// Licensed under the MIT License.

using Serilog;

namespace Tower.Core.Scripting.GameApi;

/// <summary>
/// System registration and execution.
/// </summary>
public sealed partial class GameApi
{
    /// <summary>Adds an update system callable from Lua with a specific order. Server-only.</summary>
    public void AddSystem(string name, int order, LuaUpdate update)
    {
        sideGate.EnsureServer("Systems.Add");
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (update is null)
        {
            throw new ArgumentNullException(nameof(update));
        }

        this.systems.Add(name, order, dt =>
        {
            try
            {
                update(dt);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lua system failed: {Name}", name);
            }
        });
    }
}
