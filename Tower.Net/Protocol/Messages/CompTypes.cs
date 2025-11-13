using System.Buffers.Binary;
using System.Text;

namespace Tower.Net.Protocol.Messages;

public readonly record struct CompComponent(string ModNs, string TypeName, string Json)
{
 public static void WriteTo(Span<byte> dst, ref int offset, CompComponent c)
 {
 WriteString(dst, ref offset, c.ModNs);
 WriteString(dst, ref offset, c.TypeName);
 WriteString(dst, ref offset, c.Json);
 }
 public static CompComponent ReadFrom(ReadOnlySpan<byte> src, ref int offset)
 {
 var ns = ReadString(src, ref offset);
 var tn = ReadString(src, ref offset);
 var js = ReadString(src, ref offset);
 return new CompComponent(ns, tn, js);
 }
 internal static void WriteString(Span<byte> dst, ref int offset, string s)
 {
 var bytes = Encoding.UTF8.GetBytes(s ?? string.Empty);
 BinaryPrimitives.WriteInt32LittleEndian(dst.Slice(offset,4), bytes.Length); offset +=4;
 bytes.CopyTo(dst.Slice(offset, bytes.Length)); offset += bytes.Length;
 }
 internal static string ReadString(ReadOnlySpan<byte> src, ref int offset)
 {
 var len = BinaryPrimitives.ReadInt32LittleEndian(src.Slice(offset,4)); offset +=4;
 var s = Encoding.UTF8.GetString(src.Slice(offset, len)); offset += len; return s;
 }
}

public readonly record struct CompEntity(int EntityId, CompComponent[] Components)
{
 public static void WriteTo(Span<byte> dst, ref int offset, CompEntity e)
 {
 BinaryPrimitives.WriteInt32LittleEndian(dst.Slice(offset,4), e.EntityId); offset +=4;
 BinaryPrimitives.WriteInt32LittleEndian(dst.Slice(offset,4), e.Components.Length); offset +=4;
 for (int i=0;i<e.Components.Length;i++) CompComponent.WriteTo(dst, ref offset, e.Components[i]);
 }
 public static CompEntity ReadFrom(ReadOnlySpan<byte> src, ref int offset)
 {
 var id = BinaryPrimitives.ReadInt32LittleEndian(src.Slice(offset,4)); offset +=4;
 var count = BinaryPrimitives.ReadInt32LittleEndian(src.Slice(offset,4)); offset +=4;
 var comps = new CompComponent[count];
 for (int i=0;i<count;i++) comps[i] = CompComponent.ReadFrom(src, ref offset);
 return new CompEntity(id, comps);
 }
}

public readonly record struct CompReplace(int EntityId, string ModNs, string TypeName, string Json)
{
 public static void WriteTo(Span<byte> dst, ref int offset, CompReplace r)
 {
 BinaryPrimitives.WriteInt32LittleEndian(dst.Slice(offset,4), r.EntityId); offset +=4;
 CompComponent.WriteString(dst, ref offset, r.ModNs);
 CompComponent.WriteString(dst, ref offset, r.TypeName);
 CompComponent.WriteString(dst, ref offset, r.Json);
 }
 public static CompReplace ReadFrom(ReadOnlySpan<byte> src, ref int offset)
 {
 var id = BinaryPrimitives.ReadInt32LittleEndian(src.Slice(offset,4)); offset +=4;
 var ns = CompComponent.ReadString(src, ref offset);
 var tn = CompComponent.ReadString(src, ref offset);
 var js = CompComponent.ReadString(src, ref offset);
 return new CompReplace(id, ns, tn, js);
 }
}

public readonly record struct CompRemove(int EntityId, string ModNs, string TypeName)
{
 public static void WriteTo(Span<byte> dst, ref int offset, CompRemove r)
 {
 BinaryPrimitives.WriteInt32LittleEndian(dst.Slice(offset,4), r.EntityId); offset +=4;
 CompComponent.WriteString(dst, ref offset, r.ModNs);
 CompComponent.WriteString(dst, ref offset, r.TypeName);
 }
 public static CompRemove ReadFrom(ReadOnlySpan<byte> src, ref int offset)
 {
 var id = BinaryPrimitives.ReadInt32LittleEndian(src.Slice(offset,4)); offset +=4;
 var ns = CompComponent.ReadString(src, ref offset);
 var tn = CompComponent.ReadString(src, ref offset);
 return new CompRemove(id, ns, tn);
 }
}
