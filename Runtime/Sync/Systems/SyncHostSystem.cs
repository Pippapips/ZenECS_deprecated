#nullable enable
using System;
using System.Collections.Generic;
#if ZENECS_TRACE
using ZenECS.Core.Diagnostics;
#endif
using ZenECS.Core.Sync.Util;
using ZenECS.Core.Systems;

namespace ZenECS.Core.Sync.Systems
{
    [UpdateGroup(typeof(PresentationGroup)), OrderAfter(typeof(ChangeBatchDispatchSystem))]
    public sealed class SyncHostSystem : IInitSystem, ILateRunSystem, IDisposeSystem
    {
        private readonly Dictionary<(int, System.Type), ISyncHandler> _active = new();

        // Rebind 대기 엔티티 (중복 방지)
        private readonly HashSet<int> _rebindPending = new();

        private readonly ISyncTargetRegistry _targetRegistry;
        private readonly ISyncHandlerRegistry _handlerRegistry;
        
#if ZENECS_TRACE
        [InjectOptional] private EcsTraceCenter? _traceCenter;
#endif

        public SyncHostSystem(ISyncTargetRegistry targetRegistry, ISyncHandlerRegistry handlerRegistry)
        {
            _handlerRegistry = handlerRegistry;
            _targetRegistry = targetRegistry;
        }

        public void Init(World w)
        {
            _targetRegistry.Registered += OnTargetRegistered;
        }

        public void Dispose(World w)
        {
            _targetRegistry.Registered -= OnTargetRegistered;
        }

        private void OnTargetRegistered(Entity e, ISyncTarget _) => RequestRebind(e);

        /// 외부에서 직접 호출해도 됨(씬/세이브로드/LOD 전환 등)
        public void RequestRebind(Entity e) => _rebindPending.Add(e.Id);

        public void OnComponentAdded<T>(World w, Entity e, in T v)
        {
            var key = (e.Id, typeof(T));

            // 1) 타깃 조회 (없으면 표현 스킵)
            var target = _targetRegistry.Resolve(e);
            if (target is null) return;

            // 2) 핸들러 조회 (등록 안 되어 있으면 스킵)
            var handler = _handlerRegistry.Resolve<T>();
            if (handler is null) return;

            // 3) 이미 활성화돼 있다면(중복 Added 방어) 'Apply'만 수행하고 종료
            if (_active.TryGetValue(key, out var existing))
            {
                // existing은 ISyncHandler 이므로 제네릭 캐스팅 필요
                if (existing is ISyncHandler typed)
                {
                    typed.Apply(w, e, v, target);
#if ZENECS_TRACE
                    _traceCenter?.ViewBinding.OnApply();
#endif
                }

                return;
            }

            // 4) 최초 바인딩: Bind → Apply → 활성 테이블 등록
            handler.Bind(w, e, target);
            handler.Apply(w, e, v, target);
#if ZENECS_TRACE
            _traceCenter?.ViewBinding.OnBind();
            _traceCenter?.ViewBinding.OnApply();
#endif
            _active[key] = handler;
        }

        public void OnComponentChanged<T>(World w, Entity e, in T v)
        {
            var key = (e.Id, typeof(T));

            // 1) 타깃 없으면 표현 스킵
            var target = _targetRegistry.Resolve(e);
            if (target is null) return;

            // 2) 활성 핸들러가 있으면 Apply만
            if (_active.TryGetValue(key, out var h) && h is ISyncHandler typedExisting)
            {
                typedExisting.Apply(w, e, v, target);
#if ZENECS_TRACE
                _traceCenter?.ViewBinding.OnApply();
#endif
                return;
            }

            // 3) 활성 없으면(Added를 못 받았거나 Rebind 이전 등) → 새로 Bind 후 Apply
            var handler = _handlerRegistry.Resolve<T>();
            if (handler is null) return;

            handler.Bind(w, e, target);
            handler.Apply(w, e, v, target);
#if ZENECS_TRACE
            _traceCenter?.ViewBinding.OnBind();
            _traceCenter?.ViewBinding.OnApply();
#endif
            _active[key] = handler;
        }

        public void OnComponentRemoved<T>(World w, Entity e)
        {
            var key = (e.Id, typeof(T));

            if (!_active.TryGetValue(key, out var h))
                return; // 이미 비활성(중복 Removed 등)

            // 타깃을 가져오되, 없어도 안전하게 정리
            var target = _targetRegistry.Resolve(e);
            if (target is not null && h is ISyncHandler typed)
            {
                // 정상 언바인드
                typed.Unbind(w, e, target);
#if ZENECS_TRACE
                _traceCenter?.ViewBinding.OnUnbind();
#endif
            }

            // 타깃이 없으면(이미 파괴됨) 핸들러만 테이블에서 제거
            _active.Remove(key);
        }

        public void LateRun(World w)
        {
            if (_rebindPending.Count == 0) return;

            foreach (var id in _rebindPending)
            {
                var e = new Entity(id);
                var target = _targetRegistry.Resolve(e);
                if (target is null) continue; // 아직 타깃이 없으면 다음 기회

                // 엔티티의 “현재 컴포넌트들”을 모두 Apply
                foreach (var (type, boxed) in w.GetAllComponents(e))
                {
                    var handler = _handlerRegistry.Resolve(type);
                    if (handler == null) continue;
                    
                    var key = (id, type);
                    if (!_active.TryGetValue(key, out var h))
                    {
                        handler.Bind(w, e, target);
                        handler.Apply(w, e, boxed, target);
#if ZENECS_TRACE
                        _traceCenter?.ViewBinding.OnBind();
                        _traceCenter?.ViewBinding.OnApply();
#endif
                        _active[key] = handler;
                        continue;
                    }

                    handler.Apply(w, e, boxed, target);
#if ZENECS_TRACE
                    _traceCenter?.ViewBinding.OnApply();
#endif
                }
            }

            _rebindPending.Clear();
        }
    }
}