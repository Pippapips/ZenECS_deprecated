#if ZENECS_UNIRX
using UniRx;
using System;
using System.Collections.Concurrent;
using ZenECS.Core.Messaging;

namespace ZenECS.Adapter.Unity.Rx
{
    /// UniRx + 큐 하이브리드:
    /// - Publish: 큐 Enqueue (프레임 경계 처리용) + broker.Publish (실시간 스트림용)
    /// - ConsumeAll: 큐에서 동기 Drain
    public sealed class UniRxMessageBus : IMessageBus, IDisposable
    {
        private readonly MessageBroker broker = new MessageBroker();
        private readonly ConcurrentDictionary<Type, object> queues = new();

        public void Publish<T>(in T message)
        {
            var q = (ConcurrentQueue<T>)queues.GetOrAdd(typeof(T), _ => new ConcurrentQueue<T>());
            q.Enqueue(message);
            broker.Publish(message); // 스트림 구독자에게도 전달 (옵션)
        }

        public void ConsumeAll<T>(Action<T> handler)
        {
            if (!queues.TryGetValue(typeof(T), out var obj)) return;
            var q = (ConcurrentQueue<T>)obj;
            while (q.TryDequeue(out var msg)) handler(msg);
        }

        public ISubscription Subscribe<T>(Action<T> handler, IMessageFilter filter = null)
        {
            var d = (filter == null)
                ? broker.Receive<T>().Subscribe(handler)
                : broker.Receive<T>().Subscribe(m => { if (filter.Allow(in m)) handler(m); });

            return new Sub(d);
        }

        private sealed class Sub : ISubscription
        {
            private readonly IDisposable _d;
            public Sub(IDisposable d) { _d = d; }
            public void Dispose() => _d.Dispose();
        }

        public void Dispose() => broker?.Dispose();
    }
}
#endif