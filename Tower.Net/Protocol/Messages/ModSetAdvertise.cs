using System.Buffers.Binary;
using System.Text;

namespace Tower.Net.Protocol.Messages;

public readonly record struct ModAdvert(string Id, string Version, string Sha256, int Size, string ApiVersion)
{
 public static byte[] SerializeList(ReadOnlySpan<ModAdvert> list, int contentRevision)
 {
 // layout: rev(int32) | count(int16) | repeated (idLen,u16 | id | verLen,u16 | ver | shaLen,u16 | sha | size,i32 | apiLen,u16 | api)
 var parts = new List<byte[]>();
 int total =4 +2;
 foreach (ref readonly var m in list)
 {
 var id = Encoding.UTF8.GetBytes(m.Id);
 var ver = Encoding.UTF8.GetBytes(m.Version);
 var sha = Encoding.UTF8.GetBytes(m.Sha256);
 var api = Encoding.UTF8.GetBytes(m.ApiVersion);
 var seg = new byte[2+id.Length +2+ver.Length +2+sha.Length +4 +2+api.Length];
 var span = seg.AsSpan();
 BinaryPrimitives.WriteUInt16LittleEndian(span, (ushort)id.Length); id.CopyTo(span.Slice(2)); span = span.Slice(2+id.Length);
 BinaryPrimitives.WriteUInt16LittleEndian(span, (ushort)ver.Length); ver.CopyTo(span.Slice(2)); span = span.Slice(2+ver.Length);
 BinaryPrimitives.WriteUInt16LittleEndian(span, (ushort)sha.Length); sha.CopyTo(span.Slice(2)); span = span.Slice(2+sha.Length);
 BinaryPrimitives.WriteInt32LittleEndian(span, m.Size); span = span.Slice(4);
 BinaryPrimitives.WriteUInt16LittleEndian(span, (ushort)api.Length); api.CopyTo(span.Slice(2));
 parts.Add(seg);
 total += seg.Length;
 }
 var buf = new byte[total];
 BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(0,4), contentRevision);
 BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(4,2), (ushort)list.Length);
 int off =6;
 foreach (var seg in parts) { seg.CopyTo(buf.AsSpan(off)); off += seg.Length; }
 return buf;
 }
}

public readonly record struct ModSetAdvertise(int ContentRevision, ModAdvert[] Mods)
{
 public byte[] Serialize()
 {
 return ModAdvert.SerializeList(Mods, ContentRevision);
 }
 public static ModSetAdvertise Deserialize(ReadOnlySpan<byte> data)
 {
 var rev = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(0,4));
 var count = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(4,2));
 var mods = new ModAdvert[count];
 var span = data.Slice(6);
 for (int i=0;i<count;i++)
 {
 var idLen = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(0,2)); span = span.Slice(2);
 var id = Encoding.UTF8.GetString(span.Slice(0,idLen)); span = span.Slice(idLen);
 var verLen = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(0,2)); span = span.Slice(2);
 var ver = Encoding.UTF8.GetString(span.Slice(0,verLen)); span = span.Slice(verLen);
 var shaLen = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(0,2)); span = span.Slice(2);
 var sha = Encoding.UTF8.GetString(span.Slice(0,shaLen)); span = span.Slice(shaLen);
 var size = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(0,4)); span = span.Slice(4);
 var apiLen = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(0,2)); span = span.Slice(2);
 var api = Encoding.UTF8.GetString(span.Slice(0,apiLen)); span = span.Slice(apiLen);
 mods[i] = new ModAdvert(id, ver, sha, size, api);
 }
 return new ModSetAdvertise(rev, mods);
 }
}
