using Tower.Net.Abstractions;
using Tower.Net.Protocol;
using Tower.Net.Protocol.Messages;
using Serilog;
using System.Security.Cryptography;

namespace Tower.Net.Session;

public sealed class NetClient
{
 private readonly ITransport _transport;
 private readonly IModPackCache? _cache;
 private readonly IRevisionCache? _revCache;
 public int ClientId { get; private set; }
 public (int tick, int entityId, float x, float y) LastSnapshot { get; private set; }
 public (int tick, int entityId, float x, float y)? PrevSnapshot { get; private set; }
 private ModAdvert[] _advertised = Array.Empty<ModAdvert>();
 private int _contentRevision;
 private readonly Dictionary<(string id,string sha), List<byte>> _chunks = new();
 private readonly HashSet<(string id,string sha)> _done = new();
 public bool LoadingStarted { get; private set; }
 private readonly IModPackConsumer? _consumer;
 private long _lastHbSent; private long _lastHbRecv; private int _hbTimeoutMs =5000; private int _hbIntervalMs =1000;
 public bool Disconnected { get; private set; }
 public int LastProcessedInputTick { get; private set; }
 public IReadOnlyList<(int id, float x, float y)> LastEntities { get; private set; } = Array.Empty<(int, float, float)>();
 private readonly List<(int tick, string action)> _pendingInputs = new();
 public int PendingInputCount => _pendingInputs.Count;
 // reconnect state (disabled by default)
 private bool _autoReconnect;
 private string _lastName = string.Empty;
 private int _reconnectBackoffMs =1000; private int _reconnectMaxMs =8000; private long _nextReconnectAt;
 private byte[] _authKey = System.Text.Encoding.UTF8.GetBytes("dev-shared-secret");
 private bool _authed;
 public void SetAuthKey(byte[] key) { _authKey = key ?? Array.Empty<byte>(); }
 // Multi-entity client state
 private Dictionary<int,(float x,float y)> _entities = new();
 private Dictionary<int,(float x,float y)> _prevEntities = new();
 public IReadOnlyDictionary<int,(float x,float y)> Entities => _entities;
 public bool TryGetEntity(int id, out (float x,float y) pos) => _entities.TryGetValue(id, out pos);
 // Component replication store
 private readonly Dictionary<int, Dictionary<(string modNs,string typeName), string>> _components = new();
 private bool _compBaselineReceived;
 // Reliable channel fields
 private uint _relSendSeq;
 private uint _relAckedSeq;
 private readonly Dictionary<uint, (byte id, byte[] payload, long sentAt, int retries)> _relWindow = new();
 private int _relRtoMs =250; private int _relMaxRetries =5;
 public void SetReliableParams(int rtoMs, int maxRetries) { _relRtoMs = Math.Clamp(rtoMs,50,5000); _relMaxRetries = Math.Clamp(maxRetries,1,20); }
 // Prediction/reconciliation
 private bool _predictEnabled = true;
 private bool _smoothCorrections = true;
 private float _maxCorrectionStep =1.0f; // units per update
 private (float x,float y) _predictedSelf;
 private (float x,float y) _prevPredictedSelf;
 private bool _havePredicted;
 public void SetPredictionOptions(bool enabled, bool smooth, float maxCorrectionStep)
 { _predictEnabled = enabled; _smoothCorrections = smooth; _maxCorrectionStep = Math.Max(0f, maxCorrectionStep); }
 private static void ApplyAction(string action, ref float x, ref float y)
 {
 switch (action)
 {
 case "MoveRight": x +=1; break;
 case "MoveLeft": x -=1; break;
 case "MoveUp": y -=1; break;
 case "MoveDown": y +=1; break;
 }
 }
 public bool TryGetDisplayPosition(int id, float alpha, out (float x,float y) pos)
 {
 if (_predictEnabled && id == ClientId && _havePredicted)
 { pos = _predictedSelf; return true; }
 // fallback to interpolation
 pos = GetEntityInterpolated(id, alpha); return true;
 }
 public (float x,float y) GetEntityInterpolated(int id, float alpha)
 {
 alpha = Math.Clamp(alpha,0f,1f);
 if (_entities.TryGetValue(id, out var cur))
 {
 if (_prevEntities.TryGetValue(id, out var prev))
 {
 return (prev.x + (cur.x - prev.x) * alpha, prev.y + (cur.y - prev.y) * alpha);
 }
 return cur;
 }
 return (0,0);
 }

 private int _baselineId;
 public NetClient(ITransport transport, IModPackConsumer? consumer = null, IModPackCache? cache = null, IRevisionCache? revCache = null)
 { _transport = transport; _consumer = consumer; _cache = cache; _revCache = revCache; }
 public void SetHeartbeatTimeout(int ms) { _hbTimeoutMs = Math.Max(500, ms); }
 public void Connect(string name)
 {
 _lastName = name ?? string.Empty;
 _transport.Connect();
 var req = new JoinRequest(name).Serialize();
 var buf = new byte[1 + req.Length];
 buf[0] = (byte)MessageId.JoinRequest;
 req.CopyTo(buf.AsSpan(1));
 _transport.Send(buf);
 var now = Environment.TickCount64;
 _lastHbSent = now; _lastHbRecv = now; Disconnected = false; _nextReconnectAt = now + _reconnectBackoffMs; _authed = false;
 }
 public void Poll()
 {
 var now = Environment.TickCount64;
 // retransmit pending reliable
 foreach (var kv in _relWindow.ToArray())
 {
 var seq = kv.Key; var (id, payload, sentAt, retries) = kv.Value;
 if (now - sentAt >= _relRtoMs && retries < _relMaxRetries)
 {
 var rel = new Reliable(seq, id, payload).Serialize(); var buf = new byte[1 + rel.Length]; buf[0] = (byte)MessageId.Reliable; rel.CopyTo(buf.AsSpan(1)); _transport.Send(buf);
 _relWindow[seq] = (id, payload, now, retries +1);
 }
 }
 // auto-reconnect if enabled and timed out
 if (_autoReconnect && Disconnected && now >= _nextReconnectAt)
 {
 Serilog.Log.Warning("NetClient attempting reconnect...");
 try { _transport.Disconnect(); } catch { }
 _transport.Connect();
 if (!string.IsNullOrEmpty(_lastName))
 {
 var req2 = new JoinRequest(_lastName).Serialize(); var b2 = new byte[1+req2.Length]; b2[0]=(byte)MessageId.JoinRequest; req2.CopyTo(b2.AsSpan(1)); _transport.Send(b2);
 }
 _lastHbSent = now; _lastHbRecv = now; // reset timers
 _nextReconnectAt = now + _reconnectBackoffMs;
 _reconnectBackoffMs = Math.Min(_reconnectBackoffMs *2, _reconnectMaxMs);
 }
 // send heartbeat at fixed interval
 if (now - _lastHbSent > _hbIntervalMs)
 {
 var hb = new byte[1]; hb[0] = (byte)MessageId.Heartbeat; _transport.Send(hb);
 _lastHbSent = now;
 }
 // detect timeout
 if (!Disconnected && now - _lastHbRecv > _hbTimeoutMs)
 {
 Disconnected = true; Serilog.Log.Warning("NetClient heartbeat timeout");
 }
 _transport.Poll(data =>
 {
 var id = (MessageId)data.Span[0];
 var payload = data.Slice(1);
 if (id == MessageId.Compressed)
 {
 var comp = Compressed.Deserialize(payload.Span);
 var innerBuf = new byte[1 + comp.Payload.Length]; innerBuf[0] = comp.InnerId; comp.Payload.CopyTo(innerBuf.AsSpan(1));
 // re-dispatch by invoking a nested Poll handler on the decompressed message
 HandleMessage(innerBuf);
 return;
 }
 if (id == MessageId.Reliable)
 {
 var rel = Reliable.Deserialize(payload.Span);
 // ack
 var ack = new ReliableAck(rel.Seq).Serialize(); var ab = new byte[1 + ack.Length]; ab[0] = (byte)MessageId.ReliableAck; ack.CopyTo(ab.AsSpan(1)); _transport.Send(ab);
 // deliver inner
 var inner = new byte[1 + rel.Payload.Length]; inner[0] = rel.InnerId; rel.Payload.CopyTo(inner.AsSpan(1));
 HandleMessage(inner);
 return;
 }
 if (id == MessageId.ReliableAck)
 {
 var ra = ReliableAck.Deserialize(payload.Span);
 if (_relWindow.Remove(ra.AckSeq)) _relAckedSeq = Math.Max(_relAckedSeq, ra.AckSeq);
 return;
 }
 HandleMessage(data.ToArray());
 });
 }

 private void HandleMessage(ReadOnlySpan<byte> data)
 {
 var id = (MessageId)data[0];
 var payload = data.Slice(1);
 // any message counts as activity
 _lastHbRecv = Environment.TickCount64; if (Disconnected) Disconnected = false;
 if (id == MessageId.Heartbeat) { return; }
 if (!_authed)
 {
 if (id == MessageId.AuthChallenge)
 {
 var ch = AuthChallenge.Deserialize(payload);
 using var h = new HMACSHA256(_authKey);
 var mac = h.ComputeHash(BitConverter.GetBytes(ch.Nonce));
 var resp = new AuthResponse(ch.Nonce, mac).Serialize(); var rbuf = new byte[1+resp.Length]; rbuf[0]=(byte)MessageId.AuthResponse; resp.CopyTo(rbuf.AsSpan(1)); _transport.Send(rbuf);
 return;
 }
 if (id == MessageId.JoinAccepted)
 {
 var acc = JoinAccepted.Deserialize(payload); ClientId = acc.ClientId; _authed = true; _reconnectBackoffMs =1000; Serilog.Log.Information("JoinAccepted: {ClientId}", ClientId); return;
 }
 return;
 }
 // ...existing authed handlers (advertise/mods/snapshot/etc)...
 if (id == MessageId.JoinAccepted)
 {
 var acc = JoinAccepted.Deserialize(payload);
 ClientId = acc.ClientId; _reconnectBackoffMs =1000; Serilog.Log.Information("JoinAccepted: {ClientId}", ClientId);
 }
 else if (id == MessageId.ModSetAdvertise)
 {
 var adv = ModSetAdvertise.Deserialize(payload);
 _contentRevision = adv.ContentRevision; _advertised = adv.Mods; _consumer?.OnAdvertised(_advertised);
 if (_revCache?.IsFresh(adv.ContentRevision, adv.Mods) == true)
 {
 var ack = new ModSetAck(Array.Empty<ModNeed>()).Serialize(); var abuf = new byte[1 + ack.Length]; abuf[0]=(byte)MessageId.ModSetAck; ack.CopyTo(abuf.AsSpan(1)); _transport.Send(abuf);
 }
 else
 {
 var need = _advertised.Where(m => _cache?.Has(m.Id, m.Sha256) != true).Select(m => new ModNeed(m.Id, m.Version, m.Sha256)).ToArray();
 var ack = new ModSetAck(need).Serialize(); var abuf = new byte[1 + ack.Length]; abuf[0]=(byte)MessageId.ModSetAck; ack.CopyTo(abuf.AsSpan(1)); _transport.Send(abuf);
 }
 }
 else if (id == MessageId.ModChunk)
 {
 var chunk = ModChunk.Deserialize(payload); var key = (chunk.Id, chunk.Sha256); if (!_chunks.TryGetValue(key, out var list)) { list = []; _chunks[key] = list; } list.AddRange(chunk.Bytes);
 }
 else if (id == MessageId.ModDone)
 {
 var md = ModDone.Deserialize(payload); var key = (md.Id, md.Sha256); _done.Add(key);
 if (_chunks.TryGetValue(key, out var list))
 {
 var bytes = list.ToArray();
 try
 {
 using var sha = SHA256.Create(); var hex = Convert.ToHexString(sha.ComputeHash(bytes)).ToLowerInvariant();
 var advMod = _advertised.FirstOrDefault(m => string.Equals(m.Id, md.Id, StringComparison.Ordinal) && string.Equals(m.Sha256, md.Sha256, StringComparison.OrdinalIgnoreCase));
 if (!string.IsNullOrEmpty(advMod.Id) && !string.Equals(hex, advMod.Sha256, StringComparison.OrdinalIgnoreCase)) Serilog.Log.Warning("Discarding pack {Id}: SHA mismatch adv={Adv} got={Got}", md.Id, advMod.Sha256, hex);
 else _consumer?.OnPackComplete(md.Id, md.Sha256, bytes);
 }
 catch (Exception ex) { Serilog.Log.Error(ex, "Pack verification failed for {Id}@{Sha}", md.Id, md.Sha256); }
 finally { _chunks.Remove(key); }
 }
 }
 else if (id == MessageId.StartLoading)
 {
 var start = StartLoading.Deserialize(payload);
 LoadingStarted = true;
 _revCache?.Save(_contentRevision, _advertised);
 _consumer?.OnStartLoading(start);
 Serilog.Log.Information("StartLoading seed={Seed} tickRate={Rate} world={World}", start.Seed, start.TickRate, start.WorldId);
 }
 else if (id == MessageId.SnapshotSet)
 {
 var set = SnapshotSet.Deserialize(payload);
 LastProcessedInputTick = set.LastProcessedInputTick;
 if (_pendingInputs.Count !=0) _pendingInputs.RemoveAll(pi => pi.tick <= LastProcessedInputTick);
 // authoritative world set
 var list = new List<(int id, float x, float y)>(set.Entities.Length);
 foreach (var e in set.Entities) list.Add((e.Id, e.X, e.Y));
 LastEntities = list;
 _prevEntities = _entities;
 _entities = new Dictionary<int,(float x,float y)>(set.Entities.Length);
 foreach (var e in set.Entities) _entities[e.Id] = (e.X, e.Y);
 // Update self snapshot info used by legacy methods
 if (_entities.Count >0)
 {
 if (LastSnapshot.entityId !=0) PrevSnapshot = LastSnapshot;
 var selfEntry = _entities.TryGetValue(ClientId, out var selfPos) ? (ClientId, selfPos.x, selfPos.y) : (list.First().id, list.First().x, list.First().y);
 LastSnapshot = (set.Tick, selfEntry.Item1, selfEntry.Item2, selfEntry.Item3);
 }
 // Prediction/reconciliation for owned entity
 if (_predictEnabled)
 {
 if (_entities.TryGetValue(ClientId, out var serverSelf))
 {
 _prevPredictedSelf = _havePredicted ? _predictedSelf : serverSelf;
 // compute target predicted by reapplying residual inputs
 float px = serverSelf.x, py = serverSelf.y;
 foreach (var (tick, action) in _pendingInputs) { ApplyAction(action, ref px, ref py); }
 var target = (x: px, y: py);
 if (_smoothCorrections)
 {
 // move current predicted towards target by at most max step
 var dx = target.x - _prevPredictedSelf.x; var dy = target.y - _prevPredictedSelf.y;
 var dist = MathF.Sqrt(dx*dx + dy*dy);
 if (dist <= _maxCorrectionStep || _maxCorrectionStep <=0f) _predictedSelf = target;
 else { var r = _maxCorrectionStep / dist; _predictedSelf = (_prevPredictedSelf.x + dx * r, _prevPredictedSelf.y + dy * r); }
 }
 else
 {
 _predictedSelf = target;
 }
 _havePredicted = true;
 }
 }
 }
 else if (id == MessageId.RpcEvent)
 {
 var ev = RpcEvent.Deserialize(payload);
 Serilog.Log.Information("RpcEvent: {Event} {Payload}", ev.Event, ev.Payload);
 }
 else if (id == MessageId.Snapshot)
 {
 var snap = Snapshot.Deserialize(payload);
 if (LastSnapshot.entityId !=0) PrevSnapshot = LastSnapshot;
 LastSnapshot = (snap.Tick, snap.EntityId, snap.X, snap.Y);
 Serilog.Log.Information("Snapshot: t={Tick} id={Id} pos=({X},{Y})", snap.Tick, snap.EntityId, snap.X, snap.Y);
 }
 else if (id == MessageId.SnapshotBaseline)
 {
 var baseMsg = SnapshotBaseline.Deserialize(payload);
 _baselineId = baseMsg.BaselineId;
 _prevEntities = _entities;
 _entities = new Dictionary<int,(float x,float y)>(baseMsg.Entities.Length);
 foreach (var e in baseMsg.Entities) _entities[e.Id] = (e.X, e.Y);
 }
 else if (id == MessageId.SnapshotDelta)
 {
 var delta = SnapshotDelta.Deserialize(payload);
 if (delta.BaselineId != _baselineId) { Serilog.Log.Warning("Delta baseline mismatch: got {Got} expected {Exp}", delta.BaselineId, _baselineId); return; }
 foreach (var e in delta.Replacements) _entities[e.Id] = (e.X, e.Y);
 foreach (var eid in delta.Removes) _entities.Remove(eid);
 _baselineId++;
 }
 else if (id == MessageId.CompBaseline)
 {
 var cb = CompBaseline.Deserialize(payload);
 _components.Clear();
 foreach (var e in cb.Entities)
 {
 if (!_components.TryGetValue(e.EntityId, out var map)) { map = new(); _components[e.EntityId] = map; }
 foreach (var c in e.Components) map[(c.ModNs, c.TypeName)] = c.Json ?? string.Empty;
 }
 _compBaselineReceived = true;
 }
 else if (id == MessageId.CompDelta)
 {
 if (!_compBaselineReceived) { Serilog.Log.Warning("CompDelta before baseline; ignoring"); return; }
 var cd = CompDelta.Deserialize(payload);
 foreach (var r in cd.Replacements)
 {
 if (!_components.TryGetValue(r.EntityId, out var map)) { map = new(); _components[r.EntityId] = map; }
 map[(r.ModNs, r.TypeName)] = r.Json ?? string.Empty;
 }
 foreach (var rm in cd.Removes)
 {
 if (_components.TryGetValue(rm.EntityId, out var map)) map.Remove((rm.ModNs, rm.TypeName));
 }
 }
 }

 public void SendRpc(string @event, string payload)
 {
 var rpc = new RpcEvent(@event, payload).Serialize();
 var buf = new byte[1 + rpc.Length];
 buf[0] = (byte)MessageId.RpcEvent;
 rpc.CopyTo(buf.AsSpan(1));
 _transport.Send(buf);
 }
 public void SendInput(string action, int tick)
 {
 var cid = ClientId;
 var cmd = new InputCmd(cid, tick, action).Serialize();
 var buf = new byte[1 + cmd.Length];
 buf[0] = (byte)MessageId.InputCmd;
 cmd.CopyTo(buf.AsSpan(1));
 _transport.Send(buf);
 _pendingInputs.Add((tick, action));
 }

 public void SendReliable(byte innerId, byte[] payload)
 {
 var seq = ++_relSendSeq;
 var rel = new Reliable(seq, innerId, payload ?? Array.Empty<byte>()).Serialize();
 var buf = new byte[1 + rel.Length]; buf[0] = (byte)MessageId.Reliable; rel.CopyTo(buf.AsSpan(1)); _transport.Send(buf);
 _relWindow[seq] = (innerId, payload ?? Array.Empty<byte>(), Environment.TickCount64,0);
 }

 public (float x, float y) GetInterpolated(float alpha)
 {
 alpha = Math.Clamp(alpha,0f,1f);
 if (PrevSnapshot is null) return (LastSnapshot.x, LastSnapshot.y);
 var prev = PrevSnapshot.Value;
 var last = LastSnapshot;
 var x = prev.x + (last.x - prev.x) * alpha;
 var y = prev.y + (last.y - prev.y) * alpha;
 return (x, y);
 }

 public (float x, float y) GetPredicted()
 {
 if (_predictEnabled && _havePredicted) return _predictedSelf;
 var baseX = LastSnapshot.x; var baseY = LastSnapshot.y;
 foreach (var (tick, action) in _pendingInputs)
 {
 switch (action)
 {
 case "MoveRight": baseX +=1; break;
 case "MoveLeft": baseX -=1; break;
 case "MoveUp": baseY -=1; break;
 case "MoveDown": baseY +=1; break;
 }
 }
 return (baseX, baseY);
 }
}
