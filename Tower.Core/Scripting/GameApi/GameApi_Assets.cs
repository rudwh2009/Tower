// Licensed under the MIT License.

using MoonSharp.Interpreter;
using Serilog;
using Tower.Core.Engine.Assets;
using Tower.Core.Engine.Assets.Particles;

namespace Tower.Core.Scripting.GameApi;

/// <summary>
/// Asset registration and lookup wrappers (manifest-backed, handle-based).
/// </summary>
public sealed partial class GameApi
{
 /// <summary>Registers assets from a manifest for a given mod id and root.</summary>
 public void RegisterAssets(string modId, string root, string manifestPath)
 {
 if (string.IsNullOrWhiteSpace(modId)) throw new ScriptRuntimeException("modId required");
 if (string.IsNullOrWhiteSpace(root)) throw new ScriptRuntimeException("root required");
 if (string.IsNullOrWhiteSpace(manifestPath)) throw new ScriptRuntimeException("manifestPath required");
 this.assets.RegisterFromManifest(modId, root, manifestPath);
 }

 public object GetTexture(string logicalId)
 {
 if (!this.assets.TryGet(logicalId, out var obj) || obj is not TextureHandle th)
 throw new ScriptRuntimeException($"texture not found: {logicalId}");
 return th;
 }

 public object GetSound(string logicalId)
 {
 var s = this.assets.GetSound(logicalId);
 return s;
 }

 public object GetAnim(string logicalId)
 {
 if (!this.assets.TryGet(logicalId, out var obj) || obj is not string json)
 throw new ScriptRuntimeException($"anim not found: {logicalId}");
 var def = Tower.Core.Engine.Assets.Anim.AnimLoaders.Parse(json);
 if (def is null) throw new ScriptRuntimeException($"anim parse failed: {logicalId}");
 return def;
 }

 public object GetParticle(string logicalId)
 {
 if (!this.assets.TryGetParticle(logicalId, out var obj) || obj is not ParticleEffectDef p)
 throw new ScriptRuntimeException($"particle not found: {logicalId}");
 return p;
 }

 public object GetFont(string logicalId)
 {
 if (!this.assets.TryGet(logicalId, out var obj) || obj is not FontSpec f)
 throw new ScriptRuntimeException($"font not found: {logicalId}");
 return f;
 }
}
