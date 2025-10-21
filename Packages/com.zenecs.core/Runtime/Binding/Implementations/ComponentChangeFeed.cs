#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Core.Binding.Util;

namespace ZenECS.Core.Binding
{
    // Aggregates and dispatches change batches at the end of the frame on the main thread.
    public sealed class ComponentChangeFeed : IComponentChangeFeed
    {
        private event Action<IReadOnlyList<ComponentChangeRecord>>? OnBatch;

        public void PublishBatch(IReadOnlyList<ComponentChangeRecord> records)
        {
            MainThreadGate.Ensure();
            OnBatch?.Invoke(records);
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
