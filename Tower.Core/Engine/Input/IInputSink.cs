namespace Tower.Core.Engine.Input;

public interface IInputSink
{
 void Bind(string action, string key);
 void Subscribe(string action, System.Action onPress);
}
