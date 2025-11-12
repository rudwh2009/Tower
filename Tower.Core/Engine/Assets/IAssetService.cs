namespace Tower.Core.Engine.Assets;

public interface IAssetService
{
 void RegisterFromManifest(string modId, string rootDir, string manifestPath);
 bool TryGet(string logicalId, out object? asset);
 // Particle effect logical registration
 bool TryGetParticle(string logicalId, out object? particleDef);
}
