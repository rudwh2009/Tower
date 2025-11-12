using System.Text.Json;
using Tower.Core.Scripting.GameApi;

namespace Tower.Core.Engine.Save;

public sealed class SaveSystem
{
 private const int CurrentVersion =1;
 private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General)
 {
 WriteIndented = false
 };

 public sealed class SaveFile
 {
 public int saveVersion { get; set; } = CurrentVersion;
 public long worldSeed { get; set; }
 public long tick { get; set; }
 public Dictionary<string, object?> modState { get; set; } = new(StringComparer.Ordinal);
 }

 public sealed class LoadResult
 {
 public int saveVersion { get; init; }
 public long worldSeed { get; init; }
 public long tick { get; init; }
 }

 public void Save(GameApi api, string path, long worldSeed, long tick)
 {
 var state = api.CollectSaveState();
 var file = new SaveFile { worldSeed = worldSeed, tick = tick, modState = state };
 var dir = Path.GetDirectoryName(path);
 if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
 var json = JsonSerializer.Serialize(file, Options);
 File.WriteAllText(path, json);
 }

 public LoadResult Load(GameApi api, string path)
 {
 var json = File.ReadAllText(path);
 var file = JsonSerializer.Deserialize<SaveFile>(json, Options) ?? new SaveFile();
 api.ApplyLoadState(file.modState);
 return new LoadResult { saveVersion = file.saveVersion, worldSeed = file.worldSeed, tick = file.tick };
 }
}
