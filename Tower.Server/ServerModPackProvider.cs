using Tower.Core.Modding;
using Tower.Net.Protocol.Messages;
using Tower.Net.Session;

namespace Tower.Server;

public sealed class ServerModPackProvider : IModPackProvider
{
 private readonly List<(ModMetadata meta, string root)> _mods;
 public ServerModPackProvider(IEnumerable<(ModMetadata meta, string root)> mods) { _mods = mods.ToList(); }
 public ModAdvert[] GetAdvertisedMods()
 {
 var list = new List<ModAdvert>();
 foreach (var (meta, root) in _mods)
 {
 var pack = ClientPackBuilder.Build(meta, root);
 list.Add(new ModAdvert(meta.Id, meta.Version, pack.Sha256, pack.Size, meta.ApiVersion));
 }
 return list.ToArray();
 }
 public byte[] GetPackBytes(string id, string sha256)
 {
 var pair = _mods.FirstOrDefault(m => string.Equals(m.meta.Id, id, StringComparison.Ordinal));
 if (pair == default) return Array.Empty<byte>();
 var pack = ClientPackBuilder.Build(pair.meta, pair.root);
 return pack.Bytes;
 }
}
