using System.Text.Json;
using Tower.Net.Session;

namespace Tower.Server;

public static class NetConfigJson
{
 public static NetServerConfig? Load(string path)
 {
 if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
 var json = File.ReadAllText(path);
 var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
 return JsonSerializer.Deserialize<NetServerConfig>(json, opts);
 }
}
