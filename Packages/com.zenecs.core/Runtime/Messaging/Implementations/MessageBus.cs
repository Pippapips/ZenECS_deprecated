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
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ZenECS.Core.Messaging
{
    /// <summary>
    /// Thread-safe implementation of <see cref="IMessageBus"/> that provides lock-free,
    /// per-message-type queues and subscriber lists.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This message bus handles value-type messages (<see langword="struct"/>s implementing <see cref="IMessage"/>).
    /// <see cref="Publish{T}(in T)"/> incurs no boxing or heap allocations.
    /// </para>
    /// <para>
    /// Each message type maintains its own topic (queue + subscriber list).
    /// <see cref="PumpAll"/> processes all pending messages in deterministic order and
    /// synchronously delivers them to subscribers.  
    /// Typically, you should call <c>bus.PumpAll()</c> once per frame (e.g., in <c>Runner.BeginFrame</c>).
    /// </para>
    /// <para>
    /// Publishing and subscribing are thread-safe. However, <see cref="PumpAll"/> performs synchronous dispatch;
    /// therefore, avoid long-running or blocking logic inside handlers.
    /// </para>
    /// </remarks>
    public sealed class MessageBus : IMessageBus
    {
        private interface ITopic { int Pump(); }

        private sealed class Topic<T> : ITopic where T : struct, IMessage
        {
            private readonly ConcurrentQueue<T> _queue = new();
            private readonly List<Action<T>> _subscribers = new();

            /// <summary>
            /// Enqueues a message instance for later delivery.
            /// </summary>
            /// <param name="message">The message to enqueue.</param>
            public void Publish(in T message) => _queue.Enqueue(message);

            /// <summary>
            /// Registers a subscriber callback for messages of type <typeparamref name="T"/>.
            /// </summary>
            /// <param name="handler">Callback invoked for each delivered message.</param>
            /// <returns>
            /// An <see cref="IDisposable"/> token that can be disposed to unsubscribe.
            /// </returns>
            public IDisposable Subscribe(Action<T> handler)
            {
                lock (_subscribers)
                    _subscribers.Add(handler);
                return new Unsub<T>(this, handler);
            }

            /// <summary>
            /// Delivers all queued messages to current subscribers in registration order.
            /// </summary>
            /// <returns>The number of messages processed for this topic.</returns>
            public int Pump()
            {
                Action<T>[] handlers;
                lock (_subscribers)
                    handlers = _subscribers.ToArray();

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
            /// Represents a disposable handle that removes a subscriber when disposed.
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

        /// <summary>
        /// Publishes a message of type <typeparamref name="T"/> into the bus.
        /// </summary>
        /// <typeparam name="T">A struct message type implementing <see cref="IMessage"/>.</typeparam>
        /// <param name="msg">The message instance to enqueue (passed by <see langword="in"/> reference).</param>
        /// <remarks>
        /// This method only enqueues the message; actual delivery occurs during <see cref="PumpAll"/>.
        /// Because the message type is a struct, no boxing or allocation occurs.
        /// </remarks>
        public void Publish<T>(in T msg) where T : struct, IMessage
        {
            ((Topic<T>)_topics.GetOrAdd(typeof(T), _ => new Topic<T>())).Publish(in msg);
        }

        /// <summary>
        /// Subscribes to messages of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">A struct message type implementing <see cref="IMessage"/>.</typeparam>
        /// <param name="handler">Callback invoked for each message during <see cref="PumpAll"/>.</param>
        /// <returns>
        /// An <see cref="IDisposable"/> subscription token.  
        /// Dispose it to stop receiving messages.
        /// </returns>
        /// <example>
        /// <code>
        /// var sub = bus.Subscribe&lt;MoveIntent&gt;(m =&gt; ApplyMove(m.Entity, m.Direction));
        /// ...
        /// sub.Dispose(); // stop receiving messages
        /// </code>
        /// </example>
        public IDisposable Subscribe<T>(Action<T> handler) where T : struct, IMessage
        {
            return ((Topic<T>)_topics.GetOrAdd(typeof(T), _ => new Topic<T>())).Subscribe(handler);
        }

        /// <summary>
        /// Flushes all pending messages across all topics and synchronously delivers them to subscribers.
        /// </summary>
        /// <returns>Total number of messages processed across all topics.</returns>
        /// <remarks>
        /// Typically invoked once per frame.  
        /// Handler invocations occur on the calling thread, so avoid blocking operations within handlers.
        /// </remarks>
        public int PumpAll()
        {
            int processed = 0;
            foreach (var kv in _topics)
                processed += kv.Value.Pump();
            return processed;
        }

        /// <summary>
        /// Clears all topics, message queues, and subscriber lists.
        /// </summary>
        /// <remarks>
        /// Use this method during teardown or application shutdown.  
        /// After clearing, existing subscription tokens become invalid.
        /// </remarks>
        public void Clear() => _topics.Clear();
        }
}
