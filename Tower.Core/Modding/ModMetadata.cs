using System.Text.Json;
using System.Text.RegularExpressions;
using System.Linq;

namespace Tower.Core.Modding;

public sealed record ModMetadata(string Id, string Version, string ApiVersion, string Entry, string[] Dependencies)
{
    private static readonly Regex IdRegex = new(@"^[A-Za-z0-9_-]{1,32}$", RegexOptions.Compiled);
    public string[] ServerLua { get; init; } = Array.Empty<string>();
    public string[] ClientUiLua { get; init; } = Array.Empty<string>();

    public static ModMetadata? FromFile(string path)
    {
        using var s = File.OpenRead(path);
        var doc = JsonDocument.Parse(s);
        var root = doc.RootElement;
        var id = root.GetProperty("id").GetString() ?? string.Empty;
        var version = root.TryGetProperty("version", out var v) ? v.GetString() ?? "0.0.0" : "0.0.0";
        var api = root.TryGetProperty("api_version", out var a) ? a.GetString() ?? "0" : "0";
        var entry = root.TryGetProperty("entry", out var e) ? e.GetString() ?? "modmain.lua" : "modmain.lua";
        var deps = root.TryGetProperty("dependencies", out var d) && d.ValueKind == JsonValueKind.Array
            ? d.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => x.Length > 0).ToArray()
            : Array.Empty<string>();
        if (!IdRegex.IsMatch(id)) return null;
        var meta = new ModMetadata(id, version, api, entry, deps);
        if (root.TryGetProperty("packs", out var packs) && packs.ValueKind == JsonValueKind.Object)
        {
            if (packs.TryGetProperty("server", out var server) && server.ValueKind == JsonValueKind.Object)
            {
                if (server.TryGetProperty("lua", out var luaArr) && luaArr.ValueKind == JsonValueKind.Array)
                {
                    meta = meta with { ServerLua = luaArr.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => x.Length > 0).ToArray() };
                }
            }
            if (packs.TryGetProperty("client", out var client) && client.ValueKind == JsonValueKind.Object)
            {
                if (client.TryGetProperty("ui_lua", out var uiArr) && uiArr.ValueKind == JsonValueKind.Array)
                {
                    meta = meta with { ClientUiLua = uiArr.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => x.Length > 0).ToArray() };
                }
            }
        }
        return meta;
    }
}
