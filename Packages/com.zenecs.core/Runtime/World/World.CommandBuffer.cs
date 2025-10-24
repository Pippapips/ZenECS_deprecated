// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem
// File: World.CommandBuffer.cs
// Purpose: Thread-safe command buffer for deferred or immediate structural changes.
// Key concepts:
//   • ConcurrentQueue of operations; Scheduled vs Immediate apply modes.
//   • RunScheduledJobs to commit at frame boundaries.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (see LICENSE or https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System.Collections.Concurrent;
using ZenECS.Core.Extensions;

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

        /// <summary>Write-scope apply policy.</summary>
        public enum ApplyMode
        {
            /// <summary>
            /// On dispose, enqueue into the scheduler and apply at the frame barrier.
            /// </summary>
            Schedule = 0,

            /// <summary>
            /// On dispose, apply immediately (recommended from the main thread).
            /// </summary>
            Immediate = 1,
        }

        /// <summary>
        /// Thread-safe command buffer.
        /// Can be applied via EndWrite(cb) or Schedule(cb).
        /// Also supports the using pattern with BeginWrite(...).
        /// </summary>
        public sealed class CommandBuffer : IJob, System.IDisposable
        {
            internal readonly ConcurrentQueue<IOp> q = new();

            // Bound in BeginWrite
            private World? _boundWorld;
            private ApplyMode _mode;
            private bool _disposed;
            public World? WorldRef => _boundWorld;

            internal void Bind(World w, ApplyMode mode)
            {
                _boundWorld = w;
                _mode = mode;
                _disposed = false;
            }

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

            internal interface IOp
            {
                void Apply(World w);
            } // Guarding dead entities is handled inside each Op.

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

            // Enqueue operations
            public void Add<T>(Entity e, in T v) where T : struct => q.Enqueue(new AddOp<T>(e, v));
            public void Replace<T>(Entity e, in T v) where T : struct => q.Enqueue(new ReplaceOp<T>(e, v));
            public void Remove<T>(Entity e) where T : struct => q.Enqueue(new RemoveOp<T>(e));
            public void Destroy(Entity e) => q.Enqueue(new DestroyOp(e));

            // IJob: integration with the world's scheduler
            void World.IJob.Execute(World w)
            {
                while (q.TryDequeue(out var op)) op.Apply(w);
            }
        }

        /// <summary>
        /// Begins a command-buffer write scope. Supports the using pattern:
        /// <code>
        /// using (var cb = world.BeginWrite()) { ... }                    // Applies on Dispose via Schedule
        /// using (var cb = world.BeginWrite(ApplyMode.Immediate)) { ... } // Applies on Dispose immediately
        /// </code>
        /// </summary>
        public CommandBuffer BeginWrite(ApplyMode mode = ApplyMode.Schedule)
        {
            var cb = new CommandBuffer();
            cb.Bind(this, mode);
            return cb;
        }

        /// <summary>
        /// Applies commands collected on another thread immediately (recommended to call on the main thread).
        /// </summary>
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
        /// Enqueues the command buffer into the scheduler to be applied at the next frame barrier.
        /// </summary>
        public void Schedule(CommandBuffer? cb)
        {
            if (cb != null)
            {
                Schedule((IJob)cb);
            }
        }

        // Example: if you maintain frame-local/deferred command buffers, clear them here.
        private void ClearAllCommandBuffers()
        {
            ClearAllScheduledJobs();
        }

        // Optional: customize flush/drop policy on Reset; here we flush scheduled jobs when capacity will be rebuilt.
        partial void OnBeforeWorldReset(bool keepCapacity)
        {
            if (!keepCapacity) RunScheduledJobs();
        }
    }
}
