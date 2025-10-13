#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Core.Sync;
using ZenECS.Core.Sync.Events;
using ZenECS.Core.Sync.Util;

namespace ZenECS.Core.Sync.Feed
{
    // Aggregates and dispatches change batches at the end of the frame on the main thread.
    public sealed class EndOfFrameChangeFeed : IChangeFeed
    {
        private event Action<IReadOnlyList<ChangeRecord>>? OnBatch;

        public void PublishBatch(IReadOnlyList<ChangeRecord> records)
        {
            MainThreadGate.Ensure();
            OnBatch?.Invoke(records);
        }

        public IDisposable SubscribeRaw(Action<IReadOnlyList<ChangeRecord>> cb)
        {
            OnBatch += cb;
            return new Unsub(() => OnBatch -= cb);
        }

        private sealed class Unsub : IDisposable
        {
            private readonly Action _a;
            public Unsub(Action a) => _a = a;
            public void Dispose() => _a();
        }
    }
}