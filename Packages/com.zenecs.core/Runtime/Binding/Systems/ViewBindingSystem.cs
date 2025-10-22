#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ZenECS.Core.Systems;
using ZenECS.Core.Binding.Util;

namespace ZenECS.Core.Binding.Systems
{
    [PresentationGroup]
    public sealed class ViewBindingSystem : ISystemLifecycle, IPresentationSystem
    {
        // Key: (Entity, read-only struct Component)
        private void ApplyWithCaches(World w, Entity e, System.Type componentType, IViewBinder vb, IComponentBinder binder, object? boxedValueIfNeeded)
        {
            var sig = new[] { typeof(Entity), componentType.MakeByRefType() };
            
            // World에 제네릭 TryGet<T>(Entity, out T) 또는 TryGetComponentInternal<T>(...)가 정의되어 있으면 FastPath 사용
            var hasGenericTryGet =
                typeof(World)
                    .GetMethods(System.Reflection.BindingFlags.Instance |
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic)
                    .Any(m =>
                        (m.Name == "TryGet" || m.Name == "TryGetComponentInternal") &&
                        m.IsGenericMethodDefinition &&
                        m.GetGenericArguments().Length == 1 &&
                        m.GetParameters().Length == 2 &&
                        m.GetParameters()[0].ParameterType == typeof(Entity) &&
                        m.GetParameters()[1].ParameterType.IsByRef);
            if (hasGenericTryGet)
                BinderFastInvokerCache.GetApplyNoBox(componentType)(w, e, vb, binder);
            else
                BinderInvokerCache.GetApply(componentType)(w, e, boxedValueIfNeeded!, vb, binder);
        }


        private readonly Dictionary<(Entity, Type), IComponentBinder> _activeComponentBinders = new();

        // Rebind 대기 엔티티 (중복 방지)
        private readonly HashSet<Entity> _rebindPending = new();
        private readonly Dictionary<(Entity, System.Type), int> _lastHashes = new();

        private readonly IViewBinderRegistry _viewBinderRegistry;
        private readonly IComponentBinderResolver _resolver;
        private readonly IComponentBinderRegistry _componentBinderRegistry;

        public ViewBindingSystem(IViewBinderRegistry viewBinderRegistry,
            IComponentBinderRegistry componentBinderRegistry,
            IComponentBinderResolver resolver)
        {
            _componentBinderRegistry = componentBinderRegistry;
            _viewBinderRegistry = viewBinderRegistry;
            _resolver = resolver;
        }

        public void Initialize(World w)
        {
            _viewBinderRegistry.Registered += OnViewBinderRegistered;

            // snapshot reconciliation API
        }

        public void Shutdown(World w)
        {
            _viewBinderRegistry.Registered -= OnViewBinderRegistered;
        }

        public void Run(World w)
        {
            if (_rebindPending.Count == 0) return;

            foreach (var e in _rebindPending)
            {
                var vb = _viewBinderRegistry.Resolve(e);
                if (vb is null) continue;
                ReconcileEntity(w, e, vb);
            }

            _rebindPending.Clear();
        }

        public void Run(World w, float alpha = 1)
        {
            Run(w);
        }

        // 지연된 처리를 위해서 파라미터로 들어오는 ViewBinder는 무시하고 해당 시점에 Resolve하도록 한다.
        private void OnViewBinderRegistered(Entity e, IViewBinder _) => RequestRebind(e);

        // 외부에서 직접 호출해도 됨(씬/세이브로드/LOD 전환 등)
        public void RequestRebind(Entity e) => _rebindPending.Add(e);

        public void OnComponentAdded<T>(World w, Entity e, in T v)
        {
            if (v == null) return;

            var key = (e, typeof(T));

            // 1) 뷰 바인더 조회 (없으면 표현 스킵)
            var viewBinder = _viewBinderRegistry.Resolve(e);
            if (viewBinder is null) return;

            // 2) 컴포넌트 바인더 조회 (등록 안 되어 있으면 스킵)
            var componentBinder = _componentBinderRegistry.Resolve<T>();
            if (componentBinder is null) return;

            // 3) 이미 활성화돼 있다면(중복 Added 방어) 'Apply'만 수행하고 종료
            if (_activeComponentBinders.TryGetValue(key, out var existing))
            {
                existing?.Apply(w, e, v, viewBinder);
#if ZENECS_TRACE
                _traceCenter?.ViewBinding.OnApply();
#endif
                return;
            }

            // 4) 최초 바인딩: Bind → Apply → 활성 테이블 등록
            componentBinder.Bind(w, e, viewBinder);
            componentBinder.Apply(w, e, v, viewBinder);
#if ZENECS_TRACE
            _traceCenter?.ViewBinding.OnBind();
            _traceCenter?.ViewBinding.OnApply();
#endif
            _activeComponentBinders[key] = componentBinder;
        }

        public void OnComponentChanged<T>(World w, Entity e, in T v)
        {
            if (v == null) return;

            var key = (e, typeof(T));

            // 1) 뷰 바인더 없으면 표현 스킵
            var viewBinder = _viewBinderRegistry.Resolve(e);
            if (viewBinder is null) return;

            // 2) 활성 컴포넌트 바인더가 있으면 Apply만
            if (_activeComponentBinders.TryGetValue(key, out var h) && h is IComponentBinder typedExisting)
            {
                typedExisting.Apply(w, e, v, viewBinder);
#if ZENECS_TRACE
                _traceCenter?.ViewBinding.OnApply();
#endif
                return;
            }

            // 3) 활성 없으면(Added를 못 받았거나 Rebind 이전 등) → 새로 Bind 후 Apply
            var componentBinder = _componentBinderRegistry.Resolve<T>();
            if (componentBinder is null) return;

            componentBinder.Bind(w, e, viewBinder);
            componentBinder.Apply(w, e, v, viewBinder);
#if ZENECS_TRACE
            _traceCenter?.ViewBinding.OnBind();
            _traceCenter?.ViewBinding.OnApply();
#endif
            _activeComponentBinders[key] = componentBinder;
        }

        public void OnComponentRemoved<T>(World w, Entity e)
        {
            var key = (e, typeof(T));

            if (!_activeComponentBinders.TryGetValue(key, out var h))
                return; // 이미 비활성(중복 Removed 등)

            // 뷰 바인더를 가져오되, 없어도 안전하게 정리
            var viewBinder = _viewBinderRegistry.Resolve(e);
            if (viewBinder is not null && h is IComponentBinder typed)
            {
                // 정상 언바인드
                typed.Unbind(w, e, viewBinder);
#if ZENECS_TRACE
                _traceCenter?.ViewBinding.OnUnbind();
#endif
            }

            // 타깃이 없으면(이미 파괴됨) 핸들러만 테이블에서 제거
            _activeComponentBinders.Remove(key);
        }

        public void RequestReconcile(World w, Entity e)
        {
            // 배치 Feed로 보내지 않고, 내부 펜딩 큐에만 표시
            _rebindPending.Add(e);
        }

        private void ReconcileEntity(World w, Entity e, IViewBinder vb)
        {
            foreach (var t in _resolver.RegisteredComponentTypes)
            {
                if (w.TryGetBoxed(e, t, out var value) && value is not null)
                {
                    var key = (e, t);
                    var h = value.GetHashCode();
                    if (!_lastHashes.TryGetValue(key, out var prev) || prev != h)
                    {
                        if (!_activeComponentBinders.TryGetValue(key, out var binder))
                        {
                            if (!_resolver.TryResolve(t, out binder) || binder is null) continue;
                            BinderInvokerCache.GetBind(t)(w, e, vb, binder);
                            _activeComponentBinders[key] = binder;
                        }
                        ApplyWithCaches(w, e, t, vb, binder, value);
                        _lastHashes[key] = h;
                    }
                }
                else
                {
                    var key = (e, t);
                    if (_activeComponentBinders.TryGetValue(key, out var binder))
                    {
                        BinderInvokerCache.GetUnbind(t)(w, e, vb, binder);
                        _activeComponentBinders.Remove(key);
                        _lastHashes.Remove(key);
                    }
                }
            }
        }
    }
}