# Networking Status

- Auth: HMAC nonce handshake implemented; counters present. Metrics include success and failure counts.
- Outbound queues: token bucket per-connection; `SetOutboundRateLimit` knob.
- Chaos harness: LoopbackDuplex supports loss/dup/reorder and jitter (timer-based).
- Metrics: per-type rx/tx counters, bytes, and drops (unauth, ratelimited, oversize, snapshot-budget); AOI metrics (messages, entities, candidates); Delta metrics (messages, replacements, removes); Auth metrics (success, failures); Compression metrics (original, on-wire, and total compressed bytes).
- Snapshot budget: per-connection budget via `SetSnapshotBudget`, toggle via `SetSnapshotBudgetEnabled`.
- RPC oversize: dropped when exceeding `SetMaxRpcBytes`; metric increments.
- Next:
 - Improve delta accuracy for AOI join/leave transitions; add compression baseline and expose compression ratio.
 - Expose metrics externally and integrate with ops health.
 - Promote LiteNetLib path and add chaos layer adapter.
