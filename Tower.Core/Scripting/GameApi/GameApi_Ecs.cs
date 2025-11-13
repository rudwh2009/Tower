using MoonSharp.Interpreter;
using Tower.Net.Session;

namespace Tower.Core.Scripting.GameApi;

public sealed partial class GameApi
{
 private IEntityRegistry? _entities;
 public void SetEntityRegistry(IEntityRegistry registry) { _entities = registry; }
 public int CreateEntity()
 {
 sideGate.EnsureServer("ECS.CreateEntity");
 return _entities is null ?0 : _entities.CreateEntity();
 }
 public void RegisterComponent(string modNs, string typeName)
 {
 sideGate.EnsureServer("ECS.RegisterComponent");
 if (string.IsNullOrWhiteSpace(modNs) || string.IsNullOrWhiteSpace(typeName)) throw new ScriptRuntimeException("invalid component id");
 _entities?.RegisterComponent(modNs, typeName);
 }
 public void SetComponentData(double entityId, string modNs, string typeName, string jsonUtf8)
 {
 sideGate.EnsureServer("ECS.SetComponentData");
 var id = (int)entityId; if (id<=0) throw new ScriptRuntimeException("invalid entity id");
 if (jsonUtf8 is null) throw new ScriptRuntimeException("data required");
 _entities?.SetComponentData(id, modNs, typeName, jsonUtf8);
 }
}
