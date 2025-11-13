using System.Buffers.Binary;

namespace Tower.Net.Protocol.Messages;

public sealed class CompBaseline
{
 public CompEntity[] Entities { get; }
 public CompBaseline(CompEntity[] entities) { Entities = entities; }
 public byte[] Serialize()
 {
 int size =4; // entity count
 foreach (var e in Entities)
 {
 size +=4 +4; // id + comp count
 foreach (var c in e.Components)
 {
 size +=4 + c.ModNs.Length +4 + c.TypeName.Length +4 + c.Json.Length; // length-prefixed utf8
 }
 }
 var buf = new byte[size]; int off =0;
 BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(off,4), Entities.Length); off +=4;
 foreach (var e in Entities)
 {
 CompEntity.WriteTo(buf, ref off, e);
 }
 return buf;
 }
 public static CompBaseline Deserialize(ReadOnlySpan<byte> data)
 {
 int off =0; var count = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(off,4)); off +=4;
 var ents = new CompEntity[count];
 for (int i=0;i<count;i++) ents[i] = CompEntity.ReadFrom(data, ref off);
 return new CompBaseline(ents);
 }
}
