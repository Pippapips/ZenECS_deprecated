﻿// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem
// File: World.CommandBuffer.cs
// Purpose: Thread-safe command buffer for deferred or immediate structural changes.
// Key concepts:
//   • ConcurrentQueue of operations; Scheduled vs Immediate apply modes.
//   • RunScheduledJobs to commit at frame boundaries.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System.Collections.Concurrent;

namespace ZenECS.Core
{
    public sealed partial class World
    {
        // Multithreaded command buffer + scheduling example:
        // var cb = world.BeginWrite();
        // cb.Add(e, new Damage { Amount = 10 });
        // cb.Remove<Stunned>(e);
        // world.Schedule(cb);     // Applied at the frame barrier when world.RunScheduledJobs() is called
        // Or call world.EndWrite(cb); to apply immediately.

        /// <summary>
        /// Controls how a <see cref="CommandBuffer"/> is applied when disposed or explicitly flushed.
        /// </summary>
        public enum ApplyMode
        {
            /// <summary>
            /// Queue this buffer to be applied at the next frame barrier.
            /// Use for background threads or when you want deterministic, batched commits.
            /// </summary>
            Schedule = 0,

            /// <summary>
            /// Apply this buffer immediately on dispose (or when explicitly ended).
            /// Recommended from the main thread only, to minimize contention.
            /// </summary>
            Immediate = 1,
        }

        /// <summary>
        /// Thread-safe buffer of structural ECS operations (Add/Replace/Remove/Destroy) that can be
        /// applied later as a job (scheduled) or flushed immediately on the main thread.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use <see cref="BeginWrite(ApplyMode)"/> to obtain a buffer. Enqueue operations via
        /// <see cref="Add{T}(Entity, in T)"/>, <see cref="Replace{T}(Entity, in T)"/>,
        /// <see cref="Remove{T}(Entity)"/>, and <see cref="Destroy(Entity)"/>.
        /// </para>
        /// <para>
        /// When the buffer is disposed, it is either scheduled or immediately applied depending on the
        /// <see cref="ApplyMode"/> it was created with. You can also explicitly call
        /// <see cref="World.EndWrite(CommandBuffer)"/> or <see cref="World.Schedule(CommandBuffer?)"/>.
        /// </para>
        /// </remarks>
        public sealed class CommandBuffer : IJob, System.IDisposable
        {
            internal readonly ConcurrentQueue<IOp> q = new();

            // Bound in BeginWrite
            private World? _boundWorld;
            private ApplyMode _mode;
            private bool _disposed;

            /// <summary>
            /// The world this buffer is currently bound to (set by <see cref="BeginWrite(ApplyMode)"/>).
            /// May be <see langword="null"/> after <see cref="Dispose"/> is called.
            /// </summary>
            public World? WorldRef => _boundWorld;

            /// <summary>
            /// Binds this buffer to a specific <see cref="World"/> and applies the given <see cref="ApplyMode"/>.
            /// Intended for internal use by <see cref="World.BeginWrite(ApplyMode)"/>.
            /// </summary>
            internal void Bind(World w, ApplyMode mode)
            {
                _boundWorld = w;
                _mode = mode;
                _disposed = false;
            }

            /// <summary>
            /// Disposes the buffer and either schedules or immediately applies it according to the
            /// <see cref="ApplyMode"/> that was used at creation time.
            /// </summary>
            /// <remarks>
            /// Buffers created via <see cref="World.BeginWrite(ApplyMode)"/> are auto-applied on dispose.
            /// Buffers created manually should be applied via <see cref="World.EndWrite(CommandBuffer)"/> or
            /// <see cref="World.Schedule(CommandBuffer?)"/>.
            /// </remarks>
            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;

                // Auto-apply only for buffers created by using (BeginWrite)
                var w = _boundWorld;
                _boundWorld = null;

                if (w == null) return;

                if (_mode == ApplyMode.Immediate)
                    w.EndWrite(this); // Apply immediately
                else
                    w.Schedule(this); // Apply at the barrier
            }

            /// <summary>
            /// Internal operation contract implemented by each queued structural change.
            /// </summary>
            internal interface IOp
            {
                /// <summary>
                /// Applies the operation against the provided <see cref="World"/>.
                /// Implementations should guard against dead entities.
                /// </summary>
                void Apply(World w);
            }

            /// <summary>
            /// Enqueues an Add component operation for the given entity.
            /// </summary>
            /// <typeparam name="T">Component value type.</typeparam>
            /// <param name="e">Target entity.</param>
            /// <param name="v">Component value to add.</param>
            public void Add<T>(Entity e, in T v) where T : struct => q.Enqueue(new AddOp<T>(e, v));

            /// <summary>
            /// Enqueues a Replace component operation for the given entity.
            /// </summary>
            /// <typeparam name="T">Component value type.</typeparam>
            /// <param name="e">Target entity.</param>
            /// <param name="v">New component value.</param>
            public void Replace<T>(Entity e, in T v) where T : struct => q.Enqueue(new ReplaceOp<T>(e, v));

            /// <summary>
            /// Enqueues a Remove component operation for the given entity.
            /// </summary>
            /// <typeparam name="T">Component value type.</typeparam>
            /// <param name="e">Target entity.</param>
            public void Remove<T>(Entity e) where T : struct => q.Enqueue(new RemoveOp<T>(e));

            /// <summary>
            /// Enqueues a Destroy entity operation.
            /// </summary>
            /// <param name="e">The entity to destroy.</param>
            public void Destroy(Entity e) => q.Enqueue(new DestroyOp(e));

            // IJob: integration with the world's scheduler
            void World.IJob.Execute(World w)
            {
                while (q.TryDequeue(out var op)) op.Apply(w);
            }

            // ----- Concrete ops ---------------------------------------------------------

            sealed class AddOp<T> : IOp where T : struct
            {
                readonly Entity e;
                readonly T v;
                public AddOp(Entity e, in T v)
                {
                    this.e = e;
                    this.v = v;
                }
                public void Apply(World w)
                {
                    if (!w.IsAlive(e))
                    {
                        /* w.Trace($"Skip Add<{typeof(T).Name}>: {e} dead"); */
                        return;
                    }
                    w.AddComponentInternal(e, in v);
                    Events.ComponentEvents.RaiseAdded(w, e, typeof(T));
                }
            }

            sealed class ReplaceOp<T> : IOp where T : struct
            {
                readonly Entity e;
                readonly T v;
                public ReplaceOp(Entity e, in T v)
                {
                    this.e = e;
                    this.v = v;
                }
                public void Apply(World w)
                {
                    if (!w.IsAlive(e))
                    {
                        /* w.Trace($"Skip Replace<{typeof(T).Name}>: {e} dead"); */
                        return;
                    }
                    // Route through World.Replace to align with hooks/validation/events.
                    w.Replace(e, in v);
                    Events.ComponentEvents.RaiseChanged(w, e, typeof(T));
                }
            }

            sealed class RemoveOp<T> : IOp where T : struct
            {
                readonly Entity e;
                public RemoveOp(Entity e) { this.e = e; }
                public void Apply(World w)
                {
                    if (!w.IsAlive(e))
                    {
                        /* w.Trace($"Skip Remove<{typeof(T).Name}>: {e} dead"); */
                        return;
                    }
                    if (w.RemoveComponentInternal<T>(e))
                        Events.ComponentEvents.RaiseRemoved(w, e, typeof(T));
                }
            }

            sealed class DestroyOp : IOp
            {
                readonly Entity e;
                public DestroyOp(Entity e) { this.e = e; }

                public void Apply(World w)
                {
                    if (!w.IsAlive(e))
                    {
                        /* w.Trace($"Skip Destroy: {e} already dead"); */
                        return;
                    }
                    w.DestroyEntity(e);
                }
            }
        }

        /// <summary>
        /// Begins a command-buffer write scope.
        /// </summary>
        /// <param name="mode">
        /// Application mode that determines what happens on <see cref="CommandBuffer.Dispose"/>:
        /// <see cref="ApplyMode.Schedule"/> (default) queues the buffer to apply at the frame barrier, while
        /// <see cref="ApplyMode.Immediate"/> applies instantly.
        /// </param>
        /// <returns>A new <see cref="CommandBuffer"/> bound to this world.</returns>
        /// <remarks>
        /// Supports the <c>using</c> pattern:
        /// <code>
        /// using (var cb = world.BeginWrite()) { /* enqueue ops */ }                    // Applies on Dispose via Schedule
        /// using (var cb = world.BeginWrite(ApplyMode.Immediate)) { /* enqueue ops */ } // Applies on Dispose immediately
        /// </code>
        /// </remarks>
        public CommandBuffer BeginWrite(ApplyMode mode = ApplyMode.Schedule)
        {
            var cb = new CommandBuffer();
            cb.Bind(this, mode);
            return cb;
        }

        /// <summary>
        /// Applies all queued operations in the specified <paramref name="cb"/> immediately.
        /// </summary>
        /// <param name="cb">The command buffer to flush. If <see langword="null"/>, no work is performed.</param>
        /// <returns>The number of operations applied.</returns>
        /// <remarks>
        /// This method is typically called on the main thread. For deferred application,
        /// see <see cref="Schedule(CommandBuffer?)"/>.
        /// </remarks>
        public int EndWrite(CommandBuffer cb)
        {
            if (cb == null) return 0;
            int n = 0;
            while (cb.q.TryDequeue(out var op))
            {
                op.Apply(this);
                n++;
            }
            return n;
        }

        /// <summary>
        /// Enqueues the given <paramref name="cb"/> to be executed at the next frame barrier via the world's scheduler.
        /// </summary>
        /// <param name="cb">The command buffer to schedule. If <see langword="null"/>, the call is ignored.</param>
        public void Schedule(CommandBuffer? cb)
        {
            if (cb != null)
            {
                Schedule((IJob)cb);
            }
        }

        /// <summary>
        /// Clears any frame-local/deferred command buffers by flushing the world's scheduled jobs queue.
        /// </summary>
        private void ClearAllCommandBuffers()
        {
            ClearAllScheduledJobs();
        }

        /// <summary>
        /// Hook executed before <see cref="World.Reset(bool)"/>. When capacity will be rebuilt,
        /// this flushes scheduled jobs to avoid dropping queued operations.
        /// </summary>
        /// <param name="keepCapacity">
        /// <see langword="true"/> to keep current capacities; <see langword="false"/> to rebuild.
        /// </param>
        partial void OnBeforeWorldReset(bool keepCapacity)
        {
            if (!keepCapacity) RunScheduledJobs();
        }
    }
}
