using Serilog;

namespace Tower.Core.Engine.Systems;

public sealed class SystemRegistry
{
 private readonly SortedList<int, List<Action<double>>> _update = new();
 private readonly object _gate = new();

 public void Add(string name, int order, Action<double> update)
 {
 if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name");
 if (update is null) throw new ArgumentNullException(nameof(update));
 lock (_gate)
 {
 if (!_update.TryGetValue(order, out var list))
 {
 list = [];
 _update[order] = list;
 }
 list.Add(Wrap(name, update));
 }
 }

 public void Update(double dtSeconds)
 {
 List<Action<double>> copy;
 lock (_gate)
 {
 copy = _update.Values.SelectMany(x => x).ToList();
 }
 foreach (var u in copy)
 {
 try
 {
 var start = DateTime.UtcNow;
 u(dtSeconds);
 var elapsed = DateTime.UtcNow - start;
 if (elapsed.TotalMilliseconds >8)
 {
 Log.Warning("System update exceeded budget: {Ms}ms", elapsed.TotalMilliseconds);
 }
 }
 catch (Exception ex)
 {
 Log.Error(ex, "System update failed");
 }
 }
 }

 private static Action<double> Wrap(string name, Action<double> update)
 {
 return dt =>
 {
 try { update(dt); }
 catch (Exception ex) { Log.Error(ex, "System {Name} threw", name); }
 };
 }
}
