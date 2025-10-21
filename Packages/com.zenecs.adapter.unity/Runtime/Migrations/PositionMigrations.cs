using Unity.Mathematics;
using ZenECS.Core.Serialization;
using ZenECS.Adapter.Unity.Components.Common; // Position 타입 네임스페이스
using ZenECS.Core;
using ZenECS.Core.Extensions;

namespace ZenECS.Adapter.Unity.Migrations
{
    /// <summary>
    /// 예시) v1에서 넘어온 Position은 IntValue가 0으로 들어오므로,
    ///      특정 규칙으로 보정하고 싶다면 여기서 일괄 보정.
    ///      (idempotent: 이미 보정된 값은 다시 덮지 않도록 작성)
    /// </summary>
    public sealed class PositionFillIntValueIfMissing : IPostLoadMigration
    {
        public int Order => 100; // 필요 시 다른 마이그보다 늦게/빨리 조절

        public void Run(World world)
        {
            foreach (var e in world.Query<Position>())
            {
                var p = world.Read<Position>(e);
                // 예시 규칙: 값이 (0,0,0)이 아니면서 IntValue==0이면 기본 10으로
                if (!p.Value.Equals(float3.zero) && p.IntValue == 0)
                {
                    world.Replace(e, new Position(p.Value, intValue: 10));
                }
            }
        }
    }
}