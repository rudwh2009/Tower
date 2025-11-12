using System.Text.Json;
using Serilog;

namespace Tower.Core.Engine.Assets.Anim;

public static class AnimLoaders
{
 public static AnimDef? Parse(string json)
 {
 try
 {
 using var doc = JsonDocument.Parse(json);
 var root = doc.RootElement;
 var type = root.GetProperty("type").GetString() ?? "grid";
 if (type == "grid") return ParseGrid(root);
 if (type == "atlas") return ParseAtlas(root);
 Log.Warning("Unknown anim type {Type}", type); return null;
 }
 catch (Exception ex)
 {
 Log.Error(ex, "Anim parse failed");
 return null;
 }
 }

 private static AnimDef ParseGrid(JsonElement root)
 {
 var fw = root.GetProperty("frameWidth").GetInt32();
 var fh = root.GetProperty("frameHeight").GetInt32();
 var frames = root.GetProperty("frames").GetInt32();
 var duration = root.TryGetProperty("durationMs", out var d) ? d.GetInt32() :100;
 var meta = new List<AnimFrame>();
 for (int i =0; i < frames; i++)
 {
 meta.Add(new AnimFrame(i * fw,0, fw, fh, duration));
 }
 return new AnimDef("grid", frames, fw, fh, meta);
 }

 private static AnimDef ParseAtlas(JsonElement root)
 {
 var arr = root.GetProperty("frames");
 var meta = new List<AnimFrame>();
 foreach (var el in arr.EnumerateArray())
 {
 var r = el.GetProperty("rect");
 var dur = el.TryGetProperty("durationMs", out var d)? d.GetInt32():100;
 meta.Add(new AnimFrame(r.GetProperty("x").GetInt32(), r.GetProperty("y").GetInt32(), r.GetProperty("w").GetInt32(), r.GetProperty("h").GetInt32(), dur));
 }
 var first = meta.FirstOrDefault();
 return new AnimDef("atlas", meta.Count, first?.W ??0, first?.H ??0, meta);
 }
}
