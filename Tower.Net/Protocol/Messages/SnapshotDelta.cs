using System.Buffers.Binary;

namespace Tower.Net.Protocol.Messages;

public sealed class SnapshotDelta
{
 public int BaselineId { get; }
 public EntityState[] Replacements { get; }
 public int[] Removes { get; }
 public SnapshotDelta(int baselineId, EntityState[] replacements, int[] removes)
 { BaselineId = baselineId; Replacements = replacements; Removes = removes; }
 public byte[] Serialize()
 {
 var rep = Replacements ?? Array.Empty<EntityState>();
 var rem = Removes ?? Array.Empty<int>();
 var buf = new byte[4 +4 + rep.Length*(4+4+4) +4 + rem.Length*4];
 int ofs =0;
 BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(ofs,4), BaselineId); ofs +=4;
 BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(ofs,4), rep.Length); ofs +=4;
 for (int i=0;i<rep.Length;i++)
 {
 BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(ofs,4), rep[i].Id); ofs +=4;
 BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(ofs,4), rep[i].X); ofs +=4;
 BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(ofs,4), rep[i].Y); ofs +=4;
 }
 BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(ofs,4), rem.Length); ofs +=4;
 for (int i=0;i<rem.Length;i++) { BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(ofs,4), rem[i]); ofs +=4; }
 return buf;
 }
 public static SnapshotDelta Deserialize(ReadOnlySpan<byte> data)
 {
 int ofs =0;
 var bid = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(ofs,4)); ofs +=4;
 var repCount = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(ofs,4)); ofs +=4;
 var rep = new EntityState[repCount];
 for (int i=0;i<repCount;i++)
 {
 var id = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(ofs,4)); ofs +=4;
 var x = BinaryPrimitives.ReadSingleLittleEndian(data.Slice(ofs,4)); ofs +=4;
 var y = BinaryPrimitives.ReadSingleLittleEndian(data.Slice(ofs,4)); ofs +=4;
 rep[i] = new EntityState(id, x, y);
 }
 var remCount = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(ofs,4)); ofs +=4;
 var rem = new int[remCount];
 for (int i=0;i<remCount;i++) { rem[i] = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(ofs,4)); ofs +=4; }
 return new SnapshotDelta(bid, rep, rem);
 }
}
