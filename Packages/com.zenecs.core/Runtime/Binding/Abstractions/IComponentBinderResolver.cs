using System;
using System.Collections.Generic;
namespace ZenECS.Core.Binding
{
    public interface IComponentBinderResolver
    {
        bool TryResolve(Type componentType, out IComponentBinder binder);
        IReadOnlyCollection<Type> RegisteredComponentTypes { get; }
    }
}
