using System.Buffers.Binary;
using System.IO.Compression;

namespace Tower.Net.Protocol.Messages;

public sealed class Compressed
{
 public byte InnerId { get; }
 public byte[] Payload { get; }
 public Compressed(byte innerId, byte[] payload)
 { InnerId = innerId; Payload = payload; }
 public byte[] Serialize()
 {
 var comp = Compress(Payload);
 var buf = new byte[1 +4 + comp.Length];
 buf[0] = InnerId;
 BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(1,4), comp.Length);
 comp.CopyTo(buf.AsSpan(5));
 return buf;
 }
 public static Compressed Deserialize(ReadOnlySpan<byte> data)
 {
 var inner = data[0];
 var len = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(1,4));
 var comp = data.Slice(5, len).ToArray();
 var payload = Decompress(comp);
 return new Compressed(inner, payload);
 }
 private static byte[] Compress(byte[] data)
 {
 using var ms = new MemoryStream();
 using (var ds = new DeflateStream(ms, CompressionLevel.Fastest, leaveOpen:true))
 { ds.Write(data,0, data.Length); }
 return ms.ToArray();
 }
 private static byte[] Decompress(byte[] data)
 {
 using var input = new MemoryStream(data);
 using var ds = new DeflateStream(input, CompressionMode.Decompress);
 using var outMs = new MemoryStream();
 ds.CopyTo(outMs);
 return outMs.ToArray();
 }
}
