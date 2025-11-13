using System.Buffers.Binary;

namespace Tower.Net.Protocol.Messages;

public readonly record struct AuthChallenge(int Nonce, byte[] Token)
{
 public byte[] Serialize()
 {
 var tok = Token ?? Array.Empty<byte>();
 var buf = new byte[4 +4 + tok.Length];
 BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(0,4), Nonce);
 BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4,4), tok.Length);
 tok.CopyTo(buf.AsSpan(8));
 return buf;
 }
 public static AuthChallenge Deserialize(ReadOnlySpan<byte> data)
 {
 var nonce = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(0,4));
 var len = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(4,4));
 var tok = data.Slice(8, len).ToArray();
 return new AuthChallenge(nonce, tok);
 }
}
