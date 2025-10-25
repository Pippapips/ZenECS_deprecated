using Xunit;
using ZenECS.Core.Messaging;
using ZenECS.Core.Testing;

public sealed class MessageBus_PumpAll
{
    private readonly struct Ping : IMessage { public readonly int N; public Ping(int n){ N = n; } }
    private readonly struct Pong : IMessage { public readonly int N; public Pong(int n){ N = n; } }

    [Fact]
    public void Publish_Subscribe_Pump()
    {
        var bus = new MessageBus();
        var spy = new MessageBusSpy(bus);
        spy.Track<Ping>();
        spy.Track<Pong>();

        // publish a few without immediate delivery
        for (int i = 0; i < 3; i++) bus.Publish(new Ping(i));
        for (int i = 0; i < 2; i++) bus.Publish(new Pong(i));

        // nothing delivered before PumpAll
        Assert.Equal(0, spy.Received<Ping>());
        Assert.Equal(0, spy.Received<Pong>());

        // now flush
        bus.PumpAll();
        Assert.Equal(3, spy.Received<Ping>());
        Assert.Equal(2, spy.Received<Pong>());
    }
}