using System;

namespace ZenECS.Core.Binding
{
    public interface ITypeDispatcher
    {
        void DispatchDelta(Type t, IBinder b, Entity e, byte kind, object boxed);
    }
}