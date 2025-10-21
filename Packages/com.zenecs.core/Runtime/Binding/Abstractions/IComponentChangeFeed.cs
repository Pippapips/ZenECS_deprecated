using System;
using System.Collections.Generic;

namespace ZenECS.Core.Binding
{
    public interface IComponentChangeFeed
    {
        void PublishBatch(IReadOnlyList<ComponentChangeRecord> records);
        IDisposable SubscribeRaw(Action<IReadOnlyList<ComponentChangeRecord>> onBatch);
    }
}