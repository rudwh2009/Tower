using MoonSharp.Interpreter;

namespace Tower.Core.Scripting.GameApi;

public sealed partial class GameApi
{
 private Random _rng = new Random(12345);
 /// <summary>Sets the deterministic RNG seed. Server-only.</summary>
 public void SetRandomSeed(int seed)
 {
 sideGate.EnsureServer("Random.Seed");
 _rng = new Random(seed);
 }
 /// <summary>Returns a double in [0,1). Server-only.</summary>
 public double Random()
 {
 sideGate.EnsureServer("Random.Next");
 return _rng.NextDouble();
 }
 /// <summary>Returns an integer in [min, max). Server-only.</summary>
 public int RandomRange(int min, int max)
 {
 sideGate.EnsureServer("Random.Range");
 return _rng.Next(min, max);
 }
 /// <summary>Returns true with probability p in [0,1]. Server-only.</summary>
 public bool RandomChance(double p)
 {
 sideGate.EnsureServer("Random.Chance");
 if (p <=0) return false; if (p >=1) return true;
 return _rng.NextDouble() < p;
 }
}
