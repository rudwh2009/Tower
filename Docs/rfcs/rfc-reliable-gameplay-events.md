# RFC: Reliable Path for Gameplay Events

Problem
- Gameplay events (inventory, crafting, state transitions) must arrive exactly once despite loss/reorder.

Approach (Phase1)
- Add Reliable and ReliableAck messages wrapping inner message type.
- Client and Server maintain a small resend window, retransmit with RTO/backoff, and ack received sequences.
- Start by wrapping RpcEvent; can generalize later.

APIs
- Client: `SendReliable(byte innerId, byte[] payload)`, `SetReliableParams(rtoMs, maxRetries)`.
- Server: `SendReliable(ConnectionId to, byte innerId, byte[] payload)`, `SetReliableParams(rtoMs, maxRetries)`.

Semantics
- Best-effort exactly-once delivery (de-dupe by seq can be added later if required).
- Retries until ACK or max retries reached.

Tests (next PR)
- Inject loss/reorder: reliable events arrive; retries observed; window clears on ack.
