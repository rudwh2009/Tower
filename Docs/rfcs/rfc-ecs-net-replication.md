# RFC: ECS Net Replication (schemas + baselines/deltas)

Problem
- Mods can register components server-side but no replication to clients exists.

Goals
- Allow mods/components to declare what replicates and how.
- Extend protocol with component baselines/deltas per entity, filtered by AOI.

Approach (Phase1 minimal)
- Runtime registration API: `NetServer.RegisterReplicatedComponent(modNs, typeName)`.
- Protocol: add `CompBaseline` and `CompDelta` messages.
- Encoding: compact UTF-8 JSON payload per component for now; can be swapped for binary later.
- Server: maintain per-connection last-sent component map; send a baseline after `StartLoading`, and deltas on `SetComponentData` for entities in AOI.
- Client: T.B.D. (next PR) to apply baselines and deltas to a client-side component view.

Constraints
- Honor snapshot budget and compression envelope.
- Lua-first gameplay (server-only mutation APIs); client receives replicated state only.

Acceptance tests
- Server can register a replicated component and, after setting data, emits a `CompBaseline` on load and `CompDelta` on subsequent updates when entity is in AOI.
- Unknown/unregistered components are not sent.
- Budget respected; large baselines are dropped with metric increment.

Future work
- Attribute-based schemas, reliable channel for critical comps, per-component budgets, client application, resync flows.
