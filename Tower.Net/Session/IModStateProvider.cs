namespace Tower.Net.Session;

public interface IModStateProvider
{
 System.Collections.Generic.Dictionary<string, object?> CollectSaveState();
 void ApplyLoadState(System.Collections.Generic.Dictionary<string, object?> state);
}
