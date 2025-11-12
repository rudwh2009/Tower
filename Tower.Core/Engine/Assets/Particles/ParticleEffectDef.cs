using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tower.Core.Engine.Assets.Particles;

public enum ParticleSourceKind { Programmatic, Ember }

public sealed class ParticleEffectDef
{
 public string? TextureId { get; set; }
 public bool Looped { get; set; }
 public float Duration { get; set; }
 public string? SourcePath { get; set; }
 public ParticleSourceKind SourceKind { get; set; } = ParticleSourceKind.Programmatic;
 public List<EmitterDef> Emitters { get; } = new();
 public List<ModifierDef> Modifiers { get; } = new();

 public static ParticleEffectDef FromJson(string json, string? sourcePath = null)
 {
 var model = JsonSerializer.Deserialize<ParticleEffectDef>(json, new JsonSerializerOptions
 {
 PropertyNameCaseInsensitive = true,
 ReadCommentHandling = JsonCommentHandling.Skip,
 AllowTrailingCommas = true,
 }) ?? new ParticleEffectDef();
 model.SourcePath = sourcePath;
 model.SourceKind = ParticleSourceKind.Programmatic;
 return model;
 }
}

public sealed class EmitterDef
{
 public int Capacity { get; set; }
 public float LifeSpan { get; set; }
 public ProfileDef Profile { get; set; } = new();
 public ReleaseParametersDef Release { get; set; } = new();
}

public sealed class ProfileDef
{
 public string Type { get; set; } = "Spray"; // Spray, Circle, Line
 public Vector2 Direction { get; set; } = new(0, -1); // for Spray
 public float Spread { get; set; } =0f; // for Spray
 public float Radius { get; set; } =0f; // for Circle
 public float Length { get; set; } =0f; // for Line
 public string Radiation { get; set; } = "None"; // Out/None for Circle; In/Out/None for Line
}

public sealed class ReleaseParametersDef
{
 public IntRange? Quantity { get; set; }
 public FloatRange? Speed { get; set; }
 public Vector2? Scale { get; set; }
 public Vector3? ColorHsl { get; set; }
}

public sealed class ModifierDef
{
 public string Type { get; set; } = string.Empty; // LinearGravity, Drag, Age
 public Vector2? Direction { get; set; }
 public float? Strength { get; set; }
 public float? Density { get; set; }
 public float? DragCoefficient { get; set; }
 public List<InterpolatorDef> Interpolators { get; set; } = new();
}

public sealed class InterpolatorDef
{
 public string Type { get; set; } = string.Empty; // Opacity, Hue
 public float Start { get; set; }
 public float End { get; set; }
}

public sealed class IntRange { public int Min { get; set; } public int Max { get; set; } }
public sealed class FloatRange { public float Min { get; set; } public float Max { get; set; } }
