using System;
using System.Collections.Generic;
using ZenECS.Core.Sync.Events;

namespace ZenECS.Core.Sync
{
    public interface IChangeFeed
    {
        void PublishBatch(IReadOnlyList<ChangeRecord> records);
        IDisposable SubscribeRaw(Action<IReadOnlyList<ChangeRecord>> onBatch);
    }
}