using MoonSharp.Interpreter;

namespace Tower.Core.Engine.Entities;

public interface IEntityProxy
{
 int Id { get; }
 double X { get; set; }
 double Y { get; set; }
 double VX { get; set; }
 double VY { get; set; }
 void AddTag(string tag);
 void RemoveTag(string tag);
 bool HasTag(string tag);
 void SetStat(string name, double value);
 double GetStat(string name);
 void SetString(string name, string value);
 string? GetString(string name);
 void SetBool(string name, bool value);
 bool GetBool(string name);
 double DistanceTo(double x, double y);
 void Destroy();
}
