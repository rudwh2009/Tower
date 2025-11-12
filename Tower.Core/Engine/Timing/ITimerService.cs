namespace Tower.Core.Engine.Timing;

public interface ITimerService
{
 string Schedule(double delaySeconds, Action callback);
 string Interval(double intervalSeconds, Action callback);
 bool Cancel(string id);
 void Update(double dtSeconds);
}
