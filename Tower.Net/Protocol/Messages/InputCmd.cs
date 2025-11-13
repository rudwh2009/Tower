using System.Buffers.Binary;

namespace Tower.Net.Protocol.Messages;

public readonly record struct InputCmd(int ClientId, int Tick, string Action)
{
 public byte[] Serialize()
 {
 var actionBytes = System.Text.Encoding.UTF8.GetBytes(Action ?? string.Empty);
 var buf = new byte[4+4+2+actionBytes.Length];
 BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(0,4), ClientId);
 BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4,4), Tick);
 BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(8,2), (ushort)actionBytes.Length);
 actionBytes.CopyTo(buf.AsSpan(10));
 return buf;
 }
 public static InputCmd Deserialize(ReadOnlySpan<byte> data)
 {
 var cid = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(0,4));
 var tick = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(4,4));
 var len = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(8,2));
 var action = System.Text.Encoding.UTF8.GetString(data.Slice(10,len));
 return new InputCmd(cid, tick, action);
 }
}
