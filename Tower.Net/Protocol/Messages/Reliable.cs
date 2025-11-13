using System.Buffers.Binary;

namespace Tower.Net.Protocol.Messages;

public sealed class Reliable
{
 public uint Seq { get; }
 public byte InnerId { get; }
 public byte[] Payload { get; }
 public Reliable(uint seq, byte innerId, byte[] payload) { Seq = seq; InnerId = innerId; Payload = payload; }
 public byte[] Serialize()
 {
 var buf = new byte[4 +1 +4 + Payload.Length];
 BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0,4), Seq);
 buf[4] = InnerId;
 BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(5,4), Payload.Length);
 Payload.CopyTo(buf.AsSpan(9));
 return buf;
 }
 public static Reliable Deserialize(ReadOnlySpan<byte> data)
 {
 var seq = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(0,4));
 var id = data[4];
 var len = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(5,4));
 var payload = data.Slice(9, len).ToArray();
 return new Reliable(seq, id, payload);
 }
}

public sealed class ReliableAck
{
 public uint AckSeq { get; }
 public ReliableAck(uint ackSeq) { AckSeq = ackSeq; }
 public byte[] Serialize()
 {
 var buf = new byte[4]; BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0,4), AckSeq); return buf;
 }
 public static ReliableAck Deserialize(ReadOnlySpan<byte> data)
 {
 var v = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(0,4)); return new ReliableAck(v);
 }
}
