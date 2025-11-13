namespace Tower.Net.Protocol.Messages;

public readonly record struct SnapshotAck(int Tick, uint Hash)
{
 public byte[] Serialize()
 {
 var buf = new byte[8];
 System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(0,4), Tick);
 System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(4,4), Hash);
 return buf;
 }
 public static SnapshotAck Deserialize(System.ReadOnlySpan<byte> src)
 {
 var tick = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(src.Slice(0,4));
 var hash = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(src.Slice(4,4));
 return new SnapshotAck(tick, hash);
 }
}
