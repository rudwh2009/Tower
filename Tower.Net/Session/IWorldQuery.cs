namespace Tower.Net.Session;

public interface IWorldQuery
{
 bool TryGetPosition(int id, out float x, out float y);
 IEnumerable<(int id, float x, float y)> GetEntitiesNear(int centerId, float radius);
}
