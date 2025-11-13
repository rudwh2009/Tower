using System.Buffers.Binary;

namespace Tower.Net.Protocol.Messages;

public readonly record struct StartLoading(int Seed, int TickRate, string WorldId)
{
 public byte[] Serialize()
 {
 var wid = System.Text.Encoding.UTF8.GetBytes(WorldId ?? string.Empty);
 var buf = new byte[4+4 +2+wid.Length];
 BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(0,4), Seed);
 BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4,4), TickRate);
 BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(8,2), (ushort)wid.Length);
 wid.CopyTo(buf.AsSpan(10));
 return buf;
 }
 public static StartLoading Deserialize(ReadOnlySpan<byte> data)
 {
 var seed = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(0,4));
 var tr = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(4,4));
 var wl = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(8,2));
 var wid = System.Text.Encoding.UTF8.GetString(data.Slice(10,wl));
 return new StartLoading(seed, tr, wid);
 }
}
