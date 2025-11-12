using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Timing;
using FluentAssertions;
using Xunit;

public class TimerServiceTests
{
 [Fact]
 public void Schedule_Fires()
 {
 var bus = new EventBus();
 var svc = new TimerService(bus);
 int fired =0;
 svc.Schedule(0.01, () => fired++);
 svc.Update(0.005); fired.Should().Be(0);
 svc.Update(0.01); fired.Should().Be(1);
 }

 [Fact]
 public void Interval_FiresMultiple()
 {
 var bus = new EventBus(); var svc = new TimerService(bus); int c=0;
 svc.Interval(0.01, ()=> c++);
 svc.Update(0.01); svc.Update(0.01); c.Should().Be(2);
 }
}
