#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ZenECS.Core.Events;

namespace ZenECS.Core
{
    public partial class World
    {
        // 엔티티 생명주기

        /// <summary>
        /// 지정 id가 있으면 용량 확보 후 바로 활성화,
        /// 없으면 freeIds를 우선 재사용하고 없을 때 nextId 증가로 신규 발급.
        /// 생성 이벤트 발생.
        /// </summary>
        /// <param name="fixedId"></param>
        /// <returns></returns>
        public Entity CreateEntity(int? fixedId = null)
        {
            int id;
            if (fixedId.HasValue)
            {
                id = fixedId.Value;
                EnsureEntityCapacity(id);
                alive.Set(id, true);
            }
            else if (freeIds.Count > 0)
            {
                id = freeIds.Pop();
                EnsureEntityCapacity(id);
                alive.Set(id, true);
            }
            else
            {
                id = nextId++;
                EnsureEntityCapacity(id);
                alive.Set(id, true);
            }

            // 현재 슬롯 세대를 핸들에 포함
            var e = new Entity(id, generation[id]);
            EntityEvents.RaiseCreated(this, e);
            return e;
        }

        /// <summary>
        /// 생존 체크 후 파괴 요청 이벤트
        /// → 모든 풀에서 해당 id 컴포 제거
        /// → alive 비활성
        /// → freeIds에 반납
        /// → 파괴 이벤트 발생.
        /// </summary>
        /// <param name="e"></param>
        public void DestroyEntity(Entity e)
        {
            if (!IsAlive(e)) return;
            EntityEvents.RaiseDestroyRequested(this, e);
            foreach (var kv in pools) kv.Value.Remove(e.Id);
            alive.Set(e.Id, false);
            // 🔼 세대 증가! 이후 같은 id 재사용 시에도 핸들이 달라짐
            generation[e.Id]++;
            freeIds.Push(e.Id);
            EntityEvents.RaiseDestroyed(this, e);
        }

        /// <summary>
        /// 전체 엔티티를 파괴. fireEvents=true면 개별 Destroy 이벤트를 발행(느림).
        /// 빠른 초기화가 목적이면 Reset 계열을 쓰는 것을 권장.
        /// </summary>
        public void DestroyAllEntities(bool fireEvents = false)
        {
            if (!fireEvents)
            {
                // 이벤트 생략: 빠른 경로 → 사실상 Reset과 유사
                ResetButKeepCapacity();
                return;
            }

            // 이벤트를 쏴야 한다면 실제 Destroy를 모두 호출
            // (BitSet을 스캔해 살아있는 엔티티만 순회)
            for (int id = 1; id < alive.Length; id++)
            {
                if (alive.Get(id))
                {
                    DestroyEntity(new Entity(id, generation[id])); // 기존 DestroyEntity 경로: 이벤트/풀 제거/세대++ 포함
                }
            }
        }

        public List<Entity> GetAllEntities()
        {
            var list = new List<Entity>(nextId);
            for (int id = 1; id < nextId; id++)
                if (alive.Get(id))
                    list.Add(new Entity(id, generation[id]));
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAlive(Entity e) => alive.Get(e.Id) && generation[e.Id] == e.Gen;
        public int AliveCount => GetAllEntities().Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureEntityCapacity(int id)
        {
            // BitSet: Set 호출이 내부적으로 확장/보존하도록 구현됨
            if (!alive.Get(id)) alive.Set(id, false);

            // Generation 배열 확장: 정책 기반
            if (id >= generation.Length)
            {
                int required = id + 1;
                int newLen = ComputeNewCapacity(generation.Length, required);
                Array.Resize(ref generation, newLen);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ComputeNewCapacity(int current, int required)
        {
            if (cfg.GrowthPolicy == GrowthPolicy.Step)
            {
                int step = cfg.GrowthStep;
                // step 배수로 올림
                int blocks = (required + step - 1) / step;
                return Math.Max(required, blocks * step);
            }
            else // Doubling
            {
                int cap = Math.Max(16, current);
                while (cap < required)
                {
                    int next = cap * 2;
                    // 너무 작은 증가를 피하기 위해 최소 +256 보장
                    if (next - cap < 256) next = cap + 256;
                    cap = next;
                }
                return cap;
            }
        }
    }
}
