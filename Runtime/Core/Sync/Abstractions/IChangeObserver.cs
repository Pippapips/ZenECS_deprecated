using ZenECS.Core.Sync.Events;

namespace ZenECS.Core.Sync
{
    public interface IChangeObserver<T>
    {
        void OnAdded(in ChangeRecord r);
        void OnChanged(in ChangeRecord r, in T v);
        void OnRemoved(in ChangeRecord r);
    }
}