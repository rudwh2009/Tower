using System.Buffers.Binary;

namespace Tower.Net.Protocol.Messages;

public readonly record struct ModChunk(string Id, string Sha256, ushort Seq, ushort Total, byte[] Bytes)
{
 public byte[] Serialize()
 {
 var id = System.Text.Encoding.UTF8.GetBytes(Id);
 var sha = System.Text.Encoding.UTF8.GetBytes(Sha256);
 var buf = new byte[2+id.Length +2+sha.Length +2+2 +4 + Bytes.Length];
 var span = buf.AsSpan();
 BinaryPrimitives.WriteUInt16LittleEndian(span,(ushort)id.Length); System.Text.Encoding.UTF8.GetBytes(Id).CopyTo(span.Slice(2)); span=span.Slice(2+id.Length);
 BinaryPrimitives.WriteUInt16LittleEndian(span,(ushort)sha.Length); System.Text.Encoding.UTF8.GetBytes(Sha256).CopyTo(span.Slice(2)); span=span.Slice(2+sha.Length);
 BinaryPrimitives.WriteUInt16LittleEndian(span, Seq); span=span.Slice(2);
 BinaryPrimitives.WriteUInt16LittleEndian(span, Total); span=span.Slice(2);
 BinaryPrimitives.WriteInt32LittleEndian(span, Bytes.Length); span=span.Slice(4);
 Bytes.CopyTo(span);
 return buf;
 }
 public static ModChunk Deserialize(ReadOnlySpan<byte> data)
 {
 var span = data;
 var idLen = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(0,2)); span = span.Slice(2);
 var id = System.Text.Encoding.UTF8.GetString(span.Slice(0,idLen)); span = span.Slice(idLen);
 var shaLen = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(0,2)); span = span.Slice(2);
 var sha = System.Text.Encoding.UTF8.GetString(span.Slice(0,shaLen)); span = span.Slice(shaLen);
 var seq = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(0,2)); span=span.Slice(2);
 var tot = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(0,2)); span=span.Slice(2);
 var blen = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(0,4)); span=span.Slice(4);
 var bytes = span.Slice(0,blen).ToArray();
 return new ModChunk(id, sha, seq, tot, bytes);
 }
}
