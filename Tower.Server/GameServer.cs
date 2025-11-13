using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;
using Serilog;
using Tower.Core.Modding;
using Tower.Net.Abstractions;
using Tower.Net.Clock;
using Tower.Net.Session;
using Tower.Core.Scripting.Net;
using MoonSharp.Interpreter;
using System.Text.Json;
using System.Net;

namespace Tower.Server;

public sealed class GameServer
{
 private readonly EventBus _bus = new();
 private readonly SystemRegistry _systems = new();
 private readonly TimerService _timers;
 private bool _running;
 private Tower.Core.Scripting.LuaRuntime? _lua;
 private Tower.Core.Scripting.GameApi.GameApi? _api;
 private Tower.Core.Engine.Assets.AssetService? _assets;
 private Tower.Core.Modding.ModBootstrapper? _mods;
 private ServerModPackProvider? _packProvider;
 private ITransport? _transport;
 private int _seed =12345;
 private int _tickRate =20;
 private FixedTickClock? _clock;
 private NetServer? _server;
 private (string path, int intervalSec)? _metricsCfg;
 private DateTime _nextMetricsAt;
 private int? _metricsLogIntervalSec;
 private DateTime _nextMetricsLogAt;
 private Thread? _httpThread;

 public GameServer() { _timers = new TimerService(_bus); }

 public void AttachLua(Tower.Core.Scripting.LuaRuntime lua) { _lua = lua; _systems.SetOnTickStart(() => _lua.SetInstructionBudget(200000)); }
 public void SetTransport(ITransport transport) { _transport = transport; }
 public void SetWorldSeed(int seed) { _seed = seed; }
 public void SetTickRate(int tickRate) { _tickRate = tickRate; }

 public void Run()
 {
 if (_api is null)
 {
 _assets = new Tower.Core.Engine.Assets.AssetService();
 _api = new Tower.Core.Scripting.GameApi.GameApi(_assets, _bus, _systems, _timers);
 _api.SetSideGate(new Tower.Core.Engine.Prefabs.SideGate(true)); // server
 _lua = new Tower.Core.Scripting.LuaRuntime(_api);
 AttachLua(_lua);
 _mods = new Tower.Core.Modding.ModBootstrapper(_assets, _lua, _api);
 var content = Path.Combine(AppContext.BaseDirectory, "Content");
 var scanned = new List<(ModMetadata meta, string root)>();
 var basePath = Path.Combine(content, "BaseGame"); if (Directory.Exists(basePath)) { var m = ModMetadata.FromFile(Path.Combine(basePath, "modinfo.json")); if (m is not null) scanned.Add((m, basePath)); }
 var modsPath = Path.Combine(content, "Mods"); if (Directory.Exists(modsPath)) foreach (var dir in Directory.GetDirectories(modsPath)) { var m = ModMetadata.FromFile(Path.Combine(dir, "modinfo.json")); if (m is not null) scanned.Add((m, dir)); }
 var cachePath = Path.Combine(content, "Cache", "Mods"); if (Directory.Exists(cachePath)) foreach (var dir in Directory.GetDirectories(cachePath)) { var m = ModMetadata.FromFile(Path.Combine(dir, "modinfo.json")); if (m is not null) scanned.Add((m, dir)); }
 _packProvider = new ServerModPackProvider(scanned);
 _mods.LoadAll(content, executeScripts:true, clientMode:false);
 }

 _running = true;
 _server = new NetServer(_transport ?? new Tower.Net.Transport.LoopbackTransport());
 var cfgPath = Environment.GetEnvironmentVariable("TOWER_NETCFG") ?? Path.Combine(AppContext.BaseDirectory, "netconfig.json");
 var cfg = NetConfigJson.Load(cfgPath);
 if (cfg is not null)
 {
 cfg.ApplyTo(_server);
 Log.Information("Applied NetServer config from {Path}", cfgPath);
 if (!string.IsNullOrEmpty(cfg.MetricsDumpPath) && cfg.MetricsDumpIntervalSeconds.HasValue)
 { _metricsCfg = (cfg.MetricsDumpPath!, cfg.MetricsDumpIntervalSeconds.Value); _nextMetricsAt = DateTime.UtcNow.AddSeconds(_metricsCfg.Value.intervalSec); }
 if (cfg.MetricsLogIntervalSeconds.HasValue && cfg.MetricsLogIntervalSeconds.Value >0)
 { _metricsLogIntervalSec = cfg.MetricsLogIntervalSeconds.Value; _nextMetricsLogAt = DateTime.UtcNow.AddSeconds(_metricsLogIntervalSec.Value); }
 }
 _server.SetWorldSeed(_seed);
 _server.SetTickRate(_tickRate);
 _clock = new FixedTickClock(_tickRate);
 _server.SetClock(_clock);
 _server.SetModPackProvider(_packProvider!);
 _api!.SetWorldPublisher(_server);
 _api.SetEntityRegistry(_server);
 // Optional Lua hook: function on_input(cid, tick, action)
 var onInput = _lua!.Script.Globals.Get("on_input");
 if (onInput.Type == MoonSharp.Interpreter.DataType.Function)
 {
 var bridge = new LuaNetBridge(_lua.Script, onInput);
 _server.SetGameplaySink(bridge);
 }
 // wire save/load provider
 _server.SetModStateProvider(new GameApiModStateProvider(_api));

 // optional lightweight HTTP for health/metrics
 var httpUrl = Environment.GetEnvironmentVariable("TOWER_HTTP");
 if (!string.IsNullOrWhiteSpace(httpUrl))
 {
 _httpThread = new Thread(() => HttpServe(httpUrl!, _server!)) { IsBackground = true };
 _httpThread.Start();
 }

 _server.Start();
 _api.SetRandomSeed(_seed);
 _api.SetTimeProvider(() => _clock!.Tick, () => _clock!.NowMs /1000.0);
 while (_running)
 {
 _timers.Update(1.0/_tickRate);
 _systems.Update(1.0/_tickRate);
 _server.Poll();
 if (_metricsCfg.HasValue && DateTime.UtcNow >= _nextMetricsAt)
 {
 var metrics = _server.GetMetrics();
 Directory.CreateDirectory(Path.GetDirectoryName(_metricsCfg.Value.path)!);
 File.WriteAllText(_metricsCfg.Value.path, JsonSerializer.Serialize(metrics));
 _nextMetricsAt = DateTime.UtcNow.AddSeconds(_metricsCfg.Value.intervalSec);
 }
 if (_metricsLogIntervalSec.HasValue && DateTime.UtcNow >= _nextMetricsLogAt)
 {
 var m = _server.GetMetrics();
 Log.Information("NET m:rx={rx} tx={tx} drop[u={u} rl={rl} os={os} sb={sb}] aoi[msg={am} ents={ae} cand={ac}] delta[msg={dm} rep={dr} rem={dv}] auth[s={as} f={af}]",
 m.Rx.Values.Sum(), m.Tx.Values.Sum(), m.DroppedUnauth, m.DroppedRateLimited, m.DroppedOversize, m.DroppedSnapshotBudget,
 m.SnapshotMessages, m.SnapshotEntitiesTotal, m.SnapshotCandidatesTotal,
 m.DeltaMessages, m.DeltaReplacementsTotal, m.DeltaRemovesTotal,
 m.AuthSuccess, m.AuthFailures);
 _nextMetricsLogAt = DateTime.UtcNow.AddSeconds(_metricsLogIntervalSec.Value);
 }
 Thread.Sleep(1000/_tickRate);
 }
 }

 private static void HttpServe(string url, NetServer server)
 {
 try
 {
 using var listener = new HttpListener();
 listener.Prefixes.Add(url.EndsWith("/") ? url : url + "/");
 listener.Start();
 while (true)
 {
 var ctx = listener.GetContext();
 try
 {
 if (ctx.Request.Url is null) { ctx.Response.StatusCode =400; ctx.Response.Close(); continue; }
 var path = ctx.Request.Url.AbsolutePath;
 if (path == "/health")
 {
 ctx.Response.StatusCode =200; using var sw = new StreamWriter(ctx.Response.OutputStream); sw.Write("OK"); ctx.Response.Close();
 }
 else if (path == "/metrics")
 {
 var json = JsonSerializer.Serialize(server.GetMetrics());
 ctx.Response.StatusCode =200; ctx.Response.ContentType = "application/json"; using var sw = new StreamWriter(ctx.Response.OutputStream); sw.Write(json); ctx.Response.Close();
 }
 else
 {
 ctx.Response.StatusCode =404; ctx.Response.Close();
 }
 }
 catch { /* ignore per-request errors */ }
 }
 }
 catch (Exception ex)
 {
 Log.Warning(ex, "HTTP server terminated");
 }
 }
}

internal sealed class GameApiModStateProvider : IModStateProvider
{
 private readonly Tower.Core.Scripting.GameApi.GameApi _api;
 public GameApiModStateProvider(Tower.Core.Scripting.GameApi.GameApi api) { _api = api; }
 public Dictionary<string, object?> CollectSaveState() => _api.CollectSaveState();
 public void ApplyLoadState(Dictionary<string, object?> state) => _api.ApplyLoadState(state);
}
