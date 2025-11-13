using Tower.Net.Protocol.Messages;

namespace Tower.Net.Session;

public interface IRevisionCache
{
 bool IsFresh(int contentRevision, ModAdvert[] mods);
 void Save(int contentRevision, ModAdvert[] mods);
}
