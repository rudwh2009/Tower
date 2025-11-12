using Tower.Core.Engine.EventBus;
using Serilog;

namespace Tower.Core.Engine.Timing;

public sealed class TimerService : ITimerService
{
 private readonly IEventBus _bus;
 private readonly object _gate = new();
 private double _now;
 private readonly Dictionary<string, (double when, double? period, Action cb)> _timers = new();

 public TimerService(IEventBus bus) => _bus = bus;

 public string Schedule(double delaySeconds, Action callback)
 {
 if (delaySeconds <0) delaySeconds =0;
 if (callback is null) throw new ArgumentNullException(nameof(callback));
 var id = Guid.NewGuid().ToString("N");
 lock (_gate) _timers[id] = (_now + delaySeconds, null, Safe(callback));
 return id;
 }

 public string Interval(double intervalSeconds, Action callback)
 {
 if (intervalSeconds <=0) throw new ArgumentOutOfRangeException(nameof(intervalSeconds));
 if (callback is null) throw new ArgumentNullException(nameof(callback));
 var id = Guid.NewGuid().ToString("N");
 lock (_gate) _timers[id] = (_now + intervalSeconds, intervalSeconds, Safe(callback));
 return id;
 }

 public bool Cancel(string id)
 {
 lock (_gate) return _timers.Remove(id);
 }

 public void Update(double dtSeconds)
 {
 _now += Math.Max(0, dtSeconds);
 List<string> due = new();
 lock (_gate)
 {
 foreach (var kv in _timers)
 {
 if (kv.Value.when <= _now) due.Add(kv.Key);
 }
 }
 foreach (var id in due)
 {
 (double when, double? period, Action cb) entry;
 bool exists;
 lock (_gate) exists = _timers.TryGetValue(id, out entry);
 if (!exists) continue;
 try { entry.cb(); }
 catch (Exception ex) { Log.Error(ex, "Timer callback failed"); }
 if (entry.period is { } p)
 {
 lock (_gate) _timers[id] = (_now + p, p, entry.cb);
 }
 else
 {
 Cancel(id);
 }
 }
 }

 private Action Safe(Action cb) => () =>
 {
 try { cb(); }
 catch (Exception ex) { _bus.Publish("timer_exception", ex); }
 };
}
