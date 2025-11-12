namespace Tower.Core.Engine.EventBus;

public interface IEventBus
{
 void Publish(string eventName, object? payload = null);
 void Subscribe(string eventName, Action<object?> handler);
 void Unsubscribe(string eventName, Action<object?> handler);
}
