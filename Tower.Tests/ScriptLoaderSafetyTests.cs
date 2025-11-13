using FluentAssertions;
using Tower.Core.Scripting;
using Xunit;

public class ScriptLoaderSafetyTests
{
    [Fact]
    public void Rejects_Absolute_And_Parent_Paths()
    {
        var loader = new ModScriptLoader();
        loader.SetRoot(Path.GetTempPath());
        loader.ScriptFileExists("/abs.lua").Should().BeFalse();
        loader.ScriptFileExists("..\\escape.lua").Should().BeFalse();
        loader.ScriptFileExists("..//escape.lua").Should().BeFalse();
    }
}
