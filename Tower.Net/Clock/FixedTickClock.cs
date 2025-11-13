using Tower.Net.Abstractions;

namespace Tower.Net.Clock;

public sealed class FixedTickClock : INetClock
{
 private readonly int _tickRate;
 private long _start;
 public FixedTickClock(int tickRate)
 {
 _tickRate = tickRate;
 _start = Environment.TickCount64;
 }
 public long NowMs => Environment.TickCount64 - _start;
 public int Tick => (int)(NowMs / (1000 / _tickRate));
}
