// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: ComponentBindingHubSystem.cs
// Purpose: Collects per-frame component change events into a de-duplicated batch.
// Key concepts:
//   • Subscribes to ComponentEvents.* and merges (entity,type) → mask.
//   • Publishes a single batched list each frame via IComponentChangeFeed.
//   • Presentation-stage system; ordered before ComponentBatchDispatchSystem.
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ZenECS.Core.Events;
using ZenECS.Core.Systems;

namespace ZenECS.Core.Binding.Systems
{
    [PresentationGroup]
    [OrderBefore(typeof(ComponentBatchDispatchSystem))]
    internal sealed class ComponentBindingHubSystem : ISystemLifecycle, IPresentationSystem
    {
        private readonly Dictionary<(Entity, Type), ComponentChangeMask> _batch = new();
        private readonly IComponentChangeFeed _feed;

        public ComponentBindingHubSystem(IComponentChangeFeed feed) => _feed = feed;

        public void Initialize(World w)
        {
            ComponentEvents.ComponentAdded += OnAdded;
            ComponentEvents.ComponentRemoved += OnRemoved;
            ComponentEvents.ComponentChanged += OnChanged;
        }

        public void Shutdown(World w)
        {
            ComponentEvents.ComponentAdded -= OnAdded;
            ComponentEvents.ComponentRemoved -= OnRemoved;
            ComponentEvents.ComponentChanged -= OnChanged;
            _batch.Clear();
        }

        public void Run(World w)
        {
            if (_batch.Count == 0) return;
            var list = _batch.Select(kv => new ComponentChangeRecord(kv.Key.Item1, kv.Key.Item2, kv.Value)).ToList();
            _feed.PublishBatch(list);
            _batch.Clear();
        }

        // Compatibility overload (alpha unused by this system)
        public void Run(World w, float alpha = 1) { Run(w); }

        private void OnAdded(World w, Entity e, Type t)   { if (t != null) _batch[(e, t)] = ComponentChangeMask.Added; }
        private void OnRemoved(World w, Entity e, Type t) { if (t != null) _batch[(e, t)] = ComponentChangeMask.Removed; }
        private void OnChanged(World w, Entity e, Type t) { if (t != null) _batch[(e, t)] = ComponentChangeMask.Changed; }
    }
}
