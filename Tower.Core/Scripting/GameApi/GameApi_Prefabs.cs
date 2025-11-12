// Copyright (c) Tower.
// Licensed under the MIT License.

namespace Tower.Core.Scripting.GameApi;

/// <summary>
/// Prefab registration stubs.
/// </summary>
public sealed partial class GameApi
{
    /// <summary>Registers a prefab definition (no-op).</summary>
    /// <param name="id">Prefab id.</param>
    /// <param name="def">Prefab definition object.</param>
    public void RegisterPrefab(string id, object def)
    {
    }

    /// <summary>Adds a post-init hook for a prefab (no-op).</summary>
    /// <param name="id">Prefab id.</param>
    /// <param name="fn">Callback.</param>
    public void AddPrefabPostInit(string id, LuaAction fn)
    {
    }

    /// <summary>Spawns a prefab and returns a placeholder entity (no-op).</summary>
    /// <param name="id">Prefab id.</param>
    /// <returns>Placeholder entity.</returns>
    public object SpawnPrefab(string id)
    {
        return new { Id = id };
    }
}
