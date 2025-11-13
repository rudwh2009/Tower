// Copyright (c) Tower.
// Licensed under the MIT License.

using MoonSharp.Interpreter;
using Serilog;
using Tower.Core.Engine.Prefabs;
using Tower.Core.Engine.Entities;
using Tower.Core.Engine.Entities;

namespace Tower.Core.Scripting.GameApi;

/// <summary>
/// Prefab registration and spawning, Lua-visible.
/// </summary>
public sealed partial class GameApi
{
 private readonly EntityIndex _entityIndex = new();
 /// <summary>Registers a prefab factory closure under the given id. Server-only.</summary>
 public void RegisterPrefab(string id, DynValue factory)
 {
 sideGate.EnsureServer("Prefabs.Register");
 if (factory.Type != DataType.Function) throw new ScriptRuntimeException("factory must be a function");
 prefabRegistry.Register(id, factory.Function);
 Log.Information("[API][Prefabs] register id={Id}", id);
 }

 /// <summary>Adds a post-init hook to run after prefab spawn. Server-only.</summary>
 public void AddPrefabPostInit(string id, DynValue hook)
 {
 sideGate.EnsureServer("Prefabs.PostInit");
 if (hook.Type != DataType.Function) throw new ScriptRuntimeException("hook must be a function");
 hookBus.AddPrefabPostInit(id, hook.Function);
 Log.Information("[API][Prefabs] post-init added id={Id}", id);
 }

 /// <summary>Spawns a prefab and returns a safe entity proxy. Server-only.</summary>
 public IEntityProxy SpawnPrefab(string id, double x =0, double y =0, Table? props = null)
 {
 sideGate.EnsureServer("Prefabs.Spawn");
 if (!prefabRegistry.TryGet(id, out var factory)) throw new ScriptRuntimeException($"unknown prefab: {id}");
 var proxy = spawner.Spawn(id, new SpawnContext(x, y, props), factory);
 hookBus.InvokePrefabPostInits(id, proxy, Log.Logger);
 _entityIndex.Add(proxy);
 return proxy;
 }

 /// <summary>Find entities with a tag. Server-only.</summary>
 public IReadOnlyList<IEntityProxy> FindWithTag(string tag)
 {
 sideGate.EnsureServer("Entity.FindWithTag");
 if (string.IsNullOrWhiteSpace(tag)) throw new ScriptRuntimeException("tag required");
 return _entityIndex.FindWithTag(tag);
 }

 /// <summary>Find entities within radius, optionally filtered by tag. Server-only.</summary>
 public IReadOnlyList<IEntityProxy> FindInRadius(double x, double y, double radius, string? tag = null)
 {
 sideGate.EnsureServer("Entity.FindInRadius");
 if (radius <0) throw new ScriptRuntimeException("radius must be non-negative");
 return _entityIndex.FindInRadius(x, y, radius, tag);
 }
}
