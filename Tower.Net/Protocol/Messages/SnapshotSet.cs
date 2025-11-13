using System.Buffers.Binary;

namespace Tower.Net.Protocol.Messages;

public readonly record struct EntityState(int Id, float X, float Y);

public sealed class SnapshotSet
{
 public int Tick { get; }
 public EntityState[] Entities { get; }
 public int LastProcessedInputTick { get; }
 public SnapshotSet(int tick, int lastProcessedInputTick, EntityState[] entities)
 { Tick = tick; LastProcessedInputTick = lastProcessedInputTick; Entities = entities; }
 public byte[] Serialize()
 {
 var count = Entities?.Length ??0;
 var buf = new byte[4 +4 +4 + count * (4 +4 +4)];
 BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(0,4), Tick);
 BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4,4), LastProcessedInputTick);
 BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(8,4), count);
 int offset =12;
 for (int i=0;i<count;i++)
 {
 BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(offset,4), Entities[i].Id); offset +=4;
 BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(offset,4), Entities[i].X); offset +=4;
 BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(offset,4), Entities[i].Y); offset +=4;
 }
 return buf;
 }
 public static SnapshotSet Deserialize(ReadOnlySpan<byte> data)
 {
 var tick = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(0,4));
 var last = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(4,4));
 var count = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(8,4));
 var list = new EntityState[count];
 int offset =12;
 for (int i=0;i<count;i++)
 {
 var id = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(offset,4)); offset +=4;
 var x = BinaryPrimitives.ReadSingleLittleEndian(data.Slice(offset,4)); offset +=4;
 var y = BinaryPrimitives.ReadSingleLittleEndian(data.Slice(offset,4)); offset +=4;
 list[i] = new EntityState(id, x, y);
 }
 return new SnapshotSet(tick, last, list);
 }
}
