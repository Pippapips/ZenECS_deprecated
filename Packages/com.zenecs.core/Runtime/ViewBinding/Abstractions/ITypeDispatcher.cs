using System;

namespace ZenECS.Core.ViewBinding
{
    public interface ITypeDispatcher
    {
        void DispatchDelta(Type t, IViewBinder b, Entity e, byte kind, object boxed);
    }
}