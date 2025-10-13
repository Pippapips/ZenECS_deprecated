using System;

namespace ZenECS.Core.Messaging
{
    public interface IMessageBus
    {
        void Publish<T>(in T message);
        void ConsumeAll<T>(Action<T> handler);
        ISubscription Subscribe<T>(Action<T> handler, IMessageFilter? filter = null);
    }
}