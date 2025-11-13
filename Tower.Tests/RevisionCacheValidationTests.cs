using FluentAssertions;
using Tower.Client;
using Tower.Net.Protocol.Messages;
using Xunit;

public class RevisionCacheValidationTests
{
 [Fact]
 public void IsFresh_False_When_Cache_Missing_Then_True_When_Present()
 {
 var tmp = Path.Combine(Path.GetTempPath(), "tower_rev_test", Guid.NewGuid().ToString("N"));
 Directory.CreateDirectory(tmp);
 var cacheDir = Path.Combine(tmp, "Cache", "Mods");
 Directory.CreateDirectory(Path.Combine(tmp, "Cache"));
 var rev = new RevisionCache(tmp);
 var mods = new[] { new ModAdvert("UIMod","1.0.0","deadbeef",10, "1.0") };
 rev.Save(123, mods);
 // No cache for packs yet -> IsFresh false
 rev.IsFresh(123, mods).Should().BeFalse();
 // Create expected cache folder id@ver@sha -> IsFresh true
 Directory.CreateDirectory(Path.Combine(cacheDir, "UIMod@1.0.0@deadbeef"));
 rev.IsFresh(123, mods).Should().BeTrue();
 }
}
