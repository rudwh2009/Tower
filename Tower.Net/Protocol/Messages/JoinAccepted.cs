using System.Buffers.Binary;

namespace Tower.Net.Protocol.Messages;

public readonly struct JoinAccepted
{
 public int ClientId { get; }
 public JoinAccepted(int clientId) => ClientId = clientId;
 public static JoinAccepted Deserialize(ReadOnlySpan<byte> data) => new(BinaryPrimitives.ReadInt32LittleEndian(data));
 public byte[] Serialize()
 {
 var buf = new byte[4];
 BinaryPrimitives.WriteInt32LittleEndian(buf, ClientId);
 return buf;
 }
}
