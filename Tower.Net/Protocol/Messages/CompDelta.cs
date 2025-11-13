using System.Buffers.Binary;

namespace Tower.Net.Protocol.Messages;

public sealed class CompDelta
{
 public CompReplace[] Replacements { get; }
 public CompRemove[] Removes { get; }
 public CompDelta(CompReplace[] reps, CompRemove[] rems) { Replacements = reps; Removes = rems; }
 public byte[] Serialize()
 {
 int size =4 +4; // counts
 foreach (var r in Replacements)
 {
 size +=4; // id
 size +=4 + r.ModNs.Length; size +=4 + r.TypeName.Length; size +=4 + r.Json.Length;
 }
 foreach (var rm in Removes)
 {
 size +=4; size +=4 + rm.ModNs.Length; size +=4 + rm.TypeName.Length;
 }
 var buf = new byte[size]; int off =0;
 BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(off,4), Replacements.Length); off +=4;
 BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(off,4), Removes.Length); off +=4;
 for (int i=0;i<Replacements.Length;i++) CompReplace.WriteTo(buf, ref off, Replacements[i]);
 for (int i=0;i<Removes.Length;i++) CompRemove.WriteTo(buf, ref off, Removes[i]);
 return buf;
 }
 public static CompDelta Deserialize(ReadOnlySpan<byte> data)
 {
 int off =0; var rc = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(off,4)); off +=4; var dc = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(off,4)); off +=4;
 var reps = new CompReplace[rc]; var rems = new CompRemove[dc];
 for (int i=0;i<rc;i++) reps[i] = CompReplace.ReadFrom(data, ref off);
 for (int i=0;i<dc;i++) rems[i] = CompRemove.ReadFrom(data, ref off);
 return new CompDelta(reps, rems);
 }
}
