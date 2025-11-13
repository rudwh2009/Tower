# RFC: Per-type quotas and unauth DoS guards

Problem: Unauthenticated clients could spam arbitrary messages, and certain message types need explicit quotas and size caps.

Changes:
- Added rate limits for JoinRequest, AuthResponse, and a generic unauth window.
- Added RPC payload size cap via `SetMaxRpcBytes`.

Tests:
- UnauthSpamGuardTests validates spam before auth doesn't crash and join still succeeds afterwards.

Next:
- Per-type counters and exposure in networking dashboard.
- Configurable limits via server config.
- Integration with LiteNetLib transports and chaos harness.
