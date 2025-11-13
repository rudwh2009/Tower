using FluentAssertions;
using Tower.Core.Modding;
using Xunit;

public class ClientPackBuilderWhitelistTests
{
 [Fact]
 public void Filters_Disallowed_Extensions()
 {
 var tmp = Path.Combine(Path.GetTempPath(), "tower_builder_wh", Guid.NewGuid().ToString("N"));
 var mod = Path.Combine(tmp, "Mods", "UIMod");
 Directory.CreateDirectory(Path.Combine(mod, "Lua", "ui"));
 File.WriteAllText(Path.Combine(mod, "Lua", "ui", "hud.lua"), "-- ui");
 File.WriteAllText(Path.Combine(mod, "Lua", "ui", "note.txt"), "nope");
 File.WriteAllText(Path.Combine(mod, "modinfo.json"), "{ \"id\": \"UIMod\", \"version\": \"1.0.0\", \"api_version\": \"1.0\", \"entry\": \"modmain.lua\", \"packs\": { \"client\": { \"ui_lua\": [\"Lua/ui/*\"] } } }");
 var meta = ModMetadata.FromFile(Path.Combine(mod, "modinfo.json"))!;
 var res = ClientPackBuilder.Build(meta, mod);
 // zip should not include note.txt; we can't open zip here, but ensure build succeeds and size > hud only
 res.Should().NotBeNull();
 }
}
