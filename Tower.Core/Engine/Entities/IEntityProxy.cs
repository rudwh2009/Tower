using MoonSharp.Interpreter;

namespace Tower.Core.Engine.Entities;

public interface IEntityProxy
{
 int Id { get; }
 double X { get; set; }
 double Y { get; set; }
 void AddTag(string tag);
 void SetStat(string name, double value);
 double GetStat(string name);
}
