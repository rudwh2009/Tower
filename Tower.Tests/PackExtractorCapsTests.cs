using FluentAssertions;
using Tower.Core.Modding;
using System.IO.Compression;
using Xunit;

public class PackExtractorCapsTests
{
 private static byte[] MakeZip(params (string path, int bytes)[] files)
 {
 using var ms = new MemoryStream();
 using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen:true))
 {
 foreach (var (p, n) in files)
 {
 var e = zip.CreateEntry(p);
 using var s = e.Open();
 s.Write(new byte[n],0,n);
 }
 }
 return ms.ToArray();
 }

 [Fact]
 public void Rejects_Too_Many_Files()
 {
 var files = new (string,int)[4001];
 for (int i=0;i<files.Length;i++) files[i] = ($"f{i}",1);
 var zip = MakeZip(files);
 var act = () => PackExtractor.ExtractToCache(Path.GetTempPath(), "M","1","sha", zip);
 act.Should().Throw<InvalidDataException>();
 }

 [Fact]
 public void Rejects_Too_Large_Total()
 {
 var zip = MakeZip(("big.bin", (int)(600L*1024*1024))); //600MB
 var act = () => PackExtractor.ExtractToCache(Path.GetTempPath(), "M","1","sha", zip);
 act.Should().Throw<InvalidDataException>();
 }

 [Fact]
 public void Rejects_Too_Large_Entry()
 {
 var zip = MakeZip(("big2.bin", (int)(70L*1024*1024))); //70MB
 var act = () => PackExtractor.ExtractToCache(Path.GetTempPath(), "M","1","sha", zip);
 act.Should().Throw<InvalidDataException>();
 }
}
