// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: ComponentChangeFeed.cs
// Purpose: Aggregates and dispatches batched component-change events on the main thread.
// Key concepts:
//   • Defensive snapshotting so caller-owned lists can be reused safely.
//   • Main-thread marshalling via IMainThreadGate (Post).
//   • Disposable unsubscription token for raw batch listeners.
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ZenECS.Core.Binding.Util;
using ZenECS.Core.Infrastructure;

namespace ZenECS.Core.Binding
{
    /// <summary>
    /// Aggregates and dispatches change batches at the end of the frame on the main thread.
    /// </summary>
    internal sealed class ComponentChangeFeed : IComponentChangeFeed
    {
        private readonly IMainThreadGate _gate;
        private event Action<IReadOnlyList<ComponentChangeRecord>>? OnBatch;

        /// <summary>
        /// Initializes a new change feed bound to a main-thread gate.
        /// </summary>
        public ComponentChangeFeed(IMainThreadGate gate)
        {
            _gate = gate;
            EcsRuntimeDirectory.AttachMainThreadGate(gate);
        }

        /// <summary>
        /// Publishes a batch of component changes to subscribers.
        /// </summary>
        /// <remarks>
        /// Takes a defensive snapshot immediately so the caller may clear or reuse the input list safely.
        /// </remarks>
        public void PublishBatch(IReadOnlyList<ComponentChangeRecord> records)
        {
            var snapshot = records as ComponentChangeRecord[] ?? records.ToArray();
            _gate.Post(() => OnBatch?.Invoke(snapshot));
        }

        /// <summary>
        /// Subscribes to raw change batches. Returns a disposable handle that removes the subscription.
        /// </summary>
        public IDisposable SubscribeRaw(Action<IReadOnlyList<ComponentChangeRecord>> onBatch)
        {
            OnBatch += onBatch;
            return new Unsub(() => OnBatch -= onBatch);
        }

        private sealed class Unsub : IDisposable
        {
            private readonly Action _a;
            public Unsub(Action a) => _a = a;
            public void Dispose() => _a();
        }
    }
}
