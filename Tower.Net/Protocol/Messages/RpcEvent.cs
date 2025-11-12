using System.Buffers.Binary;
using System.Text;

namespace Tower.Net.Protocol.Messages;

public readonly struct RpcEvent
{
 public string Event { get; }
 public string Payload { get; }
 public RpcEvent(string @event, string payload) { Event = @event; Payload = payload; }
 public static RpcEvent Deserialize(ReadOnlySpan<byte> data)
 {
 var eLen = BinaryPrimitives.ReadUInt16LittleEndian(data);
 var pLen = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(2));
 var ev = Encoding.UTF8.GetString(data.Slice(4, eLen));
 var pl = Encoding.UTF8.GetString(data.Slice(4 + eLen, pLen));
 return new RpcEvent(ev, pl);
 }
 public byte[] Serialize()
 {
 var e = Encoding.UTF8.GetBytes(Event ?? string.Empty);
 var p = Encoding.UTF8.GetBytes(Payload ?? string.Empty);
 var buf = new byte[4 + e.Length + p.Length];
 BinaryPrimitives.WriteUInt16LittleEndian(buf, (ushort)e.Length);
 BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(2), (ushort)p.Length);
 e.AsSpan().CopyTo(buf.AsSpan(4));
 p.AsSpan().CopyTo(buf.AsSpan(4 + e.Length));
 return buf;
 }
}
