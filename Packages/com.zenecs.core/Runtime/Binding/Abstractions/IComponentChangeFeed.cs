// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: IComponentChangeFeed.cs
// Purpose: Publish/subscribe interface for batched component change notifications.
// Key concepts:
//   • Batches reduce overhead for frequent updates.
//   • Subscribers receive raw lists for zero-copy pipelines.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core.Binding
{
    /// <summary>
    /// Provides a minimal batching feed for component change events.
    /// </summary>
    public interface IComponentChangeFeed
    {
        /// <summary>Publishes a batch of component change records.</summary>
        void PublishBatch(IReadOnlyList<ComponentChangeRecord> records);

        /// <summary>Subscribes to raw batches; returns a disposable to unsubscribe.</summary>
        IDisposable SubscribeRaw(Action<IReadOnlyList<ComponentChangeRecord>> onBatch);
    }
}