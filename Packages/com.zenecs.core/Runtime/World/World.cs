#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ZenECS.Core.Events;
using ZenECS.Core.Internal;
using ZenECS.Core.Serialization;

namespace ZenECS.Core
{
    public sealed partial class World
    {
        private readonly WorldConfig cfg;
        private BitSet alive;
        private int nextId = 1;
        private Stack<int> freeIds;
        private int[] generation; // 세대(Generation) 배열: slot별 현재 세대 카운터

        public World(WorldConfig? config = null)
        {
            cfg        = config ?? new WorldConfig();

            alive      = new BitSet(cfg.InitialEntityCapacity);                        // 엔티티 슬롯 점유 비트맵
            generation = new int[cfg.InitialEntityCapacity];                           // 슬롯별 세대 카운터(0부터 시작)
            freeIds    = new Stack<int>(cfg.InitialFreeIdCapacity);                    // 파괴된 ID 재사용 저장소
            pools      = new Dictionary<Type, IComponentPool>(cfg.InitialPoolBuckets); // 타입→풀
            nextId     = 1;                                                            // 신규 엔티티는 1부터
        }
    }
}
