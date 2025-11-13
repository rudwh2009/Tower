using FluentAssertions;
using Tower.Server;
using Tower.Net.Session;
using Xunit;

public class ServerConfigBindingTests
{
 [Fact]
 public void Loads_Json_Config()
 {
 var tmp = Path.Combine(Path.GetTempPath(), "tower_cfg_test"); Directory.CreateDirectory(tmp);
 var path = Path.Combine(tmp, "netconfig.json");
 File.WriteAllText(path, "{\"SharedKey\":\"abc\",\"OutboundBytesPerSecond\":12345,\"InterestRadius\":42}");
 var cfg = NetConfigJson.Load(path);
 cfg.Should().NotBeNull();
 cfg!.SharedKey.Should().Be("abc");
 cfg.OutboundBytesPerSecond.Should().Be(12345);
 cfg.InterestRadius.Should().Be(42);
 }
}
