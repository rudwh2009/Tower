namespace Tower.Net.Protocol.Messages;

public readonly record struct ModDone(string Id, string Sha256)
{
 public byte[] Serialize()
 {
 var id = System.Text.Encoding.UTF8.GetBytes(Id);
 var sha = System.Text.Encoding.UTF8.GetBytes(Sha256);
 var buf = new byte[2+id.Length +2+sha.Length];
 System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(0,2),(ushort)id.Length);
 id.CopyTo(buf.AsSpan(2));
 var off =2+id.Length;
 System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(off,2),(ushort)sha.Length);
 sha.CopyTo(buf.AsSpan(off+2));
 return buf;
 }
 public static ModDone Deserialize(ReadOnlySpan<byte> data)
 {
 var idLen = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(0,2));
 var id = System.Text.Encoding.UTF8.GetString(data.Slice(2,idLen));
 var shaLen = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(2+idLen,2));
 var sha = System.Text.Encoding.UTF8.GetString(data.Slice(4+idLen,shaLen));
 return new ModDone(id, sha);
 }
}
