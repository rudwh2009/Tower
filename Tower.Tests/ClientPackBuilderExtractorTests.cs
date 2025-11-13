using FluentAssertions;
using Tower.Core.Modding;
using Xunit;

public class ClientPackBuilderExtractorTests
{
 [Fact]
 public void Build_And_Extract_Client_Pack()
 {
 var tmp = Path.Combine(Path.GetTempPath(), "tower_pack", Guid.NewGuid().ToString("N"));
 var mod = Path.Combine(tmp, "Mods", "UIMod"); Directory.CreateDirectory(Path.Combine(mod, "Lua", "ui"));
 File.WriteAllText(Path.Combine(mod, "Lua", "ui", "hud.lua"), "-- ui");
 File.WriteAllText(Path.Combine(mod, "modinfo.json"), "{ \"id\": \"UIMod\", \"version\": \"1.0.0\", \"api_version\": \"1.0\", \"entry\": \"modmain.lua\", \"packs\": { \"client\": { \"ui_lua\": [\"Lua/ui/*.lua\"] } } }");
 var meta = ModMetadata.FromFile(Path.Combine(mod, "modinfo.json"))!;
 var res = ClientPackBuilder.Build(meta, mod);
 res.Bytes.Should().NotBeNull(); res.Sha256.Should().NotBeNullOrEmpty(); res.Size.Should().BeGreaterThan(0);
 var cache = PackExtractor.ExtractToCache(tmp, meta.Id, meta.Version, res.Sha256, res.Bytes);
 Directory.Exists(cache).Should().BeTrue();
 File.Exists(Path.Combine(cache, "Lua", "ui", "hud.lua")).Should().BeTrue();
 }
}
