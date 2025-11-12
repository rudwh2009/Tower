using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;
using Serilog;

namespace Tower.Server;

public sealed class GameServer
{
 private readonly EventBus _bus = new();
 private readonly SystemRegistry _systems = new();
 private readonly TimerService _timers;
 private bool _running;

 public GameServer() { _timers = new TimerService(_bus); }

 public void Run()
 {
 _running = true;
 var sw = System.Diagnostics.Stopwatch.StartNew();
 var last = sw.Elapsed;
 while (_running)
 {
 var now = sw.Elapsed; var dt = (now - last).TotalSeconds; last = now;
 _timers.Update(dt); _systems.Update(dt);
 Log.Information("Tick dt={Dt:F3}s", dt);
 Thread.Sleep(16);
 }
 }
}
