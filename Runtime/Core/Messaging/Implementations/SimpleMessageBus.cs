using System;
using System.Collections.Concurrent;

namespace ZenECS.Core.Messaging
{
    public sealed class SimpleMessageBus : IMessageBus
    {
        private readonly ConcurrentDictionary<Type, object> channels = new();

        public void Publish<T>(in T message)
        {
            var ch = (Channels.RingBuffer<T>)channels.GetOrAdd(typeof(T), _ => new Channels.RingBuffer<T>(1024));
            ch.Push(message);
        }

        public void ConsumeAll<T>(Action<T> handler)
        {
            if (channels.TryGetValue(typeof(T), out var obj))
            {
                var ch = (Channels.RingBuffer<T>)obj;
                while (ch.TryPop(out var msg)) handler(msg);
            }
        }

        public ISubscription Subscribe<T>(Action<T> handler, IMessageFilter? filter = null)
        {
            // 간단: 폴링 기반. 드라이버에서 주기적으로 Drain하도록 사용할 수 있음.
            return new Channels.Mailbox<T>(this, handler, filter);
        }
    }
}