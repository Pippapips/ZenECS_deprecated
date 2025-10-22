#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Core.Binding.Util;

namespace ZenECS.Core.Binding
{
    // Aggregates and dispatches change batches at the end of the frame on the main thread.
    public sealed class ComponentChangeFeed : IComponentChangeFeed
    {
        public ComponentChangeFeed(IMainThreadGate gate) { _gate = gate; }
        private readonly IMainThreadGate _gate;
        private event Action<IReadOnlyList<ComponentChangeRecord>>? OnBatch;

        public void PublishBatch(IReadOnlyList<ComponentChangeRecord> records)
        {
            // 발행 즉시 방어적 스냅샷(리스트가 이후에 Clear/재사용되어도 안전)
            var snapshot = records as ComponentChangeRecord[] ?? System.Linq.Enumerable.ToArray(records);
            _gate.Post(() => OnBatch?.Invoke(snapshot));
        }

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
