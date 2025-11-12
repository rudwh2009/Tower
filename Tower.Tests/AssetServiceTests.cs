using Tower.Core.Engine.Assets;
using FluentAssertions;
using Xunit;

public class AssetServiceTests
{
    [Fact]
    public void Rejects_PathTraversal()
    {
        var svc = new AssetService();
        var root = Path.GetTempPath();
        var manifest = Path.Combine(root, "assets.json");
        File.WriteAllText(manifest, "{\"assets\":[{\"id\":\"texture/bad\",\"path\":\"..\\evil.png\"}]}");
        svc.RegisterFromManifest("Mod", root, "assets.json");
        svc.TryGet("Mod/texture/bad", out _).Should().BeFalse();
    }
}
