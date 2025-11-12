// Copyright (c) Tower.
// Licensed under the MIT License.

using Serilog;

namespace Tower.Core.Scripting.GameApi;

/// <summary>
/// Event bus bridging for Lua.
/// </summary>
public sealed partial class GameApi
{
    /// <summary>Subscribe a Lua handler to a named event.</summary>
    public void SubscribeEvent(string eventName, LuaAction handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        this.bus.Subscribe(eventName, payload =>
        {
            try
            {
                handler(payload);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lua event handler failed: {Event}", eventName);
            }
        });
    }

    /// <summary>Emit an event with an optional payload.</summary>
    public void EmitEvent(string eventName, object? payload = null)
    {
        this.bus.Publish(eventName, payload);
    }
}
