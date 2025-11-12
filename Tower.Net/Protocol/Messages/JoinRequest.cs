using System.Buffers.Binary;
using System.Text;

namespace Tower.Net.Protocol.Messages;

public readonly struct JoinRequest
{
 public string Name { get; }
 public JoinRequest(string name) => Name = name;
 public static JoinRequest Deserialize(ReadOnlySpan<byte> data)
 {
 var len = BinaryPrimitives.ReadUInt16LittleEndian(data);
 var name = Encoding.UTF8.GetString(data.Slice(2, len));
 return new JoinRequest(name);
 }
 public byte[] Serialize()
 {
 var nameBytes = Encoding.UTF8.GetBytes(Name ?? string.Empty);
 var buf = new byte[2 + nameBytes.Length];
 BinaryPrimitives.WriteUInt16LittleEndian(buf, (ushort)nameBytes.Length);
 nameBytes.AsSpan().CopyTo(buf.AsSpan(2));
 return buf;
 }
}
