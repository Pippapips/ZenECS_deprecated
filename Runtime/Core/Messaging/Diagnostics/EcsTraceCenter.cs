#nullable enable
#if ZENECS_TRACE
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using ZenECS.Core;
using ZenECS.Core.Events;
using ZenECS.Core.Messaging.Diagnostics;
using ZenECS.Core.Sync.Systems;

namespace ZenECS.Core.Diagnostics
{
    /// <summary>옵트인 진단 허브(런타임 전역 단일 인스턴스)</summary>
    public sealed class EcsTraceCenter
    {
        private MessageCounters _messageCounters;
        
        // --- Singleton ---
        public EcsTraceCenter(MessageCounters messageCounters)
        {
            Infrastructure.EcsRuntimeDirectory.AttachTraceCenter(this);
            _messageCounters = messageCounters;
            
            // 월드/컴포넌트 이벤트 결합 (구독자가 있을 때만 비용 발생)
            EntityEvents.EntityCreated         += (_, __) => WorldTrace.Inc(ref World.EntityCreated);
            EntityEvents.EntityDestroyRequested+= (_, __) => WorldTrace.Inc(ref World.EntityDestroyRequested);
            EntityEvents.EntityDestroyed       += (_, __) => WorldTrace.Inc(ref World.EntityDestroyed);

            ComponentEvents.ComponentAdded    += (_, __, t) => WorldTrace.IncType(World.ComponentAddedByType, t);
            ComponentEvents.ComponentChanged  += (_, __, t) => WorldTrace.IncType(World.ComponentChangedByType, t);
            ComponentEvents.ComponentRemoved  += (_, __, t) => WorldTrace.IncType(World.ComponentRemovedByType, t);
        }

        // ============== WORLD TRACE ==============
        public sealed class WorldTrace
        {
            public long EntityCreated;
            public long EntityDestroyRequested;
            public long EntityDestroyed;

            // 타입별 카운터
            public readonly ConcurrentDictionary<Type, long> ComponentAddedByType   = new();
            public readonly ConcurrentDictionary<Type, long> ComponentChangedByType = new();
            public readonly ConcurrentDictionary<Type, long> ComponentRemovedByType = new();

            internal static void Inc(ref long v) => System.Threading.Interlocked.Increment(ref v);
            internal static void IncType(ConcurrentDictionary<Type,long> dict, Type t)
                => dict.AddOrUpdate(t, 1, (_, old) => old + 1);
        }
        public readonly WorldTrace World = new();

        // ============== SYSTEM TRACE ==============
        public sealed class SystemRunStats
        {
            public long Calls;            // 누적 호출 수
            public double LastMs;         // 마지막 호출 소요
            public double AvgMs;          // 지수평균
            public int Exceptions;        // 누적 예외 수
            public int FrameCalls;        // 프레임 내 호출 수(ResetFrame에서 0으로)
        }

        readonly ConcurrentDictionary<string, SystemRunStats> _system = new();
        public SystemRunStats GetSystem(string name) => _system.GetOrAdd(name, _ => new SystemRunStats());
        const double Alpha = 0.2; // EMA

        public IDisposable SystemScope(string systemName)
        {
            var stats = GetSystem(systemName);
            var sw = Stopwatch.StartNew();
            stats.FrameCalls++;
            return new Scope(() =>
            {
                sw.Stop();
                var ms = sw.Elapsed.TotalMilliseconds;
                stats.LastMs = ms;
                stats.Calls++;
                stats.AvgMs = stats.Calls == 1 ? ms : (1 - Alpha) * stats.AvgMs + Alpha * ms;
            },
            () =>
            {
                // 예외 발생 경로(Dispose 전에 OnException 호출)
                stats.Exceptions++;
            });
        }

        internal sealed class Scope : IDisposable
        {
            readonly Action _onDispose;
            readonly Action _onException;
            bool _failed;
            public Scope(Action onDispose, Action onException) { _onDispose = onDispose; _onException = onException; }
            public void MarkException() => _failed = true;
            public void Dispose()
            {
                if (_failed) _onException();
                _onDispose();
            }
        }

        public void ResetFrame()
        {
            foreach (var kv in _system) kv.Value.FrameCalls = 0;
            ViewBinding.ResetFrame();
        }

        // ============== MESSAGE TRACE ==============
        public MessageCounters Message => _messageCounters; // 타입별 Publish/Consume 카운터

        // ============== VIEW BINDING TRACE ==============
        public sealed class ViewBindingTrace
        {
            public long Bind;
            public long Unbind;
            public long Apply;
            public long Failures;
            public int FrameBind, FrameUnbind, FrameApply, FrameFailures;

            internal void OnBind(bool ok = true)   { System.Threading.Interlocked.Increment(ref Bind);   if (ok) FrameBind++; else FrameFailures++; }
            internal void OnUnbind(bool ok = true) { System.Threading.Interlocked.Increment(ref Unbind); if (ok) FrameUnbind++; else FrameFailures++; }
            internal void OnApply(bool ok = true)  { System.Threading.Interlocked.Increment(ref Apply);  if (ok) FrameApply++; else FrameFailures++; }

            internal void ResetFrame() { FrameBind = FrameUnbind = FrameApply = FrameFailures = 0; }
        }
        public readonly ViewBindingTrace ViewBinding = new();
    }
}
#endif
