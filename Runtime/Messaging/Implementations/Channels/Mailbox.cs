using System;

namespace ZenECS.Core.Messaging.Channels
{
    internal sealed class Mailbox<T> : ISubscription
    {
        private readonly SimpleMessageBus bus;
        private readonly Action<T> handler;
        private readonly IMessageFilter filter;

        public Mailbox(SimpleMessageBus bus, Action<T> handler, IMessageFilter? filter)
        { this.bus = bus; this.handler = handler; this.filter = filter; }

        public void Dispose() { /* no-op: SimpleMessageBus는 폴링 기반 */ }

        public void Drain(RingBuffer<T> ch)
        {
            while (ch.TryPop(out var msg))
            {
                if (filter == null || filter.Allow(in msg))
                    handler(msg);
            }
        }
    }
}