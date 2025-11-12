// Licensed under the MIT License.

using Serilog;

namespace Tower.Core.Scripting.GameApi;

/// <summary>
/// Asset registration and lookup wrappers.
/// </summary>
public sealed partial class GameApi
{
 /// <summary>Registers a texture path for the given id.</summary>
 /// <param name="id">Logical texture id (without prefix).</param>
 /// <param name="path">Relative asset path.</param>
 public void RegisterTexture(string id, string path) => this.Register("texture/" + id, path);

 /// <summary>Registers a sound path for the given id.</summary>
 /// <param name="id">Logical sound id.</param>
 /// <param name="path">Relative asset path.</param>
 public void RegisterSound(string id, string path) => this.Register("sound/" + id, path);

 /// <summary>Registers an animation metadata path for the given id.</summary>
 /// <param name="id">Logical anim id.</param>
 /// <param name="path">Relative asset path.</param>
 public void RegisterAnim(string id, string path) => this.Register("anim/" + id, path);

 /// <summary>Registers a font path for the given id.</summary>
 /// <param name="id">Logical font id.</param>
 /// <param name="path">Relative asset path.</param>
 public void RegisterFont(string id, string path) => this.Register("font/" + id, path);

 /// <summary>Registers a shader path for the given id.</summary>
 /// <param name="id">Logical shader id.</param>
 /// <param name="path">Relative asset path.</param>
 public void RegisterShader(string id, string path) => this.Register("shader/" + id, path);

 /// <summary>Gets a texture by logical id.</summary>
 /// <param name="logicalId">Full logical id.</param>
 /// <returns>Texture descriptor object or null.</returns>
 public object? GetTexture(string logicalId) => this.Get("texture/" + logicalId);

 /// <summary>Gets a sound by logical id.</summary>
 /// <param name="logicalId">Full logical id.</param>
 /// <returns>Sound descriptor object or null.</returns>
 public object? GetSound(string logicalId) => this.Get("sound/" + logicalId);

 /// <summary>Gets an animation definition by logical id.</summary>
 /// <param name="logicalId">Full logical id.</param>
 /// <returns>Animation definition or null.</returns>
 public object? GetAnim(string logicalId) => this.Get("anim/" + logicalId);

 private void Register(string id, string path)
 {
 Log.Information("Register asset {Id} => {Path}", id, path);
 }

 private object? Get(string logicalId)
 {
 this.assets.TryGet(logicalId, out var asset);
 return asset;
 }
}
