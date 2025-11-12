namespace Tower.Core.Modding;

public static class PathSafety
{
    private static readonly HashSet<string> AllowedExt = new(StringComparer.OrdinalIgnoreCase)
 { ".png", ".json", ".ogg", ".wav", ".ttf", ".mgfxo", ".lua" };

    public static bool IsSafe(string rel)
    {
        if (string.IsNullOrWhiteSpace(rel)) return false;
        if (Path.IsPathRooted(rel)) return false;
        if (rel.Contains("..")) return false;
        var ext = Path.GetExtension(rel);
        return AllowedExt.Contains(ext);
    }
}
