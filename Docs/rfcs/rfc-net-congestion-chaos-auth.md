# RFC: Networking hardening (auth, quotas, chaos, metrics)

Problem: Current networking lacks authentication, quotas, chaos tests, and bandwidth metrics.

Goals:
- Add tokenless HMAC auth handshake (nonce + shared secret) as a first step.
- Enforce message size cap and per-type rate limits (existing windows) and prepare per-type quotas.
- Wire chaos test scaffolding for loss/latency/dup/ooo using LoopbackDuplex to simulate network faults.
- Add basic metrics counters for auth success/failure and bytes sent per message type.

Design:
- Server: on JoinRequest, send AuthChallenge(nonce). Client: compute HMAC(nonce) with shared key and reply AuthResponse.
- Server validates, then sends JoinAccepted and proceeds as today.
- Drop all non-auth messages until authenticated.
- Maintain counters: auth_success, auth_failures.
- Configurable shared key: `NetServer.SetAuthKey`, `NetClient.SetAuthKey`.

Tests:
- Unit test auth happy path over LoopbackTransport; ensure client gets JoinAccepted only after AuthResponse.
- Negative test: wrong client key -> no JoinAccepted.
- Chaos tests: inject loss/jitter and assert auth eventually succeeds (with retries) and join proceeds.

Migration:
- Clients must support the new AuthChallenge/AuthResponse before JoinAccepted; no change to mod sync or gameplay.
