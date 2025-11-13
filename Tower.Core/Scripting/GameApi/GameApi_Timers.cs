// Copyright (c) Tower.
// Licensed under the MIT License.

using Serilog;

namespace Tower.Core.Scripting.GameApi;

/// <summary>
/// Timer scheduling helpers exposed to Lua.
/// </summary>
public sealed partial class GameApi
{
 private readonly Dictionary<string, List<string>> _modTimers = new(StringComparer.Ordinal);

 /// <summary>Schedules a one-shot timer. Server-only.</summary>
 public string ScheduleTimer(double delaySeconds, LuaAction cb)
 {
 sideGate.EnsureServer("Timers.Schedule");
 if (cb is null)
 {
 throw new ArgumentNullException(nameof(cb));
 }

 var id = this.timers.Schedule(delaySeconds, () =>
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
 var mod = currentModId ?? "";
 if (!string.IsNullOrWhiteSpace(mod))
 {
 if (!_modTimers.TryGetValue(mod, out var list)) { list = []; _modTimers[mod] = list; }
 list.Add(id);
 }
 return id;
 }

 /// <summary>Schedules a repeating interval timer. Server-only.</summary>
 public string Interval(double seconds, LuaAction cb)
 {
 sideGate.EnsureServer("Timers.Interval");
 if (cb is null)
 {
 throw new ArgumentNullException(nameof(cb));
 }

 var id = this.timers.Interval(seconds, () =>
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
 var mod = currentModId ?? "";
 if (!string.IsNullOrWhiteSpace(mod))
 {
 if (!_modTimers.TryGetValue(mod, out var list)) { list = []; _modTimers[mod] = list; }
 list.Add(id);
 }
 return id;
 }

 /// <summary>Cancels a timer by id. Server-only.</summary>
 public bool Cancel(string id)
 {
 sideGate.EnsureServer("Timers.Cancel");
 return this.timers.Cancel(id);
 }

 /// <summary>Cancel all timers registered by a specific mod.</summary>
 public void CancelAllForMod(string modId)
 {
 if (string.IsNullOrWhiteSpace(modId)) return;
 if (_modTimers.TryGetValue(modId, out var list))
 {
 foreach (var id in list) this.timers.Cancel(id);
 _modTimers.Remove(modId);
 }
 }
}
