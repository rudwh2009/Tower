using System.Buffers.Binary;

namespace Tower.Net.Protocol.Messages;

public sealed class SnapshotBaseline
{
 public int BaselineId { get; }
 public EntityState[] Entities { get; }
 public SnapshotBaseline(int baselineId, EntityState[] entities)
 { BaselineId = baselineId; Entities = entities; }
 public byte[] Serialize()
 {
 var count = Entities?.Length ??0;
 var buf = new byte[4 +4 + count * (4 +4 +4)];
 BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(0,4), BaselineId);
 BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4,4), count);
 int offset =8;
 for (int i=0;i<count;i++)
 {
 BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(offset,4), Entities[i].Id); offset +=4;
 BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(offset,4), Entities[i].X); offset +=4;
 BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(offset,4), Entities[i].Y); offset +=4;
 }
 return buf;
 }
 public static SnapshotBaseline Deserialize(ReadOnlySpan<byte> data)
 {
 var bid = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(0,4));
 var count = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(4,4));
 var list = new EntityState[count];
 int offset =8;
 for (int i=0;i<count;i++)
 {
 var id = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(offset,4)); offset +=4;
 var x = BinaryPrimitives.ReadSingleLittleEndian(data.Slice(offset,4)); offset +=4;
 var y = BinaryPrimitives.ReadSingleLittleEndian(data.Slice(offset,4)); offset +=4;
 list[i] = new EntityState(id, x, y);
 }
 return new SnapshotBaseline(bid, list);
 }
}
