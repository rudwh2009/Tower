using System.Text.Json;
using System.Text.RegularExpressions;
using System.Linq;

namespace Tower.Core.Modding;

public sealed record ModMetadata(string Id, string Version, string ApiVersion, string Entry, string[] Dependencies)
{
    private static readonly Regex IdRegex = new(@"^[A-Za-z0-9_-]{1,32}$", RegexOptions.Compiled);

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
        return new ModMetadata(id, version, api, entry, deps);
    }
}
