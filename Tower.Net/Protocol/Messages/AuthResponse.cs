using System.Buffers.Binary;

namespace Tower.Net.Protocol.Messages;

public readonly record struct AuthResponse(int Nonce, byte[] Hmac)
{
 public byte[] Serialize()
 {
 var mac = Hmac ?? Array.Empty<byte>();
 var buf = new byte[4 +4 + mac.Length];
 BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(0,4), Nonce);
 BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4,4), mac.Length);
 mac.CopyTo(buf.AsSpan(8));
 return buf;
 }
 public static AuthResponse Deserialize(ReadOnlySpan<byte> data)
 {
 var nonce = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(0,4));
 var len = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(4,4));
 var mac = data.Slice(8, len).ToArray();
 return new AuthResponse(nonce, mac);
 }
}
