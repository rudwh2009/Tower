namespace Tower.Core.Engine.Entities;

public sealed class EntityIndex
{
 private readonly object _gate = new();
 private readonly HashSet<IEntityProxy> _all = [];
 public void Add(IEntityProxy e) { lock (_gate) _all.Add(e); }
 public void Remove(IEntityProxy e) { lock (_gate) _all.Remove(e); }
 public IReadOnlyList<IEntityProxy> FindWithTag(string tag)
 {
 lock (_gate)
 {
 return _all.Where(e => e.HasTag(tag)).ToList();
 }
 }
 public IReadOnlyList<IEntityProxy> FindInRadius(double x, double y, double r, string? tag = null)
 {
 var r2 = r * r;
 lock (_gate)
 {
 return _all.Where(e => (tag is null || e.HasTag(tag)) && Dist2(e.X, e.Y, x, y) <= r2).ToList();
 }
 }
 private static double Dist2(double ax, double ay, double bx, double by)
 {
 var dx = ax - bx; var dy = ay - by; return dx * dx + dy * dy;
 }
}
