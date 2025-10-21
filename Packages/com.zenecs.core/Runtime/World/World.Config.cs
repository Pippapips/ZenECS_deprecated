#nullable enable
using System;

namespace ZenECS.Core
{
    public enum GrowthPolicy
    {
        Doubling, // 확장 시 2배(최소 +256 보장)로 늘려 재할당 횟수 최소화
        Step      // 확장 시 고정 스텝(GrowthStep)만큼 늘려 메모리 사용 예측 가능
    }

    public readonly struct WorldConfig
    {
        /// <summary>초기 엔티티 슬롯 수(Alive/Generation 등 엔티티 관련 배열의 초기 길이).</summary>
        public readonly int InitialEntityCapacity;

        /// <summary>풀 딕셔너리의 초기 버킷 수(해시 재해시/확장 빈도를 줄임).</summary>
        public readonly int InitialPoolBuckets;

        /// <summary>반납된 엔티티 ID를 담아둘 스택의 초기 용량(재사용 빈도가 높다면 키우기).</summary>
        public readonly int InitialFreeIdCapacity;

        /// <summary>용량 부족 시 배열 확장 정책(2배 vs 고정 스텝 증설).</summary>
        public readonly GrowthPolicy GrowthPolicy;

        /// <summary>Step 정책에서 한 번에 늘릴 슬롯 수(예: 256, 512, 1024 등).</summary>
        public readonly int GrowthStep;

        public WorldConfig(
            int initialEntityCapacity = 256,
            int initialPoolBuckets = 256,
            int initialFreeIdCapacity = 128,
            GrowthPolicy growthPolicy = GrowthPolicy.Doubling,
            int growthStep = 256)
        {
            InitialEntityCapacity = Math.Max(16, initialEntityCapacity);
            InitialPoolBuckets = Math.Max(16, initialPoolBuckets);
            InitialFreeIdCapacity = Math.Max(16, initialFreeIdCapacity);
            GrowthPolicy = growthPolicy;
            GrowthStep = Math.Max(32, growthStep);
        }
    }
}
