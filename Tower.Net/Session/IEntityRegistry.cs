namespace Tower.Net.Session;

public interface IEntityRegistry
{
 int CreateEntity();
 void RegisterComponent(string modNs, string typeName);
 void SetComponentData(int entityId, string modNs, string typeName, string jsonUtf8);
}
