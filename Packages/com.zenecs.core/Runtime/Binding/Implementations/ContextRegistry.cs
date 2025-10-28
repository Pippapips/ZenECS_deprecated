#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core.Binding
{
    internal sealed class ContextRegistry : IContextRegistry
    {
        private readonly Dictionary<(World, int), Dictionary<Type, IContext>> _map = new();

        public bool TryGet<T>(World w, Entity e, out T? ctx) where T : class, IContext
        {
            ctx = null;
            if (!_map.TryGetValue((w, e.Id), out var byType)) return false;
            if (!byType.TryGetValue(typeof(T), out var boxed)) return false;
            return (ctx = boxed as T) != null;
        }

        public void Register<T>(World w, Entity e, T ctx) where T : class, IContext
        {
            var key = (w, e.Id);
            if (!_map.TryGetValue(key, out var byType))
                _map[key] = byType = new Dictionary<Type, IContext>(4);
            byType[typeof(T)] = ctx;
        }

        public void UnregisterAll(World w, Entity e)
        {
            _map.Remove((w, e.Id));
        }
    }
}