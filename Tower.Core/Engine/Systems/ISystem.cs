namespace Tower.Core.Engine.Systems;

public interface ISystem
{
 string Name { get; }
 int Order { get; }
 void Update(double dtSeconds);
}
