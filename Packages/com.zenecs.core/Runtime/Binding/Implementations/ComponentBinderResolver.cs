#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core.Binding
{
    public sealed class ComponentBinderResolver : IComponentBinderResolver
    {
        private readonly IComponentBinderRegistry _registry;
        public ComponentBinderResolver(IComponentBinderRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public bool TryResolve(Type componentType, out IComponentBinder binder)
        {
            var b = _registry.Resolve(componentType);
            if (b is not null) { binder = b; return true; }
            binder = default!; return false;
        }

        public IReadOnlyCollection<Type> RegisteredComponentTypes
            => (_registry as ComponentBinderRegistry)?.RegisteredComponentTypes
               ?? Array.Empty<Type>();
    }
}
