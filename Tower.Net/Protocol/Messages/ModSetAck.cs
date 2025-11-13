using System.Buffers.Binary;
using System.Text;

namespace Tower.Net.Protocol.Messages;

public readonly record struct ModNeed(string Id, string Version, string Sha256)
{
 public static byte[] SerializeList(ReadOnlySpan<ModNeed> list)
 {
 // layout: count(u16) | repeated (idLen,u16 | id | verLen,u16 | ver | shaLen,u16 | sha)
 var parts = new List<byte[]>(); int total =2;
 foreach (ref readonly var m in list)
 {
 var id = Encoding.UTF8.GetBytes(m.Id);
 var ver = Encoding.UTF8.GetBytes(m.Version);
 var sha = Encoding.UTF8.GetBytes(m.Sha256);
 var seg = new byte[2+id.Length +2+ver.Length +2+sha.Length]; var span = seg.AsSpan();
 BinaryPrimitives.WriteUInt16LittleEndian(span,(ushort)id.Length); id.CopyTo(span.Slice(2)); span=span.Slice(2+id.Length);
 BinaryPrimitives.WriteUInt16LittleEndian(span,(ushort)ver.Length); ver.CopyTo(span.Slice(2)); span=span.Slice(2+ver.Length);
 BinaryPrimitives.WriteUInt16LittleEndian(span,(ushort)sha.Length); sha.CopyTo(span.Slice(2));
 parts.Add(seg); total += seg.Length;
 }
 var buf = new byte[total]; BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(0,2),(ushort)list.Length);
 int off=2; foreach (var seg in parts) { seg.CopyTo(buf.AsSpan(off)); off += seg.Length; }
 return buf;
 }
}

public readonly record struct ModSetAck(ModNeed[] Need)
{
 public byte[] Serialize() => ModNeed.SerializeList(Need);
 public static ModSetAck Deserialize(ReadOnlySpan<byte> data)
 {
 var count = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(0,2));
 var list = new ModNeed[count]; var span = data.Slice(2);
 for (int i=0;i<count;i++)
 {
 var idLen = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(0,2)); span = span.Slice(2);
 var id = Encoding.UTF8.GetString(span.Slice(0,idLen)); span = span.Slice(idLen);
 var verLen = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(0,2)); span = span.Slice(2);
 var ver = Encoding.UTF8.GetString(span.Slice(0,verLen)); span = span.Slice(verLen);
 var shaLen = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(0,2)); span = span.Slice(2);
 var sha = Encoding.UTF8.GetString(span.Slice(0,shaLen)); span = span.Slice(shaLen);
 list[i] = new ModNeed(id, ver, sha);
 }
 return new ModSetAck(list);
 }
}
