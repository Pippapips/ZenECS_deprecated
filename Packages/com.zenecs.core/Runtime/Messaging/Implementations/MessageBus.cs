// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: MessageBus.cs
// Purpose: Thread-safe publish/subscribe message dispatcher for ECS systems.
// Key concepts:
//   • Struct-based messages, no boxing or allocations on Publish.
//   • Each message type maintains its own queue and subscriber list.
//   • PumpAll() flushes all message queues per frame (deterministic order).
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ZenECS.Core.Messaging
{
    /// <summary>
    /// Default thread-safe implementation of <see cref="IMessageBus"/>.
    /// </summary>
    public sealed class MessageBus : IMessageBus
    {
        private interface ITopic
        {
            int Pump();
        }

        private sealed class Topic<T> : ITopic where T : struct, IMessage
        {
            private readonly ConcurrentQueue<T> _queue = new();
            private readonly List<Action<T>> _subscribers = new();

            /// <summary>
            /// Enqueues a message for later dispatch.
            /// </summary>
            public void Publish(in T message) => _queue.Enqueue(message);

            /// <summary>
            /// Registers a subscriber for messages of type <typeparamref name="T"/>.
            /// </summary>
            public IDisposable Subscribe(Action<T> handler)
            {
                lock (_subscribers) _subscribers.Add(handler);
                return new Unsub<T>(this, handler);
            }

            /// <summary>
            /// Delivers all queued messages to current subscribers.
            /// Maintains deterministic order (subscription order preserved).
            /// </summary>
            public int Pump()
            {
                Action<T>[] handlers;
                lock (_subscribers) handlers = _subscribers.ToArray();

                int count = 0;
                while (_queue.TryDequeue(out var msg))
                {
                    for (int i = 0; i < handlers.Length; i++)
                        handlers[i](msg);
                    count++;
                }
                return count;
            }

            /// <summary>
            /// Disposable handle that automatically removes the subscriber when disposed.
            /// </summary>
            private sealed class Unsub<TMsg> : IDisposable where TMsg : struct, IMessage
            {
                private readonly Topic<TMsg> _owner;
                private readonly Action<TMsg> _handler;

                public Unsub(Topic<TMsg> owner, Action<TMsg> handler)
                {
                    _owner = owner;
                    _handler = handler;
                }

                public void Dispose()
                {
                    lock (_owner._subscribers)
                        _owner._subscribers.Remove(_handler);
                }
            }
        }

        private readonly ConcurrentDictionary<Type, ITopic> _topics = new();

        /// <inheritdoc />
        public void Publish<T>(in T msg) where T : struct, IMessage
        {
            ((Topic<T>)_topics.GetOrAdd(typeof(T), _ => new Topic<T>())).Publish(in msg);
        }

        /// <inheritdoc />
        public IDisposable Subscribe<T>(Action<T> handler) where T : struct, IMessage
        {
            return ((Topic<T>)_topics.GetOrAdd(typeof(T), _ => new Topic<T>())).Subscribe(handler);
        }

        /// <inheritdoc />
        public int PumpAll()
        {
            int processed = 0;
            foreach (var kv in _topics)
                processed += kv.Value.Pump();
            return processed;
        }

        /// <summary>
        /// Clears all topics and subscriptions.
        /// Useful during test teardown or full runtime shutdown.
        /// </summary>
        public void Clear() => _topics.Clear();
    }
}
