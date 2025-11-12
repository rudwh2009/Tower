using Tower.Core.Engine.EventBus;
using FluentAssertions;
using Xunit;

public class EventBusTests
{
    [Fact]
    public void Publish_FansOut()
    {
        var bus = new EventBus();
        int c = 0; bus.Subscribe("a", _ => c++); bus.Subscribe("a", _ => c++);
        bus.Publish("a");
        c.Should().Be(2);
    }
}
