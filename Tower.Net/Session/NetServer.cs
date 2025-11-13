using Serilog;
using Tower.Net.Abstractions;
using Tower.Net.Protocol;
using Tower.Net.Protocol.Messages;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Text.Json;
using System.Collections.Concurrent;

namespace Tower.Net.Session;

public sealed partial class NetServer : IWorldPublisher, IWorldQuery, IEntityRegistry
{
 // transport and mod sync
 private readonly IServerTransport _transport;
 private int _nextId =1;
 private int _contentRevision;
 private readonly ModAdvert[] _mods;
 private IModPackProvider? _packs;

 // connection/state
 private readonly Dictionary<ConnectionId, long> _lastSeen = new();
 private long _lastHbSent;
 private int _timeoutMs =5000;
 private int _inboundCap =256 *1024;
 private int _hbIntervalMs =1000;

 // mod streaming
 private readonly Dictionary<ConnectionId, List<(string id, string sha, byte[] bytes, int pos)>> _streams = new();
 private long _bytesPerSec =512 *1024; private int _maxChunk =64 *1024; private long _lastRefill;
 private readonly Dictionary<ConnectionId, double> _tokens = new();

 // inputs and rpc windows
 private readonly Dictionary<ConnectionId, int> _lastInputTick = new();
 private int _maxInputsPerSecond =60; private readonly Dictionary<ConnectionId, (int count, long start)> _inputWindow = new();
 private int _maxRpcPerSecond =30; private readonly Dictionary<ConnectionId, (int count, long start)> _rpcWindow = new();
 private readonly Dictionary<ConnectionId, (int count, long start)> _rpcCapWindow = new();
 private int _maxRpcBytesPerSecond =128 *1024;

 // unauth quotas and rpc size cap
 private readonly Dictionary<ConnectionId, (int count, long start)> _authRespWindow = new();
 private readonly Dictionary<ConnectionId, (int count, long start)> _joinWindow = new();
 private readonly Dictionary<ConnectionId, (int count, long start)> _unauthWindow = new();
 private int _maxAuthResponsesPerSecond =4;
 private int _maxJoinPerSecond =1;
 private int _maxUnauthMsgsPerSecond =8;
 private int _maxRpcBytes =32 *1024;

 // time/tick
 private INetClock? _clock;
 private int _tick;
 private int _tickRate =20;
 private long _serverStartMs;

 // ECS registry backing store
 private int _nextEntityId =1000;
 private readonly Dictionary<string, HashSet<string>> _componentTypes = new();
 private readonly Dictionary<int, Dictionary<(string modNs, string typeName), string>> _components = new();
 private sealed record ComponentSchema(int Version, Func<JsonElement, bool>? Validator);
 private readonly Dictionary<(string modNs, string typeName), ComponentSchema> _schemas = new();
 private readonly Dictionary<(int entityId, string modNs, string typeName), (int jsonBytes, int tick)> _compDirty = new();
 private readonly Dictionary<(int entityId, string modNs, string typeName), int> _compVersion = new();
 private readonly Dictionary<(string modNs, string typeName, int fromVer, int toVer), Func<JsonElement, JsonElement>> _migrations = new();
 public void RegisterComponentSchema(string modNs, string typeName, int version, Func<JsonElement, bool>? validator = null)
 { if (string.IsNullOrWhiteSpace(modNs) || string.IsNullOrWhiteSpace(typeName)) return; _schemas[(modNs, typeName)] = new ComponentSchema(version, validator); }
 public void RegisterComponentMigration(string modNs, string typeName, int fromVersion, int toVersion, Func<JsonElement, JsonElement> migrator)
 { if (string.IsNullOrWhiteSpace(modNs) || string.IsNullOrWhiteSpace(typeName)) return; if (migrator is null) return; _migrations[(modNs, typeName, fromVersion, toVersion)] = migrator; }

 // replication registry and per-connection last-sent state + budgets
 public enum RepBudget { High, Medium, Low }
 private readonly Dictionary<(string modNs, string typeName), RepBudget> _replicated = new();
 private readonly Dictionary<ConnectionId, Dictionary<(int id, string modNs, string typeName), string>> _lastCompSent = new();
 private long _compHighPerSec =16 *1024;
 private long _compMedPerSec =8 *1024;
 private long _compLowPerSec =2 *1024;
 private readonly Dictionary<ConnectionId, Dictionary<RepBudget, double>> _compTokens = new();
 private long _compBytesHigh, _compBytesMed, _compBytesLow;
 public void SetComponentBudgets(long highPerSec, long medPerSec, long lowPerSec)
 { _compHighPerSec = Math.Max(0, highPerSec); _compMedPerSec = Math.Max(0, medPerSec); _compLowPerSec = Math.Max(0, lowPerSec); }
 public void RegisterReplicatedComponent(string modNs, string typeName) => RegisterReplicatedComponent(modNs, typeName, RepBudget.Medium);
 public void RegisterReplicatedComponent(string modNs, string typeName, RepBudget budget)
 { if (string.IsNullOrWhiteSpace(modNs) || string.IsNullOrWhiteSpace(typeName)) return; _replicated[(modNs, typeName)] = budget; }

 // gameplay sink
 private INetGameplaySink? _sink;

 // auth and outbound rate limiting
 private readonly Dictionary<ConnectionId, bool> _authed = new();
 private byte[] _authKey = Encoding.UTF8.GetBytes("dev-shared-secret");
 private readonly Dictionary<ConnectionId, int> _nonces = new();
 private readonly List<(byte[] key, long expiresAt)> _oldAuthKeys = new();
 private long _outBytesPerSec =128 *1024;
 private readonly Dictionary<ConnectionId, double> _outTokens = new();
 private readonly Dictionary<ConnectionId, Queue<byte[]>> _outbox = new();

 // world and interest
 private readonly Dictionary<int, (float x, float y)> _world = new();
 private readonly Dictionary<ConnectionId, int> _connToEntity = new();
 private float _interestRadius =128f;
 private int _worldSeed =12345;

 // AOI grid cell indexing
 private int _aoiCellSize; //0 = disabled
 private readonly Dictionary<(int cx, int cy), HashSet<int>> _cells = new();
 private static (int cx, int cy) CellFor(float x, float y, int cellSize)
 { if (cellSize <=0) return (0,0); int cx = (int)Math.Floor(x / cellSize); int cy = (int)Math.Floor(y / cellSize); return (cx, cy); }
 private void IndexEntity(int id, float x, float y)
 { if (_aoiCellSize <=0) return; var c = CellFor(x, y, _aoiCellSize); if (!_cells.TryGetValue(c, out var set)) { set = new HashSet<int>(); _cells[c] = set; } set.Add(id); }
 private void ReindexEntity(int id, float oldX, float oldY, float newX, float newY)
 { if (_aoiCellSize <=0) return; var cold = CellFor(oldX, oldY, _aoiCellSize); var cnew = CellFor(newX, newY, _aoiCellSize); if (cold == cnew) return; if (_cells.TryGetValue(cold, out var setOld)) { setOld.Remove(id); if (setOld.Count ==0) _cells.Remove(cold); } if (!_cells.TryGetValue(cnew, out var setNew)) { setNew = new HashSet<int>(); _cells[cnew] = setNew; } setNew.Add(id); }
 private void RemoveIndex(int id, float x, float y)
 { if (_aoiCellSize <=0) return; var c = CellFor(x, y, _aoiCellSize); if (_cells.TryGetValue(c, out var set)) { set.Remove(id); if (set.Count ==0) _cells.Remove(c); } }

 // snapshot bandwidth budget
 private long _snapBytesPerSec =64 *1024;
 private readonly Dictionary<ConnectionId, double> _snapTokens = new();

 // snapshot compression
 private int _compressThreshold =32 *1024;
 private long _txCompressedBytes;
 private bool _snapBudgetEnabled = true;
 public void SetSnapshotBudgetEnabled(bool enabled) { _snapBudgetEnabled = enabled; }
 public void SetCompressThreshold(int bytes) { _compressThreshold = Math.Max(0, bytes); }

 // metrics
 private readonly Dictionary<MessageId, long> _rxCounts = new();
 private readonly Dictionary<MessageId, long> _txCounts = new();
 private readonly Dictionary<MessageId, long> _rxBytes = new();
 private readonly Dictionary<MessageId, long> _txBytes = new();
 private long _dropsUnauth;
 private long _dropsRateLimited;
 private long _dropsOversize;
 private long _dropsSnapshotBudget;
 private long _aoiSnapshotMsgs;
 private long _aoiEntitiesSentTotal;
 private long _aoiCandidatesTotal;
 private long _deltaMessages;
 private long _deltaReplacementsTotal;
 private long _deltaRemovesTotal;
 private long _authSuccess;
 private long _authFailures;
 private long _compOriginalBytes;
 private long _compOnWireBytes;

 // reliable messaging support
 private readonly Dictionary<ConnectionId, Dictionary<uint, (byte id, byte[] payload, long sentAt, int retries)>> _relWindow = new();
 private readonly Dictionary<ConnectionId, uint> _relSendSeq = new();
 private readonly Dictionary<ConnectionId, uint> _relAcked = new();
 private readonly Dictionary<ConnectionId, HashSet<uint>> _relRecvSeen = new();
 private readonly Dictionary<ConnectionId, Queue<uint>> _relRecvOrder = new();
 private int _relRecvWindow =256;
 private int _relRtoMs =250; private int _relMaxRetries =5;
 private long _relRetriesTotal;
 private readonly Dictionary<ConnectionId, Queue<long>> _relRttSamples = new();
 public void SetReliableParams(int rtoMs, int maxRetries) { _relRtoMs = Math.Clamp(rtoMs,50,5000); _relMaxRetries = Math.Clamp(maxRetries,1,20); }
 public void SetReliableRecvWindow(int entries) { _relRecvWindow = Math.Clamp(entries,16,4096); }
 private void EnsureReliableState(ConnectionId c)
 { if (!_relWindow.ContainsKey(c)) _relWindow[c] = new(); if (!_relSendSeq.ContainsKey(c)) _relSendSeq[c] =0; if (!_relAcked.ContainsKey(c)) _relAcked[c] =0; if (!_relRecvSeen.ContainsKey(c)) _relRecvSeen[c] = new(); if (!_relRecvOrder.ContainsKey(c)) _relRecvOrder[c] = new(); }
 private bool MarkReliableSeen(ConnectionId c, uint seq)
 { EnsureReliableState(c); var seen = _relRecvSeen[c]; if (seen.Contains(seq)) return false; seen.Add(seq); var q = _relRecvOrder[c]; q.Enqueue(seq); while (q.Count > _relRecvWindow) { var old = q.Dequeue(); seen.Remove(old); } return true; }
 private readonly Random _rng = new(); private double _chaosDropOutPct; public void SetChaosDropOutbound(double pct) { _chaosDropOutPct = Math.Clamp(pct,0.0,1.0); }
 private void SendWithChaos(ConnectionId to, byte[] data) { if (_chaosDropOutPct >0 && _rng.NextDouble() < _chaosDropOutPct) return; _transport.Send(to, data); }
 private void PumpReliable(long now)
 { foreach (var (conn, win) in _relWindow.ToArray()) foreach (var kv in win.ToArray()) { var seq = kv.Key; var (id, payload, sentAt, retries) = kv.Value; if (now - sentAt >= _relRtoMs && retries < _relMaxRetries) { var rel = new Reliable(seq, id, payload).Serialize(); var buf = new byte[1 + rel.Length]; buf[0] = (byte)MessageId.Reliable; rel.CopyTo(buf.AsSpan(1)); SendWithChaos(conn, buf); _relWindow[conn][seq] = (id, payload, now, retries +1); _relRetriesTotal++; } } }
 public void SendReliable(ConnectionId to, byte innerId, byte[] payload)
 { EnsureReliableState(to); var seq = ++_relSendSeq[to]; var rel = new Reliable(seq, innerId, payload ?? Array.Empty<byte>()).Serialize(); var buf = new byte[1 + rel.Length]; buf[0] = (byte)MessageId.Reliable; rel.CopyTo(buf.AsSpan(1)); SendWithChaos(to, buf); _relWindow[to][seq] = (innerId, payload ?? Array.Empty<byte>(), Environment.TickCount64,0); }
 private IEnumerable<long> AllRtts() { foreach (var q in _relRttSamples.Values) foreach (var v in q) yield return v; }
 private static long Percentile(IEnumerable<long> values, int p)
 { var list = values is IList<long> il ? il : values.ToList(); if (list.Count ==0) return0; var arr = list.OrderBy(x => x).ToArray(); var rank = Math.Clamp((int)Math.Round((p/100.0) * (arr.Length -1)),0, arr.Length -1); return arr[rank]; }
 private IModStateProvider? _modStateProvider; public void SetModStateProvider(IModStateProvider provider) { _modStateProvider = provider; }
 private sealed class SingleConnAdapter : IServerTransport
 { private readonly ITransport _t; private readonly ConnectionId _id = new(1); public SingleConnAdapter(ITransport t) { _t = t; } public void Start() => _t.Bind(); public void Stop() => _t.Disconnect(); public void Send(ConnectionId to, ReadOnlyMemory<byte> data) => _t.Send(data); public void Broadcast(ReadOnlyMemory<byte> data) => _t.Send(data); public void Poll(Action<ConnectionId, ReadOnlyMemory<byte>> onMessage) => _t.Poll(m => onMessage(_id, m)); public IEnumerable<ConnectionId> Connections { get { yield return _id; } } }
 public NetServer(IServerTransport transport, ModAdvert[]? mods = null) { _transport = transport; _mods = mods ?? new[] { new ModAdvert("SampleUi", "1.0.0", "hash123",12, "1.0") }; }
 public NetServer(ITransport transport, ModAdvert[]? mods = null) : this(new SingleConnAdapter(transport), mods) { }
 public void SetModPackProvider(IModPackProvider provider) => _packs = provider;
 public void SetGameplaySink(INetGameplaySink sink) => _sink = sink;
 public void SetWorldSeed(int seed) { _worldSeed = seed; }
 public void SetInterestRadius(float radius) { _interestRadius = Math.Max(0f, radius); }
 public void SetClock(INetClock clock) { _clock = clock; _serverStartMs = clock.NowMs; }
 public void SetTickRate(int ticksPerSecond) { _tickRate = Math.Clamp(ticksPerSecond,1,240); }
 public void Start() { _transport.Start(); _lastHbSent = Environment.TickCount64; _lastRefill = _lastHbSent; _serverStartMs = _clock?.NowMs ?? Environment.TickCount64; _tick =0; }
 public void Stop()
 {
 _transport.Stop();
 _lastSeen.Clear(); _streams.Clear(); _tokens.Clear(); _lastInputTick.Clear(); _inputWindow.Clear(); _rpcWindow.Clear();
 _outTokens.Clear(); _outbox.Clear(); _world.Clear(); _connToEntity.Clear(); _cells.Clear();
 _snapTokens.Clear(); _connBaseline.Clear(); _lastSent.Clear(); _lastCompSent.Clear(); _compTokens.Clear();
 _authRespWindow.Clear(); _joinWindow.Clear(); _unauthWindow.Clear();
 _relRttSamples.Clear();
 _lastSnapSentTick.Clear();
 }
 public void SetTimeout(int ms) { _timeoutMs = Math.Max(1000, ms); }
 public void SetInboundCap(int bytes) { _inboundCap = Math.Max(1024, bytes); }
 public void SetRateLimit(long bytesPerSecond, int maxChunk =64 *1024) { _bytesPerSec = Math.Max(1024, bytesPerSecond); _maxChunk = Math.Max(1024, maxChunk); }
 public void SetInputRateCap(int perSecond) { _maxInputsPerSecond = Math.Max(10, perSecond); }
 public void SetRpcRateCap(int perSecond) { _maxRpcPerSecond = Math.Max(5, perSecond); }
 public void SetRpcByteBudget(int bytesPerSecond) { _maxRpcBytesPerSecond = Math.Max(1024, bytesPerSecond); }
 public void SetOutboundRateLimit(long bytesPerSecond) { _outBytesPerSec = Math.Max(1024, bytesPerSecond); }
 public void SetAuthKey(byte[] key) { _authKey = key ?? Array.Empty<byte>(); }
 public int GetTotalOutboundQueued() => _outbox.Values.Sum(q => q.Count);
 public void SetSnapshotBudget(long bytesPerSecond) { _snapBytesPerSec = Math.Max(1024, bytesPerSecond); }
 public void SetAuthLimits(int maxJoinPerSec =1, int maxAuthRespPerSec =4, int maxUnauthMsgsPerSec =8) { _maxJoinPerSecond = Math.Max(1, maxJoinPerSec); _maxAuthResponsesPerSecond = Math.Max(1, maxAuthRespPerSec); _maxUnauthMsgsPerSecond = Math.Max(1, maxUnauthMsgsPerSec); }
 public void SetMaxRpcBytes(int bytes) { _maxRpcBytes = Math.Max(256, bytes); }
 public void SetAoiCellSize(int size) { _aoiCellSize = Math.Max(0, size); }
 public void SetUseDeltas(bool enabled) { _useDeltas = enabled; }

 // baseline/delta scaffolding
 private bool _useDeltas;
 private readonly Dictionary<ConnectionId, int> _connBaseline = new();
 private readonly Dictionary<ConnectionId, Dictionary<int, (float x, float y)>> _lastSent = new();
 private int _nextBaselineId;

 // snapshot ack tracking
 private readonly Dictionary<ConnectionId, (int tick, uint hash)> _lastSnapAck = new();
 private readonly Dictionary<ConnectionId, Dictionary<int, uint>> _sentSnapHashes = new();
 private long _rxSnapshotAcks;
 private long _rxSnapshotAckMismatches;

 // per-connection snapshot cadence
 private readonly Dictionary<ConnectionId, int> _lastSnapSentTick = new();

 private static uint Fnv1a32(ReadOnlySpan<byte> data)
 {
 const uint offset =2166136261u; const uint prime =16777619u;
 uint hash = offset;
 for (int i=0;i<data.Length;i++) { hash ^= data[i]; hash *= prime; }
 return hash;
 }

 public sealed class NetMetrics
 {
 public Dictionary<string, long> Rx { get; init; } = new();
 public Dictionary<string, long> Tx { get; init; } = new();
 public Dictionary<string, long> RxBytes { get; init; } = new();
 public Dictionary<string, long> TxBytes { get; init; } = new();
 public long DroppedUnauth { get; init; }
 public long DroppedRateLimited { get; init; }
 public long DroppedOversize { get; init; }
 public long DroppedSnapshotBudget { get; init; }
 public long SnapshotMessages { get; init; }
 public long SnapshotEntitiesTotal { get; init; }
 public long SnapshotCandidatesTotal { get; init; }
 public long DeltaMessages { get; init; }
 public long DeltaReplacementsTotal { get; init; }
 public long DeltaRemovesTotal { get; init; }
 public long AuthSuccess { get; init; }
 public long AuthFailures { get; init; }
 public long TxCompressedBytes { get; init; }
 public long CompBytesHigh { get; init; }
 public long CompBytesMed { get; init; }
 public long CompBytesLow { get; init; }
 public long CompressedOriginalBytes { get; init; }
 public long CompressedOnWireBytes { get; init; }
 public int Connections { get; init; }
 public int AoiCellCount { get; init; }
 public long ReliableOutstanding { get; init; }
 public long ReliableRetriesTotal { get; init; }
 public long ReliableRttP50Ms { get; init; }
 public long ReliableRttP95Ms { get; init; }
 public long RxSnapshotAcks { get; init; }
 public long RxSnapshotAckMismatches { get; init; }
 }
 public NetMetrics GetMetrics() => new NetMetrics
 {
 Rx = _rxCounts.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
 Tx = _txCounts.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
 RxBytes = _rxBytes.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
 TxBytes = _txBytes.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
 DroppedUnauth = _dropsUnauth,
 DroppedRateLimited = _dropsRateLimited,
 DroppedOversize = _dropsOversize,
 DroppedSnapshotBudget = _dropsSnapshotBudget,
 SnapshotMessages = _aoiSnapshotMsgs,
 SnapshotEntitiesTotal = _aoiEntitiesSentTotal,
 SnapshotCandidatesTotal = _aoiCandidatesTotal,
 DeltaMessages = _deltaMessages,
 DeltaReplacementsTotal = _deltaReplacementsTotal,
 DeltaRemovesTotal = _deltaRemovesTotal,
 AuthSuccess = _authSuccess,
 AuthFailures = _authFailures,
 TxCompressedBytes = _txCompressedBytes,
 CompBytesHigh = _compBytesHigh,
 CompBytesMed = _compBytesMed,
 CompBytesLow = _compBytesLow,
 CompressedOriginalBytes = _compOriginalBytes,
 CompressedOnWireBytes = _compOnWireBytes,
 Connections = _transport.Connections.Count(),
 AoiCellCount = _cells.Count,
 ReliableOutstanding = _relWindow.Sum(kv => (long)kv.Value.Count),
 ReliableRetriesTotal = _relRetriesTotal,
 ReliableRttP50Ms = Percentile(AllRtts(),50),
 ReliableRttP95Ms = Percentile(AllRtts(),95),
 RxSnapshotAcks = _rxSnapshotAcks,
 RxSnapshotAckMismatches = _rxSnapshotAckMismatches,
 };

 public void DebugSpawnEntity(int id, float x, float y) { _world[id] = (x, y); IndexEntity(id, x, y); }
 public void PublishEntityPosition(int id, float x, float y) { if (_world.TryGetValue(id, out var old)) ReindexEntity(id, old.x, old.y, x, y); else IndexEntity(id, x, y); _world[id] = (x, y); }

 private void Enqueue(ConnectionId conn, byte[] buf)
 {
 if (buf.Length >0)
 {
 var id = (MessageId)buf[0];
 _txCounts[id] = _txCounts.TryGetValue(id, out var c) ? c +1 :1;
 _txBytes[id] = _txBytes.TryGetValue(id, out var b) ? b + buf.Length : buf.Length;
 }
 if (!_outbox.TryGetValue(conn, out var q)) { q = new Queue<byte[]>(); _outbox[conn] = q; }
 q.Enqueue(buf);
 if (!_outTokens.ContainsKey(conn)) _outTokens[conn] = _outBytesPerSec;
 }
 private void EnqueueMaybeCompressed(ConnectionId conn, MessageId id, byte[] payload)
 {
 if (_compressThreshold >0 && payload.Length >= _compressThreshold)
 {
 var msg = new Compressed((byte)id, payload).Serialize();
 var buf = new byte[1 + msg.Length]; buf[0] = (byte)MessageId.Compressed; msg.CopyTo(buf.AsSpan(1));
 _txCompressedBytes += msg.Length;
 _compOriginalBytes += payload.Length;
 _compOnWireBytes += msg.Length;
 Enqueue(conn, buf);
 }
 else
 {
 var buf = new byte[1 + payload.Length]; buf[0] = (byte)id; payload.CopyTo(buf.AsSpan(1));
 Enqueue(conn, buf);
 }
 }
 private static int ComputeRevision(ModAdvert[] mods)
 {
 using var sha = SHA256.Create();
 var sb = new StringBuilder();
 foreach (var m in mods.OrderBy(m => m.Id + "@" + m.Version + "@" + m.Sha256, StringComparer.Ordinal)) sb.Append(m.Id).Append('@').Append(m.Version).Append('@').Append(m.Sha256).Append(';');
 var bytes = Encoding.UTF8.GetBytes(sb.ToString()); var hash = sha.ComputeHash(bytes); int rev = BitConverter.ToInt32(hash,0); if (rev <0) rev = -rev; if (rev ==0) rev =1; return rev;
 }
 private void EnsureConnState(ConnectionId c)
 {
 if (!_streams.ContainsKey(c)) _streams[c] = new List<(string, string, byte[], int)>();
 if (!_tokens.ContainsKey(c)) _tokens[c] = _bytesPerSec;
 if (!_inputWindow.ContainsKey(c)) _inputWindow[c] = (0, Environment.TickCount64);
 if (!_rpcWindow.ContainsKey(c)) _rpcWindow[c] = (0, Environment.TickCount64);
 if (!_outTokens.ContainsKey(c)) _outTokens[c] = _outBytesPerSec;
 if (!_outbox.ContainsKey(c)) _outbox[c] = new Queue<byte[]>();
 if (!_snapTokens.ContainsKey(c)) _snapTokens[c] = _snapBytesPerSec;
 if (!_lastCompSent.ContainsKey(c)) _lastCompSent[c] = new();
 if (!_compTokens.ContainsKey(c)) _compTokens[c] = new() { [RepBudget.High] = _compHighPerSec, [RepBudget.Medium] = _compMedPerSec, [RepBudget.Low] = _compLowPerSec };
 }
 private void SendAdvertise(ConnectionId conn)
 {
 _contentRevision = ComputeRevision(_mods);
 var adv = new ModSetAdvertise(_contentRevision, _mods).Serialize(); var abuf = new byte[1 + adv.Length]; abuf[0] = (byte)MessageId.ModSetAdvertise; adv.CopyTo(abuf.AsSpan(1));
 Enqueue(conn, abuf);
 }
 private void SendStartLoading(ConnectionId conn)
 {
 var start = new StartLoading(_worldSeed, _tickRate, "world-1").Serialize(); var sbuf = new byte[1 + start.Length]; sbuf[0] = (byte)MessageId.StartLoading; start.CopyTo(sbuf.AsSpan(1)); Enqueue(conn, sbuf);
 SendBaseline(conn);
 SendCompBaseline(conn);
 }
 private void SendBaseline(ConnectionId conn)
 {
 var list = _world.Select(kv => new EntityState(kv.Key, kv.Value.x, kv.Value.y)).ToArray();
 var bid = ++_nextBaselineId; var msg = new SnapshotBaseline(bid, list).Serialize();
 var outLen =1 + msg.Length;
 if (_compressThreshold >0 && msg.Length >= _compressThreshold)
 {
 var compLen = new Compressed((byte)MessageId.SnapshotBaseline, msg).Serialize().Length;
 outLen =1 + compLen;
 }
 if (!_snapTokens.TryGetValue(conn, out var _)) EnsureConnState(conn);
 if (_snapBudgetEnabled)
 {
 if (_snapTokens[conn] < outLen) { _dropsSnapshotBudget++; return; }
 _snapTokens[conn] -= outLen;
 }
 EnqueueMaybeCompressed(conn, MessageId.SnapshotBaseline, msg);
 _connBaseline[conn] = bid; _lastSent[conn] = _world.ToDictionary(kv => kv.Key, kv => (kv.Value.x, kv.Value.y));
 }
 private void SendCompBaseline(ConnectionId conn)
 {
 var entities = new List<CompEntity>();
 foreach (var kv in _components)
 {
 var eid = kv.Key; var cmap = kv.Value;
 var comps = cmap.Where(p => _replicated.ContainsKey((p.Key.modNs, p.Key.typeName)))
 .Select(p => new CompComponent(p.Key.modNs, p.Key.typeName, p.Value)).ToArray();
 if (comps.Length ==0) continue;
 entities.Add(new CompEntity(eid, comps));
 }
 if (entities.Count ==0) return;
 var payload = new CompBaseline(entities.ToArray()).Serialize();
 var outLen =1 + payload.Length;
 if (_compressThreshold >0 && payload.Length >= _compressThreshold)
 { var compLen = new Compressed((byte)MessageId.CompBaseline, payload).Serialize().Length; outLen =1 + compLen; }
 if (!_snapTokens.TryGetValue(conn, out var _)) EnsureConnState(conn);
 if (_snapTokens[conn] < outLen) { _dropsSnapshotBudget++; return; }
 _snapTokens[conn] -= outLen;
 EnqueueMaybeCompressed(conn, MessageId.CompBaseline, payload);
 var map = _lastCompSent[conn];
 foreach (var e in entities)
 foreach (var c in e.Components)
 map[(e.EntityId, c.ModNs, c.TypeName)] = c.Json ?? string.Empty;
 }
 private void RefillTokens(long elapsedMs)
 {
 foreach (var key in _tokens.Keys.ToList()) _tokens[key] = Math.Min(_bytesPerSec, _tokens[key] + (_bytesPerSec * (elapsedMs /1000.0)));
 foreach (var key in _outTokens.Keys.ToList()) _outTokens[key] = Math.Min(_outBytesPerSec, _outTokens[key] + (_outBytesPerSec * (elapsedMs /1000.0)));
 foreach (var key in _snapTokens.Keys.ToList()) _snapTokens[key] = Math.Min(_snapBytesPerSec, _snapTokens[key] + (_snapBytesPerSec * (elapsedMs /1000.0)));
 foreach (var (conn, dict) in _compTokens.ToArray())
 {
 if (!dict.ContainsKey(RepBudget.High)) dict[RepBudget.High] =0; if (!dict.ContainsKey(RepBudget.Medium)) dict[RepBudget.Medium] =0; if (!dict.ContainsKey(RepBudget.Low)) dict[RepBudget.Low] =0;
 dict[RepBudget.High] = Math.Min(_compHighPerSec, dict[RepBudget.High] + (_compHighPerSec * (elapsedMs /1000.0)));
 dict[RepBudget.Medium] = Math.Min(_compMedPerSec, dict[RepBudget.Medium] + (_compMedPerSec * (elapsedMs /1000.0)));
 dict[RepBudget.Low] = Math.Min(_compLowPerSec, dict[RepBudget.Low] + (_compLowPerSec * (elapsedMs /1000.0)));
 }
 }
 private void UpdateTick() { var ms = (_clock?.NowMs ?? Environment.TickCount64) - _serverStartMs; _tick = (int)(ms / (1000 / _tickRate)); }
 private void Pump()
 {
 UpdateTick();
 var now = Environment.TickCount64;
 if (now - _lastHbSent >= _hbIntervalMs)
 {
 var hb = new byte[1]; hb[0] = (byte)MessageId.Heartbeat;
 foreach (var c in _transport.Connections) { EnsureConnState(c); Enqueue(c, hb); }
 _lastHbSent = now;
 }
 PumpReliable(now);
 // timeouts
 foreach (var kv in _lastSeen.ToArray())
 {
 if (now - kv.Value > _timeoutMs)
 {
 _lastSeen.Remove(kv.Key);
 _streams.Remove(kv.Key);
 _tokens.Remove(kv.Key);
 _lastInputTick.Remove(kv.Key);
 _inputWindow.Remove(kv.Key);
 _rpcWindow.Remove(kv.Key);
 _outTokens.Remove(kv.Key);
 _outbox.Remove(kv.Key);
 _snapTokens.Remove(kv.Key);
 _connBaseline.Remove(kv.Key);
 _lastSent.Remove(kv.Key);
 _lastCompSent.Remove(kv.Key);
 _compTokens.Remove(kv.Key);
 _lastSnapSentTick.Remove(kv.Key);
 if (_connToEntity.Remove(kv.Key, out var ent)) { if (_world.TryGetValue(ent, out var p)) RemoveIndex(ent, p.x, p.y); _world.Remove(ent); _sink?.OnClientLeave(ent); }
 }
 }
 var elapsedMs = Math.Max(0, now - _lastRefill);
 _lastRefill = now;
 RefillTokens(elapsedMs);
 // drain mod streams
 foreach (var kv in _streams.ToArray())
 {
 var conn = kv.Key; var list = kv.Value;
 if (!_tokens.TryGetValue(conn, out var tok) || tok <=0) continue;
 int i =0;
 while (i < list.Count && _tokens[conn] >0)
 {
 var (id, sha, bytes, pos) = list[i];
 var remaining = bytes.Length - pos;
 if (remaining <=0)
 {
 var done = new ModDone(id, sha).Serialize(); var dbuf = new byte[1 + done.Length]; dbuf[0] = (byte)MessageId.ModDone; done.CopyTo(dbuf.AsSpan(1)); Enqueue(conn, dbuf);
 list.RemoveAt(i); continue;
 }
 var budget = (int)Math.Min(_tokens[conn], int.MaxValue);
 var toSend = Math.Min(_maxChunk, Math.Min(remaining, budget));
 var payload = new byte[toSend]; Array.Copy(bytes, pos, payload,0, toSend);
 var totalChunks = (ushort)((bytes.Length + _maxChunk -1) / _maxChunk);
 var seq = (ushort)(pos / _maxChunk);
 var chunk = new ModChunk(id, sha, seq, totalChunks, payload).Serialize(); var cbuf = new byte[1 + chunk.Length]; cbuf[0] = (byte)MessageId.ModChunk; chunk.CopyTo(cbuf.AsSpan(1)); Enqueue(conn, cbuf);
 _tokens[conn] -= toSend; list[i] = (id, sha, bytes, pos + toSend); i++;
 }
 if (list.Count ==0 && _tokens[conn] >=0) { SendStartLoading(conn); _streams.Remove(conn); }
 }
 // per-tick snapshots
 foreach (var c in _transport.Connections)
 {
 if (!_authed.TryGetValue(c, out var ok) || !ok) continue;
 if (!_lastSnapSentTick.TryGetValue(c, out var lt) || lt != _tick)
 {
 EnsureConnState(c); SendSnapshotSet(c); _lastSnapSentTick[c] = _tick;
 }
 }
 // drain outbound queues
 foreach (var (conn, q) in _outbox.ToArray())
 {
 if (!_outTokens.TryGetValue(conn, out var tok) || tok <=0) continue;
 while (q.Count >0 && _outTokens[conn] >0)
 {
 var msg = q.Peek(); var len = msg.Length; if (len > _outTokens[conn]) break; SendWithChaos(conn, msg); _outTokens[conn] -= len; q.Dequeue();
 }
 }
 }

 private void SendSnapshotSet(ConnectionId conn)
 {
 if (!_connToEntity.TryGetValue(conn, out var selfId)) return;
 var self = _world[selfId];
 var list = new List<EntityState>();
 int candidates =0;
 if (_aoiCellSize >0)
 {
 var (scx, scy) = CellFor(self.x, self.y, _aoiCellSize);
 int cr = (int)Math.Ceiling(_interestRadius / _aoiCellSize);
 var r2 = _interestRadius * _interestRadius;
 var visited = new HashSet<int>();
 for (int dy = -cr; dy <= cr; dy++)
 for (int dx = -cr; dx <= cr; dx++)
 {
 var key = (scx + dx, scy + dy);
 if (_cells.TryGetValue(key, out var cellSet))
 {
 foreach (var eid in cellSet)
 {
 if (!visited.Add(eid)) continue; if (!_world.TryGetValue(eid, out var p)) continue; var ddx = p.x - self.x; var ddy = p.y - self.y; if (ddx * ddx + ddy * ddy <= r2) list.Add(new EntityState(eid, p.x, p.y));
 }
 }
 }
 candidates = visited.Count;
 }
 else
 {
 candidates = _world.Count;
 foreach (var kv in _world)
 {
 var id = kv.Key; var p = kv.Value; var dx = p.x - self.x; var dy = p.y - self.y; if ((dx * dx + dy * dy) <= _interestRadius * _interestRadius) list.Add(new EntityState(id, p.x, p.y));
 }
 }
 if (list.Count ==0) list.Add(new EntityState(selfId, self.x, self.y));
 var lastInput = _lastInputTick.TryGetValue(conn, out var t) ? t :0;
 var set = new SnapshotSet(_tick, lastInput, list.ToArray()).Serialize();
 if (!_sentSnapHashes.TryGetValue(conn, out var m)) { m = new(); _sentSnapHashes[conn] = m; }
 m[_tick] = Fnv1a32(set);
 if (!_snapTokens.TryGetValue(conn, out var _)) EnsureConnState(conn);
 var outLenSnap =1 + set.Length;
 if (_compressThreshold >0 && set.Length >= _compressThreshold)
 { var compLen = new Compressed((byte)MessageId.SnapshotSet, set).Serialize().Length; outLenSnap =1 + compLen; }
 if (_snapTokens[conn] < outLenSnap) { _dropsSnapshotBudget++; return; }
 _snapTokens[conn] -= outLenSnap;
 _aoiSnapshotMsgs++;
 _aoiEntitiesSentTotal += list.Count;
 _aoiCandidatesTotal += candidates;
 EnqueueMaybeCompressed(conn, MessageId.SnapshotSet, set);

 // AOI enter/leave hooks
 if (_sink is not null)
 {
 var newIds = new HashSet<int>(list.Select(e => e.Id));
 var oldIds = new HashSet<int>();
 if (_lastSent.TryGetValue(conn, out var prevMap)) foreach (var k in prevMap.Keys) oldIds.Add(k);
 foreach (var id in newIds) if (!oldIds.Contains(id)) _sink.OnAoiEnter(selfId, id);
 foreach (var id in oldIds) if (!newIds.Contains(id)) _sink.OnAoiLeave(selfId, id);
 }

 // component deltas for AOI using per-class budgets
 var aoiIds = new HashSet<int>(list.Select(e => e.Id));
 if (!_lastCompSent.TryGetValue(conn, out var lastMap)) lastMap = _lastCompSent[conn] = new();
 var candHigh = new List<(CompReplace rep, int sz)>();
 var candMed = new List<(CompReplace rep, int sz)>();
 var candLow = new List<(CompReplace rep, int sz)>();
 foreach (var eid in aoiIds)
 {
 if (!_components.TryGetValue(eid, out var cmap)) continue;
 foreach (var kv in cmap)
 {
 if (!_replicated.TryGetValue((kv.Key.modNs, kv.Key.typeName), out var cls)) continue;
 var rep = new CompReplace(eid, kv.Key.modNs, kv.Key.typeName, kv.Value);
 if (!lastMap.TryGetValue((eid, rep.ModNs, rep.TypeName), out var prev) || prev != rep.Json)
 {
 int ns = Encoding.UTF8.GetByteCount(rep.ModNs ?? string.Empty);
 int tn = Encoding.UTF8.GetByteCount(rep.TypeName ?? string.Empty);
 int js = _compDirty.TryGetValue((rep.EntityId, rep.ModNs ?? string.Empty, rep.TypeName ?? string.Empty), out var meta) ? meta.jsonBytes : Encoding.UTF8.GetByteCount(rep.Json ?? string.Empty);
 var sz =4 + (4 + ns) + (4 + tn) + (4 + js);
 switch (cls)
 {
 case RepBudget.High: candHigh.Add((rep, sz)); break;
 case RepBudget.Medium: candMed.Add((rep, sz)); break;
 case RepBudget.Low: candLow.Add((rep, sz)); break;
 }
 }
 }
 }
 // allocate per-class
 var toSend = new List<CompReplace>();
 if (!_compTokens.TryGetValue(conn, out var _)) EnsureConnState(conn);
 double h = _compTokens[conn][RepBudget.High], m = _compTokens[conn][RepBudget.Medium], l = _compTokens[conn][RepBudget.Low];
 foreach (var (rep, sz) in candHigh.OrderBy(x => x.rep.EntityId)) { if (h >= sz) { toSend.Add(rep); h -= sz; } }
 foreach (var (rep, sz) in candMed.OrderBy(x => x.rep.EntityId)) { if (m >= sz) { toSend.Add(rep); m -= sz; } }
 foreach (var (rep, sz) in candLow.OrderBy(x => x.rep.EntityId)) { if (l >= sz) { toSend.Add(rep); l -= sz; } }
 if (toSend.Count >0)
 {
 var deltaPayload = new CompDelta(toSend.ToArray(), Array.Empty<CompRemove>()).Serialize();
 var outLen =1 + deltaPayload.Length;
 if (_compressThreshold >0 && deltaPayload.Length >= _compressThreshold)
 { var compLen = new Compressed((byte)MessageId.CompDelta, deltaPayload).Serialize().Length; outLen =1 + compLen; }
 if (_snapTokens[conn] >= outLen)
 {
 _snapTokens[conn] -= outLen;
 EnqueueMaybeCompressed(conn, MessageId.CompDelta, deltaPayload);
 // commit tokens and last-sent
 foreach (var rep in toSend)
 {
 int ns = Encoding.UTF8.GetByteCount(rep.ModNs ?? string.Empty);
 int tn = Encoding.UTF8.GetByteCount(rep.TypeName ?? string.Empty);
 int js = _compDirty.TryGetValue((rep.EntityId, rep.ModNs ?? string.Empty, rep.TypeName ?? string.Empty), out var meta) ? meta.jsonBytes : Encoding.UTF8.GetByteCount(rep.Json ?? string.Empty);
 var sz =4 + (4 + ns) + (4 + tn) + (4 + js);
 var cls = _replicated[(rep.ModNs, rep.TypeName)];
 if (cls == RepBudget.High) { _compTokens[conn][RepBudget.High] = h; _compBytesHigh += sz; }
 else if (cls == RepBudget.Medium) { _compTokens[conn][RepBudget.Medium] = m; _compBytesMed += sz; }
 else { _compTokens[conn][RepBudget.Low] = l; _compBytesLow += sz; }
 lastMap[(rep.EntityId, rep.ModNs, rep.TypeName)] = rep.Json ?? string.Empty;
 }
 }
 else { _dropsSnapshotBudget++; }
 if (_useDeltas && _connBaseline.TryGetValue(conn, out var curBid))
 {
 var current = list.ToDictionary(e => e.Id, e => (e.X, e.Y));
 if (!_lastSent.TryGetValue(conn, out var prev)) prev = new Dictionary<int, (float x, float y)>();
 var replacements = new List<EntityState>();
 foreach (var kv in current)
 {
 if (!prev.TryGetValue(kv.Key, out var pp) || Math.Abs(pp.x - kv.Value.Item1) > float.Epsilon || Math.Abs(pp.y - kv.Value.Item2) > float.Epsilon)
 replacements.Add(new EntityState(kv.Key, kv.Value.Item1, kv.Value.Item2));
 }
 var removes = prev.Keys.Where(id => !current.ContainsKey(id)).ToArray();
 if (replacements.Count >0 || removes.Length >0)
 {
 var dmsg = new SnapshotDelta(curBid, replacements.ToArray(), removes).Serialize(); var dbuf = new byte[1 + dmsg.Length]; dbuf[0] = (byte)MessageId.SnapshotDelta; dmsg.CopyTo(dbuf.AsSpan(1)); Enqueue(conn, dbuf);
 _connBaseline[conn] = curBid +1;
 _lastSent[conn] = current.ToDictionary(kv => kv.Key, kv => (kv.Value.Item1, kv.Value.Item2));
 _deltaMessages++;
 _deltaReplacementsTotal += replacements.Count;
 _deltaRemovesTotal += removes.Length;
 }
 }
 }
 private void SendAuthChallenge(ConnectionId conn)
 {
 var nonce = RandomNumberGenerator.GetInt32(int.MaxValue);
 var challenge = new AuthChallenge(nonce, Array.Empty<byte>()).Serialize(); var buf = new byte[1 + challenge.Length]; buf[0] = (byte)MessageId.AuthChallenge; challenge.CopyTo(buf.AsSpan(1)); Enqueue(conn, buf); _nonces[conn] = nonce;
 }
 private bool ValidateAuth(ConnectionId conn, ReadOnlySpan<byte> payload)
 {
 var resp = AuthResponse.Deserialize(payload);
 if (!_nonces.TryGetValue(conn, out var n)) { _authFailures++; return false; }
 var now = Environment.TickCount64; if (_oldAuthKeys.Count >0) _oldAuthKeys.RemoveAll(k => k.expiresAt <= now);
 bool ok = false;
 foreach (var key in EnumerateAuthKeys())
 {
 using var h = new HMACSHA256(key);
 var mac = h.ComputeHash(BitConverter.GetBytes(resp.Nonce));
 if (resp.Nonce == n && CryptographicOperations.FixedTimeEquals(mac, resp.Hmac)) { ok = true; break; }
 }
 if (ok) { _authed[conn] = true; _nonces.Remove(conn); _authSuccess++; }
 else { _dropsUnauth++; _authFailures++; }
 return ok;
 }
 private IEnumerable<byte[]> EnumerateAuthKeys()
 {
 yield return _authKey; foreach (var (key, _) in _oldAuthKeys) yield return key;
 }

 public void Poll()
 {
 Pump();
 _transport.Poll((conn, data) =>
 {
 if (data.Length ==0) return;
 if (data.Length > _inboundCap) { return; }
 var id = (MessageId)data.Span[0]; var payload = data.Slice(1);
 _rxCounts[id] = _rxCounts.TryGetValue(id, out var c0) ? c0 +1 :1;
 _rxBytes[id] = _rxBytes.TryGetValue(id, out var b0) ? b0 + data.Length : data.Length;
 if (id == MessageId.Heartbeat) { _lastSeen[conn] = Environment.TickCount64; return; }
 if (!_authed.TryGetValue(conn, out var isAuthed) || !isAuthed)
 {
 if (id == MessageId.JoinRequest)
 {
 if (!Allow(conn, _joinWindow, _maxJoinPerSecond)) { _dropsRateLimited++; return; }
 _lastSeen[conn] = Environment.TickCount64; EnsureConnState(conn); SendAuthChallenge(conn); return;
 }
 if (id == MessageId.AuthResponse)
 {
 if (!Allow(conn, _authRespWindow, _maxAuthResponsesPerSecond)) { _dropsRateLimited++; return; }
 if (ValidateAuth(conn, payload.Span))
 {
 var cid = _nextId++; _lastSeen[conn] = Environment.TickCount64; EnsureConnState(conn);
 var acc = new JoinAccepted(cid).Serialize(); var buf = new byte[1 + acc.Length]; buf[0] = (byte)MessageId.JoinAccepted; acc.CopyTo(buf.AsSpan(1)); Enqueue(conn, buf); _connToEntity[conn] = cid; _world[cid] = (0,0); IndexEntity(cid,0,0); SendAdvertise(conn);
 _sink?.OnClientJoin(cid);
 }
 return;
 }
 _dropsUnauth++; return;
 }
 if (id == MessageId.JoinRequest) { return; }
 if (id == MessageId.ModSetAck)
 {
 EnsureConnState(conn);
 var ack = ModSetAck.Deserialize(payload.Span);
 if (ack.Need.Length ==0) { SendStartLoading(conn); return; }
 foreach (var need in ack.Need)
 {
 byte[] bytes = _packs is not null ? _packs.GetPackBytes(need.Id, need.Sha256) : Encoding.UTF8.GetBytes("HELLO CLIENT PACK");
 _streams[conn].Add((need.Id, need.Sha256, bytes,0));
 }
 }
 else if (id == MessageId.InputCmd)
 {
 EnsureConnState(conn);
 if (!Allow(conn, _inputWindow, _maxInputsPerSecond)) { _dropsRateLimited++; return; }
 var cmd = InputCmd.Deserialize(payload.Span);
 _lastInputTick[conn] = cmd.Tick;
 _lastSeen[conn] = Environment.TickCount64; // liveness
 _sink?.OnInput(cmd.ClientId, cmd.Tick, cmd.Action);
 // snapshot now periodic
 }
 else if (id == MessageId.RpcEvent)
 {
 EnsureConnState(conn);
 if (!Allow(conn, _rpcWindow, _maxRpcPerSecond)) { _dropsRateLimited++; return; }
 if (payload.Length > _maxRpcBytes) { _dropsOversize++; return; }
 if (!AllowBytes(conn, _rpcCapWindow, _maxRpcBytesPerSecond, payload.Length)) { _dropsRateLimited++; return; }
 // fan-out unreliable
 var outbound = data.ToArray();
 foreach (var other in _transport.Connections)
 {
 if (other.Equals(conn)) continue;
 if (!_authed.TryGetValue(other, out var ok2) || !ok2) continue;
 Enqueue(other, outbound);
 }
 }
 else if (id == MessageId.Reliable)
 {
 EnsureConnState(conn); EnsureReliableState(conn);
 var rel = Reliable.Deserialize(payload.Span);
 var ack = new ReliableAck(rel.Seq).Serialize(); var abuf = new byte[1 + ack.Length]; abuf[0] = (byte)MessageId.ReliableAck; ack.CopyTo(abuf.AsSpan(1)); SendWithChaos(conn, abuf);
 if (!MarkReliableSeen(conn, rel.Seq)) return;
 var innerId = (MessageId)rel.InnerId;
 if (innerId == MessageId.RpcEvent)
 {
 var innerSpan = rel.Payload.AsSpan();
 if (!Allow(conn, _rpcWindow, _maxRpcPerSecond)) { _dropsRateLimited++; return; }
 if (innerSpan.Length > _maxRpcBytes) { _dropsOversize++; return; }
 if (!AllowBytes(conn, _rpcCapWindow, _maxRpcBytesPerSecond, innerSpan.Length)) { _dropsRateLimited++; return; }
 // reliable fan-out
 foreach (var other in _transport.Connections)
 {
 if (other.Equals(conn)) continue;
 if (!_authed.TryGetValue(other, out var ok2) || !ok2) continue;
 SendReliable(other, (byte)MessageId.RpcEvent, rel.Payload ?? Array.Empty<byte>());
 }
 }
 return;
 }
 else if (id == MessageId.ReliableAck)
 {
 EnsureReliableState(conn);
 var ra = ReliableAck.Deserialize(payload.Span);
 if (_relWindow.TryGetValue(conn, out var win))
 {
 if (win.TryGetValue(ra.AckSeq, out var entry))
 {
 var rtt = Math.Max(0, Environment.TickCount64 - entry.sentAt);
 if (!_relRttSamples.TryGetValue(conn, out var q)) { q = new Queue<long>(64); _relRttSamples[conn] = q; }
 q.Enqueue(rtt); while (q.Count >64) q.Dequeue();
 }
 win.Remove(ra.AckSeq);
 }
 var cur = _relAcked[conn]; if (ra.AckSeq > cur) _relAcked[conn] = ra.AckSeq;
 return;
 }
 else if (id == MessageId.SnapshotAck)
 {
 EnsureConnState(conn);
 var ack = SnapshotAck.Deserialize(payload.Span);
 _lastSnapAck[conn] = (ack.Tick, ack.Hash);
 _rxSnapshotAcks++;
 _lastSeen[conn] = Environment.TickCount64; // liveness
 if (_sentSnapHashes.TryGetValue(conn, out var map) && map.TryGetValue(ack.Tick, out var h) && h != ack.Hash) _rxSnapshotAckMismatches++;
 if (_sentSnapHashes.TryGetValue(conn, out var hashMap))
 {
 hashMap.Remove(ack.Tick);
 if (hashMap.Count >64)
 {
 foreach (var k in hashMap.Keys.OrderBy(x => x).Take(hashMap.Count -64).ToArray()) hashMap.Remove(k);
 }
 }
 return;
 }
 });
 }
 private static bool Allow(ConnectionId c, Dictionary<ConnectionId, (int count, long start)> window, int limit)
 {
 var now = Environment.TickCount64;
 (int count, long start) state;
 if (!window.TryGetValue(c, out state)) state = (0, now);
 if (now - state.start >1000) { window[c] = (0, now); return true; }
 if (state.count >= limit) return false;
 window[c] = (state.count +1, state.start); return true;
 }
 private static bool AllowBytes(ConnectionId c, Dictionary<ConnectionId, (int count, long start)> window, int limitBytes, int toAdd)
 {
 var now = Environment.TickCount64;
 (int count, long start) state;
 if (!window.TryGetValue(c, out state)) state = (0, now);
 if (now - state.start >1000) { window[c] = (0, now); state = (0, now); }
 var next = state.count + toAdd;
 if (next > limitBytes) return false;
 window[c] = (next, state.start);
 return true;
 }

 public bool TryGetPosition(int id, out float x, out float y)
 { if (_world.TryGetValue(id, out var p)) { x = p.x; y = p.y; return true; } x =0; y =0; return false; }
 public IEnumerable<(int id, float x, float y)> GetEntitiesNear(int centerId, float radius)
 {
 if (!_world.TryGetValue(centerId, out var c)) yield break; var r2 = radius * radius;
 foreach (var kv in _world) { var dx = kv.Value.x - c.x; var dy = kv.Value.y - c.y; if (dx * dx + dy * dy <= r2) yield return (kv.Key, kv.Value.x, kv.Value.y); }
 }
}
