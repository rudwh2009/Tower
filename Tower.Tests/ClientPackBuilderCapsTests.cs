using FluentAssertions;
using Tower.Core.Modding;
using Xunit;

public class ClientPackBuilderCapsTests
{
 [Fact]
 public void Builder_Rejects_Too_Many_Files()
 {
 var tmp = Path.Combine(Path.GetTempPath(), "tower_builder", Guid.NewGuid().ToString("N"));
 var mod = Path.Combine(tmp, "Mods", "UIMod");
 Directory.CreateDirectory(Path.Combine(mod, "Lua", "ui"));
 // create4001 files
 for (int i=0;i<4001;i++) File.WriteAllText(Path.Combine(mod, "Lua", "ui", $"f{i}.lua"), "-- ui");
 File.WriteAllText(Path.Combine(mod, "modinfo.json"), "{ \"id\": \"UIMod\", \"version\": \"1.0.0\", \"api_version\": \"1.0\", \"entry\": \"modmain.lua\", \"packs\": { \"client\": { \"ui_lua\": [\"Lua/ui/*.lua\"] } } }");
 var meta = ModMetadata.FromFile(Path.Combine(mod, "modinfo.json"))!;
 var act = () => ClientPackBuilder.Build(meta, mod);
 act.Should().Throw<InvalidDataException>();
 }
}
