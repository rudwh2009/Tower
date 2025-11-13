using System.Buffers.Binary;

namespace Tower.Net.Protocol.Messages;

public readonly record struct Snapshot(int Tick, int EntityId, float X, float Y)
{
 public byte[] Serialize()
 {
 var buf = new byte[4+4+4+4];
 BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(0,4), Tick);
 BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4,4), EntityId);
 BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(8,4), X);
 BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(12,4), Y);
 return buf;
 }
 public static Snapshot Deserialize(ReadOnlySpan<byte> data)
 {
 var tick = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(0,4));
 var eid = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(4,4));
 var x = BinaryPrimitives.ReadSingleLittleEndian(data.Slice(8,4));
 var y = BinaryPrimitives.ReadSingleLittleEndian(data.Slice(12,4));
 return new Snapshot(tick, eid, x, y);
 }
}
