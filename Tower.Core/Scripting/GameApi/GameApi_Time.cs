using MoonSharp.Interpreter;

namespace Tower.Core.Scripting.GameApi;

public sealed partial class GameApi
{
 private Func<int>? _getTick;
 private Func<double>? _getSeconds;
 /// <summary>Injects time providers (server controlled).</summary>
 public void SetTimeProvider(Func<int> getTick, Func<double> getSeconds)
 {
 _getTick = getTick ?? throw new ArgumentNullException(nameof(getTick));
 _getSeconds = getSeconds ?? throw new ArgumentNullException(nameof(getSeconds));
 }
 /// <summary>Returns current server tick. Server-only.</summary>
 public int TimeTick()
 {
 sideGate.EnsureServer("Time.Tick");
 return _getTick is null ?0 : _getTick();
 }
 /// <summary>Returns current server time in seconds. Server-only.</summary>
 public double TimeSeconds()
 {
 sideGate.EnsureServer("Time.Seconds");
 return _getSeconds is null ?0.0 : _getSeconds();
 }
}
