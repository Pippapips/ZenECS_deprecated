﻿// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem
// File: World.JobScheduling.cs
// Purpose: Lightweight job scheduling for deferring world mutations or batched work.
// Key concepts:
//   • A simple IJob interface executed from a thread-safe queue.
//   • Schedule() enqueues; RunScheduledJobs() dequeues and executes in FIFO order.
//   • Intended for frame-barrier style application alongside CommandBuffer.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (see LICENSE or https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System.Collections.Concurrent;

namespace ZenECS.Core
{
    public sealed partial class World
    {
        /// <summary>
        /// Minimal job contract for the world's scheduler.
        /// Implementors encapsulate a unit of work that can run later against a <see cref="World"/>.
        /// </summary>
        /// <remarks>
        /// Jobs are executed on the thread that calls <see cref="RunScheduledJobs"/>.
        /// Ensure your job is thread-safe with respect to any shared state it touches.
        /// </remarks>
        /// <example>
        /// Example of scheduling a job:
        /// <code>
        /// // Enqueue work to heal a batch of entities at the next barrier:
        /// world.Schedule(new HealJob(entities, amount: 3));
        ///
        /// struct HealJob : World.IJob
        /// {
        ///     private readonly Entity[] _ents;
        ///     private readonly int _amount;
        ///
        ///     public HealJob(Entity[] ents, int amount)
        ///     {
        ///         _ents = ents;
        ///         _amount = amount;
        ///     }
        ///
        ///     public void Execute(World w)
        ///     {
        ///         for (int i = 0; i &lt; _ents.Length; i++)
        ///         {
        ///             var e = _ents[i];
        ///             if (w.Has&lt;Health&gt;(e))
        ///             {
        ///                 ref var h = ref w.RefExisting&lt;Health&gt;(e);
        ///                 h.Value += _amount;
        ///             }
        ///         }
        ///     }
        /// }
        /// </code>
        /// </example>
        public interface IJob
        {
            /// <summary>Executes the job against the provided <see cref="World"/>.</summary>
            void Execute(World w);
        }

        /// <summary>
        /// Thread-safe queue that stores scheduled jobs until they are executed.
        /// </summary>
        private readonly ConcurrentQueue<IJob> jobQueue = new();

        /// <summary>
        /// Enqueues a job for later execution. A null reference is ignored.
        /// </summary>
        /// <param name="job">The job instance to enqueue.</param>
        private void Schedule(IJob? job)
        {
            if (job != null)
            {
                jobQueue.Enqueue(job);
            }
        }

        /// <summary>
        /// Executes all scheduled jobs in FIFO order.
        /// </summary>
        /// <returns>The number of jobs executed.</returns>
        /// <remarks>
        /// This method drains the queue on the calling thread. Call it at a frame barrier or other
        /// consistent point in your main loop to make structural changes deterministic.
        /// </remarks>
        public int RunScheduledJobs()
        {
            int n = 0;
            while (jobQueue.TryDequeue(out var j))
            {
                j.Execute(this);
                n++;
            }

            return n;
        }

        /// <summary>
        /// Clears all pending jobs without executing them.
        /// </summary>
        /// <remarks>
        /// Useful during hard resets or when discarding work between scene loads.
        /// </remarks>
        private void ClearAllScheduledJobs()
        {
            jobQueue.Clear();
        }
    }
}
