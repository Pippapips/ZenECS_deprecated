// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: DefaultMainThreadGate.cs
// Purpose: Main-thread guard and marshaling utility built on SynchronizationContext.
// Key concepts:
//   • Captures the main thread id at construction and exposes IsMainThread/Ensure.
//   • Post() and Send() marshal actions to the main thread (async vs sync).
//   • Optional inline mode when no context is available (console-style runners).
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ZenECS.Core.Binding.Util
{
    /// <summary>
    /// Default implementation of <see cref="IMainThreadGate"/> that uses
    /// <see cref="SynchronizationContext"/> to marshal actions to the main thread.
    /// </summary>
    public sealed class DefaultMainThreadGate : IMainThreadGate
    {
        private readonly int _mainThreadId;
        private readonly SynchronizationContext _ctx;
        private readonly ConcurrentQueue<Action> _queue = new();
        private readonly bool _inline;

        /// <summary>
        /// Creates a new gate bound to the current thread as the "main" thread.
        /// When no <see cref="SynchronizationContext"/> is present and
        /// <paramref name="inlineWhenNoContext"/> is true, actions are run inline.
        /// </summary>
        /// <param name="inlineWhenNoContext">
        /// If true and there is no current context, fall back to inline execution.
        /// </param>
        public DefaultMainThreadGate(bool inlineWhenNoContext = true)
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            _ctx = SynchronizationContext.Current;
            _inline = inlineWhenNoContext && _ctx is null; // Console runners: no context → execute inline.
            if (_ctx is null) _ctx = new SynchronizationContext();
        }

        /// <summary>
        /// Indicates whether the current thread is the captured main thread.
        /// </summary>
        public bool IsMainThread => Thread.CurrentThread.ManagedThreadId == _mainThreadId;

        /// <summary>
        /// Ensures that the caller is on the main thread; otherwise throws.
        /// </summary>
        public void Ensure()
        {
            if (!IsMainThread) throw new InvalidOperationException("Must be called on main thread.");
        }

        /// <summary>
        /// Posts an action to run asynchronously on the main thread.
        /// If inline mode is enabled (no context), the action executes immediately.
        /// </summary>
        public void Post(Action action)
        {
            if (action == null) return;
            if (_inline) { action(); return; }
            _queue.Enqueue(action);
            _ctx.Post(_ => PumpOnce(), null);
        }

        /// <summary>
        /// Sends an action to run on the main thread and blocks until completion.
        /// If already on the main thread (or inline mode is enabled), executes directly.
        /// </summary>
        public void Send(Action action)
        {
            if (action == null) return;

            if (IsMainThread)
            {
                action();
                return;
            }

            if (_inline || IsMainThread)
            {
                action();
                return;
            }

            using var done = new ManualResetEventSlim(false);
            Exception? ex = null;

            _queue.Enqueue(() =>
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    ex = e;
                }
                finally
                {
                    done.Set();
                }
            });

            _ctx.Post(_ => PumpOnce(), null);
            done.Wait();

            if (ex != null) throw ex; // Note: rethrowing the captured exception resets its stack trace.
        }

        /// <summary>
        /// Drains the pending actions queue once on the current thread (main thread when called via context).
        /// </summary>
        private void PumpOnce()
        {
            while (_queue.TryDequeue(out var a))
            {
                try
                {
                    a();
                }
                catch
                {
                    // Swallow exceptions to avoid breaking the message pump;
                    // the original sender in Send() will rethrow via captured exception.
                }
            }
        }
    }
}
