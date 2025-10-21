#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ZenECS.Core.Internal;

namespace ZenECS.Core
{
    public sealed partial class World
    {
        /// <summary>Reset 직전에 서브시스템에게 통지(선택 구현).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        partial void OnBeforeWorldReset(bool keepCapacity);

        /// <summary>Reset 직후에 서브시스템에게 통지(선택 구현).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        partial void OnAfterWorldReset(bool keepCapacity);

        /// <summary>Reset에 포함할 공통 초기화(명령버퍼/잡/타이밍/훅/쿼리캐시 등).</summary>
        private void ResetSubsystems(bool keepCapacity)
        {
            OnBeforeWorldReset(keepCapacity);

            // 1) 명령버퍼
            ClearAllCommandBuffers();

            // 2) 잡 스케줄러
            ClearAllScheduledJobs();

            // 3) 훅/이벤트 큐
            ClearAllHookQueues();

            // 4) 타이밍/프레임 카운터
            ResetTimingCounters();

            // 5) 쿼리/필터/캐시(있다면)
            ResetQueryCaches();

            OnAfterWorldReset(keepCapacity);
        }

        /// <summary>
        /// 현재 용량(capacity)은 그대로 유지하면서 월드 상태를 초기화(데이터만 비움).
        /// 월드 초기화 가장 빠름!
        /// </summary>
        public void ResetButKeepCapacity()
        {
            // 현재 엔티티 관련 용량(둘 중 큰 쪽 사용)
            int entityCap = Math.Max(alive.Length, generation?.Length ?? 0);

            // alive / generation 초기화 (용량 보존)
            alive = new BitSet(entityCap);
            if (generation == null || generation.Length != entityCap)
                generation = new int[entityCap];
            else
                Array.Clear(generation, 0, generation.Length);

            // ID 재할당 상태 초기화
            nextId = 1;                 // 0은 예약
            if (freeIds == null)
                freeIds = new Stack<int>(cfg.InitialFreeIdCapacity);
            else
                freeIds.Clear();        // 스택은 그대로 두고 비움

            // 각 컴포넌트 풀도 "비운 상태"로 재생성(용량 유지)
            // (인터페이스에 Clear가 없다고 가정: 동일 타입의 새 풀로 교체)
            // 키 목록을 먼저 복사(사전 변경 중 열거 방지)
            var types = new List<Type>(pools.Keys);
            foreach (var t in types)
            {
                pools[t] = CreateEmptyPoolForType(t, entityCap);
            }

            ResetSubsystems(keepCapacity: true);
        }

        /// <summary>
        /// 월드를 완전 초기화: 모든 자료구조를 WorldConfig의 초기 용량으로 재구성.
        /// 메모리까지 되감기
        /// </summary>
        public void HardReset()
        {
            // 엔티티 관련 구조 재구성
            alive = new BitSet(cfg.InitialEntityCapacity);
            generation = new int[cfg.InitialEntityCapacity];

            // ID 재할당 상태 초기화
            nextId = 1;
            freeIds = new Stack<int>(cfg.InitialFreeIdCapacity);

            // 풀 사전도 완전 초기화(등록 정보 자체를 비움)
            pools.Clear();

            ResetSubsystems(keepCapacity: false);
        }

        // ──────────────────────────────────────────────────────────────────────
        // 내부 유틸

        /// <summary>
        /// 지정 타입의 컴포넌트 풀을 비운 상태로 생성하되, 주어진 용량(cap)을 확보해서 반환.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IComponentPool CreateEmptyPoolForType(Type compType, int cap)
        {
            // ComponentPool<T>(int initialCapacity)가 있으면 우선 사용
            var closed = typeof(ComponentPool<>).MakeGenericType(compType);
            var ctorWithCap = closed.GetConstructor(new[] { typeof(int) });
            if (ctorWithCap != null)
            {
                return (IComponentPool)Activator.CreateInstance(closed, cap)!;
            }

            // 없으면 기존 팩토리로 생성 후 EnsureCapacity로 확보
            var factory = GetOrBuildPoolFactory(compType);
            var pool = factory();
            // cap이 비트/인덱스 기반이므로 cap-1까지 접근 가능하도록 확보
            if (cap > 0) pool.EnsureCapacity(cap - 1);
            return pool;
        }
    }
}
