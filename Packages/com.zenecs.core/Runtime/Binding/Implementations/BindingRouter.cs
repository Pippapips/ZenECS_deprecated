#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ZenECS.Core;

namespace ZenECS.Core.Binding
{
    /// <summary>Manages binders per entity, dispatches deltas, calls Apply() once per frame.</summary>
    public sealed class BindingRouter : IBindingRouter 
    {
        private readonly World _world;
        private readonly IContextRegistry _ctx;
        private readonly Dictionary<int, List<IBinder>> _byEntity = new(1024);
        private int _attachSeq = 0;

        public BindingRouter(World world, IContextRegistry registry)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _ctx   = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public void Attach(Entity e, IBinder binder, AttachOptions options = AttachOptions.Strict)
        {
            if (binder == null) throw new ArgumentNullException(nameof(binder));
            ValidateRequiredContexts(binder, e, options);
            binder.Bind(_world, e);
            if (!_byEntity.TryGetValue(e.Id, out var list))
                _byEntity[e.Id] = list = new List<IBinder>(4);
            InsertOrdered(list, binder);
        }

        public void Detach(Entity e, IBinder binder)
        {
            if (_byEntity.TryGetValue(e.Id, out var list) && list.Remove(binder))
                binder.Unbind();
        }

        public void DetachAll(Entity e)
        {
            if (_byEntity.TryGetValue(e.Id, out var list))
            {
                foreach (var b in list) b.Unbind();
                list.Clear();
                _byEntity.Remove(e.Id);
            }
        }

        public void OnEntityDestroyed(Entity e)
        {
            DetachAll(e);
            _ctx.Clear(_world, e);
        }

        public void ApplyAll()
        {
            foreach (var list in _byEntity.Values)
                for (int i = 0; i < list.Count; i++)
                    list[i].Apply();
        }

        public void Dispatch<T>(in ComponentDelta<T> d) where T : struct
        {
            if (!_byEntity.TryGetValue(d.Entity.Id, out var list)) return;
            int n = list.Count;
            for (int i = 0; i < n; i++)
            {
                if (i >= list.Count) break;
                if (list[i] is IBinds<T> b) b.OnDelta(in d);
            }
        }

        private void InsertOrdered(List<IBinder> list, IBinder binder)
        {
            int attachOrder = ++_attachSeq;
            if (binder is IAttachOrderMarker m) m.AttachOrder = attachOrder;

            int idx = list.FindIndex(x =>
            {
                int byPriority = x.Priority.CompareTo(binder.Priority);
                if (byPriority != 0) return byPriority > 0;
                int a1 = (x is IAttachOrderMarker mm) ? mm.AttachOrder : int.MaxValue;
                return a1 > attachOrder;
            });

            if (idx < 0) list.Add(binder); else list.Insert(idx, binder);
        }

        private void ValidateRequiredContexts(IBinder binder, Entity e, AttachOptions options)
        {
            var need = binder.GetType().GetInterfaces()
                             .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IRequireContext<>))
                             .Select(t => t.GetGenericArguments()[0])
                             .Distinct().ToArray();

            foreach (var tCtx in need)
            {
                var has = HasContextDynamic(tCtx, e);
                if (!has)
                {
                    var msg = $"[BindingRouter] Missing required context {tCtx.Name} for binder {binder.GetType().Name} on {e}.";
                    if (options == AttachOptions.Strict)
                    {
                        throw new InvalidOperationException(msg);
                    }
                    else
                    {
                        // just warning
                    }
                }
            }
        }

        private bool HasContextDynamic(Type ctxType, Entity e)
        {
            var mHas = _ctx.GetType().GetMethods()
                .FirstOrDefault(mi => mi.IsGenericMethodDefinition && mi.Name == "Has"
                                      && mi.GetParameters().Length == 2
                                      && mi.GetParameters()[0].ParameterType == typeof(World)
                                      && mi.GetParameters()[1].ParameterType == typeof(Entity));
            if (mHas != null)
                return (bool)mHas.MakeGenericMethod(ctxType).Invoke(_ctx, new object[] { _world, e });

            mHas = _ctx.GetType().GetMethods()
                .FirstOrDefault(mi => mi.IsGenericMethodDefinition && mi.Name == "Has"
                                      && mi.GetParameters().Length == 1
                                      && mi.GetParameters()[0].ParameterType == typeof(Entity));
            if (mHas != null)
                return (bool)mHas.MakeGenericMethod(ctxType).Invoke(_ctx, new object[] { e });

            var mTry = _ctx.GetType().GetMethods()
                .FirstOrDefault(mi => mi.IsGenericMethodDefinition && mi.Name == "TryGet"
                                      && mi.GetParameters().Length == 3
                                      && mi.GetParameters()[0].ParameterType == typeof(World)
                                      && mi.GetParameters()[1].ParameterType == typeof(Entity)
                                      && mi.GetParameters()[2].IsOut);
            if (mTry != null)
            {
                var args = new object[] { _world, e, null! };
                return (bool)mTry.MakeGenericMethod(ctxType).Invoke(_ctx, args);
            }

            mTry = _ctx.GetType().GetMethods()
                .FirstOrDefault(mi => mi.IsGenericMethodDefinition && mi.Name == "TryGet"
                                      && mi.GetParameters().Length == 2
                                      && mi.GetParameters()[0].ParameterType == typeof(Entity)
                                      && mi.GetParameters()[1].IsOut);
            if (mTry != null)
            {
                var args = new object[] { e, null! };
                return (bool)mTry.MakeGenericMethod(ctxType).Invoke(_ctx, args);
            }

            return false;
        }
    }
}
