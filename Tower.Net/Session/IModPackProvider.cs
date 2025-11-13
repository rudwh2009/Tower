using Tower.Net.Protocol.Messages;

namespace Tower.Net.Session;

public interface IModPackProvider
{
 ModAdvert[] GetAdvertisedMods();
 byte[] GetPackBytes(string id, string sha256);
}
