#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core.Binding
{
    internal sealed class ContextFactoryHub : IContextFactoryHub
    {
        private readonly Dictionary<Type, object> _map = new();

        public void Register<T>(IContextFactory<T> f) where T : class, IContext
            => _map[typeof(T)] = f;

        public bool TryCreate<T>(World w, Entity e, out T? ctx) where T : class, IContext
        {
            ctx = null;
            if (!_map.TryGetValue(typeof(T), out var f)) return false;
            ctx = ((IContextFactory<T>)f).Create(w, e);
            return true;
        }
    }
}