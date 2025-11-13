namespace Tower.Net.Session;

public interface INetGameplaySink
{
 void OnInput(int clientId, int tick, string action);
 void OnClientJoin(int clientId);
 void OnClientLeave(int clientId);
 void OnAoiEnter(int clientId, int entityId);
 void OnAoiLeave(int clientId, int entityId);
}
