// Copyright (c) Tower.
// Licensed under the MIT License.

using Serilog;

namespace Tower.Core.Scripting.GameApi;

/// <summary>
/// Event bus bridging for Lua.
/// </summary>
public sealed partial class GameApi
{
    private readonly Dictionary<string, List<(string evt, Action<object?> cb)>> _modSubscriptions = new(StringComparer.Ordinal);

    /// <summary>Subscribe a Lua handler to a named event, returns a handle for unsubscribe.</summary>
    public object SubscribeEvent(string eventName, LuaAction handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        Action<object?> shim = payload =>
        {
            try
            {
                handler(payload);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lua event handler failed: {Event}", eventName);
            }
        };
        this.bus.Subscribe(eventName, shim);
        var mod = currentModId ?? "";
        if (!string.IsNullOrWhiteSpace(mod))
        {
            if (!_modSubscriptions.TryGetValue(mod, out var list)) { list = []; _modSubscriptions[mod] = list; }
            list.Add((eventName, shim));
        }
        return shim;
    }

    /// <summary>Unsubscribe a previously registered handler using the handle returned by SubscribeEvent.</summary>
    public void UnsubscribeEvent(string eventName, object handle)
    {
        if (string.IsNullOrWhiteSpace(eventName) || handle is null) return;
        if (handle is Action<object?> action)
        {
            this.bus.Unsubscribe(eventName, action);
        }
    }

    /// <summary>Unsubscribe all event handlers registered by a specific mod.</summary>
    public void UnsubscribeAllForMod(string modId)
    {
        if (string.IsNullOrWhiteSpace(modId)) return;
        if (_modSubscriptions.TryGetValue(modId, out var list))
        {
            foreach (var (evt, cb) in list)
            {
                this.bus.Unsubscribe(evt, cb);
            }
            _modSubscriptions.Remove(modId);
        }
    }

    /// <summary>Emit an event with an optional payload.</summary>
    public void EmitEvent(string eventName, object? payload = null)
    {
        this.bus.Publish(eventName, payload);
    }
}
