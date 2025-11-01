#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ZenECS.Core;

namespace ZenECS.Core.Binding
{
    public sealed class ContextRegistry : IContextRegistry
    {
        private sealed class Entry
        {
            public IContext Ctx;
            public Type KeyType;
            public bool Initialized;
            public Entry(IContext ctx, Type key)
            {
                Ctx = ctx; KeyType = key; Initialized = false;
            }
        }

        // World → EntityId → (KeyType → Entry)
        private readonly Dictionary<World, Dictionary<Entity, Dictionary<Type, Entry>>> _map
            = new(ReferenceEqualityComparer<World>.Instance);

        private Dictionary<Entity, Dictionary<Type, Entry>> Bag(World w)
            => _map.TryGetValue(w, out var d) ? d : (_map[w] = new());
        private Dictionary<Type, Entry> Bag(World w, Entity e)
            => Bag(w).TryGetValue(e, out var d) ? d : (Bag(w)[e] = new());

        // ── Lookup ───────────────────────────────────────────────────────
        public bool TryGet<T>(World w, Entity e, out T ctx) where T : class, IContext
        {
            ctx = null!;
            if (!Bag(w).TryGetValue(e, out var dict)) return false;

            // Exact type first
            if (dict.TryGetValue(typeof(T), out var exact) && exact.Ctx is T exactCtx)
            { ctx = exactCtx; return true; }

            // Most-specific assignable type
            Entry best = null;
            foreach (var kv in dict)
            {
                var t = kv.Key; var entry = kv.Value;
                if (!typeof(T).IsAssignableFrom(t)) continue;
                if (entry.Ctx is not T cand) continue;
                if (best == null || best.KeyType.IsAssignableFrom(t))
                { best = entry; ctx = cand; }
            }
            return ctx != null;
        }

        bool TryGet(World w, Entity e, out IContext ctx)
        {
            return TryGet(w, e, out ctx);
        }

        public T Get<T>(World w, Entity e) where T : class, IContext
            => TryGet<T>(w, e, out var v) ? v :
               throw new KeyNotFoundException($"Context {typeof(T).Name} not found for {e}.");

        public bool Has<T>(World w, Entity e) where T : class, IContext
            => TryGet<T>(w, e, out _);

        public bool Has(World w, Entity e, IContext ctx)
            => TryGet(w, e, out _);

        // ── Register / Remove ────────────────────────────────────────────
        public void Register(World w, Entity e, IContext ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            var key = ctx.GetType();
            var bag = Bag(w, e);

            if (!bag.TryGetValue(key, out var entry))
            {
                entry = new Entry(ctx, key);
                bag[key] = entry;
            }
            else
            {
                // replace: deinit old if needed
                if (entry.Initialized && entry.Ctx is IContextInitialize oldIni)
                {
                    oldIni.Deinitialize(w, e);
                    entry.Initialized = false;
                }
                entry.Ctx = ctx;
                entry.KeyType = key;
            }

            if (ctx is IContextInitialize ini)
            {
                ini.Initialize(w, e, this);
                entry.Initialized = true;
            }
        }

        public bool Remove(World w, Entity e, IContext ctx)
        {
            if (ctx == null) return false;
            if (!Bag(w).TryGetValue(e, out var dict)) return false;

            var key = dict.Keys.FirstOrDefault(t => ReferenceEquals(dict[t].Ctx, ctx)) ??
                      dict.Keys.FirstOrDefault(t => t.IsInstanceOfType(ctx));
            if (key == null) return false;

            var entry = dict[key];
            if (entry.Initialized && entry.Ctx is IContextInitialize ini)
            {
                ini.Deinitialize(w, e);
                entry.Initialized = false;
            }
            dict.Remove(key);
            if (dict.Count == 0) Bag(w).Remove(e);
            return true;
        }

        public bool Remove<T>(World w, Entity e) where T : class, IContext
            => TryGet<T>(w, e, out var ctx) && Remove(w, e, ctx);

        // ── Reinitialize ─────────────────────────────────────────────────
        public bool Reinitialize(World w, Entity e, IContext ctx)
        {
            if (!Bag(w).TryGetValue(e, out var dict)) return false;
            var key = dict.Keys.FirstOrDefault(t => ReferenceEquals(dict[t].Ctx, ctx)) ??
                      dict.Keys.FirstOrDefault(t => t.IsInstanceOfType(ctx));
            if (key == null) return false;

            var entry = dict[key];
            if (entry.Ctx is not IContextInitialize ini) return false;

            if (entry.Initialized)
            {
                if (entry.Ctx is IContextReinitialize fast)
                    fast.Reinitialize(w, e, this);
                else
                {
                    ini.Deinitialize(w, e);
                    ini.Initialize  (w, e, this);
                }
            }
            else
            {
                ini.Initialize(w, e, this);
            }

            entry.Initialized = true;
            return true;
        }

        public bool Reinitialize<T>(World w, Entity e) where T : class, IContext
            => TryGet<T>(w, e, out var ctx) && Reinitialize(w, e, ctx);

        // ── State / Clear ────────────────────────────────────────────────
        public bool IsInitialized(World w, Entity e, IContext ctx)
        {
            if (!Bag(w).TryGetValue(e, out var dict)) return false;
            foreach (var kv in dict)
                if (ReferenceEquals(kv.Value.Ctx, ctx)) return kv.Value.Initialized;
            return false;
        }

        public bool IsInitialized<T>(World w, Entity e) where T : class, IContext
            => TryGet<T>(w, e, out var ctx) && IsInitialized(w, e, ctx);

        public void Clear(World w, Entity e)
        {
            if (!Bag(w).TryGetValue(e, out var dict)) return;
            foreach (var entry in dict.Values.ToArray())
                if (entry.Initialized && entry.Ctx is IContextInitialize ini)
                    ini.Deinitialize(w, e);
            Bag(w).Remove(e);
        }

        public void ClearAll()
        {
            foreach (var (w, perE) in _map)
                foreach (var (e, dict) in perE)
                    foreach (var entry in dict.Values)
                        if (entry.Initialized && entry.Ctx is IContextInitialize ini)
                            ini.Deinitialize(w, e);
            _map.Clear();
        }
    }

    internal sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        public static readonly ReferenceEqualityComparer<T> Instance = new();
        public bool Equals(T x, T y) => ReferenceEquals(x, y);
        public int  GetHashCode(T obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
