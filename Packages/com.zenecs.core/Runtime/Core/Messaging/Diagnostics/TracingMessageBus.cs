using System;

namespace ZenECS.Core.Messaging.Diagnostics
{
    /// <summary>IMessageBus를 감싸 트레이서를 호출하는 데코레이터</summary>
    public sealed class TracingMessageBus : IMessageBus
    {
        private readonly IMessageBus inner;
        private readonly MessageCounters c;

        public TracingMessageBus(IMessageBus inner, MessageCounters counters)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
            this.c = counters ?? throw new ArgumentNullException(nameof(counters));
        }

        public void Publish<T>(in T message)
        {
            c.IncPublish(typeof(T));
            inner.Publish(message);
        }

        public void ConsumeAll<T>(Action<T> handler)
        {
            inner.ConsumeAll<T>(msg =>
            {
                c.IncConsume(typeof(T));
                handler(msg);
            });
        }

        public ISubscription Subscribe<T>(Action<T> handler, IMessageFilter? filter = null)
        {
            return inner.Subscribe<T>(msg =>
            {
                c.IncConsume(typeof(T));
                handler(msg);
            }, filter);
        }
    }
}