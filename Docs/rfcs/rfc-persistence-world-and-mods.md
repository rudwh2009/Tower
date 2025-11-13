# RFC: Persistence (World Snapshot + Mod Pinning)

Problem
- Need durable world snapshots with mod pinning and per-mod state.

Goals
- Save/load entities + components with the pinned modset and revision.
- Validate modset on load; fail fast if mismatch (later: migration hooks).

Format (v1)
```
{
 saveVersion:1,
 contentRevision: <int>,
 worldSeed: <int>,
 tick: <int>,
 mods: [{id, ver, sha}],
 entities: [{ id, x, y, comps: { "mod:Type": json } }]
}
```

Server API
- `bool SaveWorld(string path, out string error)`
- `bool LoadWorld(string path, out string error)`

Notes
- Uses compact JSON for now. Atomic write (tmp + replace/move).
- Migration hooks and per-mod GameApi.OnSave/OnLoad already exist in Core and will be hooked in later work.

Acceptance
- Round-trip world; load fails if modset differs.
