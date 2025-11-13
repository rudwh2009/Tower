using Tower.Net.Protocol.Messages;

namespace Tower.Net.Session;

public interface IModPackConsumer
{
 void OnAdvertised(ModAdvert[] mods);
 void OnPackComplete(string id, string sha256, byte[] bytes);
 void OnStartLoading(StartLoading start);
}
