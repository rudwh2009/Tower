# RFC: ECS Net Replication – Client Apply

Problem
- Server now emits `CompBaseline` and `CompDelta`, but client had no handlers.

Solution
- Add client-side component store keyed by `(entityId, (modNs,type))`.
- Handle `CompBaseline` by clearing and seeding the store, set `compBaselineReceived=true`.
- Handle `CompDelta` by applying replacements/removes; ignore deltas until baseline arrives.
- Expose `TryGetComponentJson(entityId, modNs, typeName, out json)` helper for UI mods/tests.

Tests
- To be extended once sample UI queries replicated components.
