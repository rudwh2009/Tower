using Tower.Net.Session;
using Tower.Net.Abstractions;
using Tower.Net.Protocol.Messages;
using Tower.Core.Modding;

namespace Tower.Client;

public sealed class ListenSync : IModPackConsumer
{
 private readonly string _contentRoot;
 public bool Started { get; private set; }
 public ListenSync(string contentRoot) { _contentRoot = contentRoot; }
 public NetClient CreateClient(ITransport transport) => new NetClient(transport, this);
 public void OnAdvertised(ModAdvert[] mods) { }
 public void OnPackComplete(string id, string sha256, byte[] bytes)
 {
 var modInfo = Path.Combine(_contentRoot, "Mods", id, "modinfo.json");
 var meta = ModMetadata.FromFile(modInfo) ?? new ModMetadata(id, "1.0.0", "1.0", "modmain.lua", Array.Empty<string>());
 PackExtractor.ExtractToCache(_contentRoot, id, meta.Version, sha256, bytes);
 }
 public void OnStartLoading(StartLoading start) { Started = true; }
}
