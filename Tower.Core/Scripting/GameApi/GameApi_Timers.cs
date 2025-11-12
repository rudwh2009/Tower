// Copyright (c) Tower.
// Licensed under the MIT License.

using Serilog;

namespace Tower.Core.Scripting.GameApi;

/// <summary>
/// Timer scheduling helpers exposed to Lua.
/// </summary>
public sealed partial class GameApi
{
    /// <summary>Schedules a one-shot timer.</summary>
    public string ScheduleTimer(double delaySeconds, LuaAction cb)
    {
        if (cb is null)
        {
            throw new ArgumentNullException(nameof(cb));
        }

        return this.timers.Schedule(delaySeconds, () =>
        {
            try
            {
                cb(null);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Timer cb failed");
            }
        });
    }

    /// <summary>Schedules a repeating interval timer.</summary>
    public string Interval(double seconds, LuaAction cb)
    {
        if (cb is null)
        {
            throw new ArgumentNullException(nameof(cb));
        }

        return this.timers.Interval(seconds, () =>
        {
            try
            {
                cb(null);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Interval cb failed");
            }
        });
    }

    /// <summary>Cancels a timer by id.</summary>
    public bool Cancel(string id)
    {
        return this.timers.Cancel(id);
    }
}
