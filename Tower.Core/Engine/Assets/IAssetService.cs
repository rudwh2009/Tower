namespace Tower.Core.Engine.Assets;

public interface IAssetService
{
 void RegisterFromManifest(string modId, string rootDir, string manifestPath);
 bool TryGet(string logicalId, out object? asset);
 // Particle effect logical registration
 bool TryGetParticle(string logicalId, out object? particleDef);
 // Sound registration and lookup (object-based to avoid MonoGame ref in Core)
 void RegisterSound(string logicalId, Func<object> loader);
 bool TryGetSound(string logicalId, out object? sound);
 object GetSound(string logicalId);
}
