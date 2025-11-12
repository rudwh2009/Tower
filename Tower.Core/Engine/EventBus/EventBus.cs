using Serilog;

namespace Tower.Core.Engine.EventBus;

public sealed class EventBus : IEventBus
{
 private readonly object _gate = new();
 private readonly Dictionary<string, List<Action<object?>>> _subscribers = new(StringComparer.Ordinal);

 public void Publish(string eventName, object? payload = null)
 {
 if (string.IsNullOrWhiteSpace(eventName)) return;
 List<Action<object?>>? targets;
 lock (_gate)
 {
 if (!_subscribers.TryGetValue(eventName, out var list)) return;
 targets = [.. list];
 }
 foreach (var h in targets!)
 {
 try { h(payload); }
 catch (Exception ex) { Log.Error(ex, "Event handler failed for {Event}", eventName); }
 }
 }

 public void Subscribe(string eventName, Action<object?> handler)
 {
 if (string.IsNullOrWhiteSpace(eventName)) throw new ArgumentException("eventName");
 if (handler is null) throw new ArgumentNullException(nameof(handler));
 lock (_gate)
 {
 if (!_subscribers.TryGetValue(eventName, out var list))
 {
 list = [];
 _subscribers[eventName] = list;
 }
 list.Add(handler);
 }
 }

 public void Unsubscribe(string eventName, Action<object?> handler)
 {
 if (string.IsNullOrWhiteSpace(eventName) || handler is null) return;
 lock (_gate)
 {
 if (_subscribers.TryGetValue(eventName, out var list))
 {
 list.RemoveAll(h => h == handler);
 if (list.Count ==0) _subscribers.Remove(eventName);
 }
 }
 }
}
