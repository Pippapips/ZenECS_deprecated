#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Core.Systems;

namespace ZenECS.Core.Binding.Systems
{
    [PresentationGroup]
    public sealed class ViewBindingSystem : ISystemLifecycle, IPresentationSystem
    {
        // Key: (Entity.Id, read-only struct Component)
        private readonly Dictionary<(Entity, Type), IComponentBinder> _activeComponentBinders = new();

        // Rebind 대기 엔티티 (중복 방지)
        private readonly HashSet<Entity> _rebindPending = new();

        private readonly IViewBinderRegistry _viewBinderRegistry;
        private readonly IComponentBinderRegistry _componentBinderRegistry;

        public ViewBindingSystem(IViewBinderRegistry viewBinderRegistry,
            IComponentBinderRegistry componentBinderRegistry)
        {
            _componentBinderRegistry = componentBinderRegistry;
            _viewBinderRegistry = viewBinderRegistry;
        }

        public void Initialize(World w)
        {
            _viewBinderRegistry.Registered += OnViewBinderRegistered;
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
                var viewBinder = _viewBinderRegistry.Resolve(e);
                if (viewBinder is null) continue; // 아직 타깃이 없으면 다음 기회

                // 엔티티의 “현재 컴포넌트들”을 모두 Apply
                // 엔티티의 등록된 컴포넌들을 순회하며
                // 해당 컴포넌트의 등록된 핸들러를 찾고
                // 있으면 핸들러를 통해 핸들링을 한다.
                // 이때 핸들링 되었던 목록에 없으면 Bind/Apply한 후 목록에 기록한다.
                // 추후 재진입시에는 Bind는 건너띄고 바로 Apply만 한다.
                // Bind로 기록된 핸들링은 Unbind를 통해 기록이 삭제되어 다음 핸들링시에는 새로 Bind한다.
                foreach ((var type, object? boxed) in w.GetAllComponents(e))
                {
                    var componentBinder = _componentBinderRegistry.Resolve(type);
                    if (componentBinder == null) continue;

                    var key = (e, type);
                    if (!_activeComponentBinders.TryGetValue(key, out var h))
                    {
                        componentBinder.Bind(w, e, viewBinder);
                        if (boxed != null) componentBinder.Apply(w, e, boxed, viewBinder);
                        _activeComponentBinders[key] = componentBinder;
                        continue;
                    }

                    if (boxed != null) componentBinder.Apply(w, e, boxed, viewBinder);
                }
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
                return;
            }

            // 4) 최초 바인딩: Bind → Apply → 활성 테이블 등록
            componentBinder.Bind(w, e, viewBinder);
            componentBinder.Apply(w, e, v, viewBinder);
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
                return;
            }

            // 3) 활성 없으면(Added를 못 받았거나 Rebind 이전 등) → 새로 Bind 후 Apply
            var componentBinder = _componentBinderRegistry.Resolve<T>();
            if (componentBinder is null) return;

            componentBinder.Bind(w, e, viewBinder);
            componentBinder.Apply(w, e, v, viewBinder);
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
            }

            // 타깃이 없으면(이미 파괴됨) 핸들러만 테이블에서 제거
            _activeComponentBinders.Remove(key);
        }
    }
}
