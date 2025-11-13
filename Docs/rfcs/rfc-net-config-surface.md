# RFC: Net server config surface

Goal: Provide a typed configuration surface to apply networking/auth/limits knobs without scattering setters across hosts.

API:
- `NetServerConfig` with optional properties (see code). Includes:
 - `MetricsDumpPath`, `MetricsDumpIntervalSeconds` to periodically write server metrics to a JSON file.
 - `MetricsLogIntervalSeconds` to emit summary metrics to logs.
 - `UseDeltas` to toggle delta snapshot emission.
 - `CompressThresholdBytes` to enable Deflate-compressed envelopes for large messages (baseline/snapshot) and set a size threshold.
- `NetConfigJson.Load(path)` binds from JSON.
- `GameServer` reads from `TOWER_NETCFG` environment var or `netconfig.json` and applies to `NetServer`. If configured, it dumps metrics to file and/or logs at intervals.

Log format example:
- `NET m:rx={rx} tx={tx} drop[u={u} rl={rl} os={os} sb={sb}] aoi[msg={am} ents={ae} cand={ac}] delta[msg={dm} rep={dr} rem={dv}] auth[s={as} f={af}]`

Tests:
- Existing server config binding tests cover JSON loader. Log interval and compression knobs are smoke-only.
