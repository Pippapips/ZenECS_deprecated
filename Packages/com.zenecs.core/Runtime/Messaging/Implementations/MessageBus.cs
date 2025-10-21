#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ZenECS.Core.Messaging
{
    public sealed class MessageBus : IMessageBus
    {
        interface ITopic
        {
            int Pump();
        }
        sealed class Topic<T> : ITopic where T : struct, IMessage
        {
            private readonly ConcurrentQueue<T> _q = new();
            private readonly List<Action<T>> _subs = new();

            public void Publish(in T m) => _q.Enqueue(m);

            public IDisposable Subscribe(Action<T> h)
            {
                lock (_subs) _subs.Add(h);
                return new Unsub<T>(this, h);
            }

            public int Pump()
            {
                // 구독자 스냅샷(결정적 순서 유지: 등록 순서)
                Action<T>[] handlers;
                lock (_subs) handlers = _subs.ToArray();

                int n = 0;
                while (_q.TryDequeue(out var m))
                {
                    for (int i = 0; i < handlers.Length; i++) handlers[i](m);
                    n++;
                }
                return n;
            }

            private sealed class Unsub<TMsg> : IDisposable where TMsg : struct, IMessage
            {
                private readonly Topic<TMsg> _owner;
                private readonly Action<TMsg> _h;
                public Unsub(Topic<TMsg> owner, Action<TMsg> h)
                {
                    _owner = owner;
                    _h = h;
                }
                public void Dispose()
                {
                    lock (_owner._subs) _owner._subs.Remove(_h);
                }
            }
        }

        private readonly ConcurrentDictionary<Type, ITopic> _topics = new();

        public void Publish<T>(in T msg) where T : struct, IMessage
        {
            ((Topic<T>)_topics.GetOrAdd(typeof(T), _ => new Topic<T>())).Publish(in msg);
        }

        public IDisposable Subscribe<T>(Action<T> handler) where T : struct, IMessage
        {
            return ((Topic<T>)_topics.GetOrAdd(typeof(T), _ => new Topic<T>())).Subscribe(handler);
        }

        public int PumpAll()
        {
            int n = 0;
            foreach (var kv in _topics) n += kv.Value.Pump();
            return n;
        }
    }
}
