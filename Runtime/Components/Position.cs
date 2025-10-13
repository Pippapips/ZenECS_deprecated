using System;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using ZenECS.Core;
using ZenECS.Core.Serialization;
using ZenECS.Core.Serialization.Formats.Binary;
using float3 = Unity.Mathematics.float3;

namespace ZenECS.Adapter.Unity.Components.Common
{
    [ZenComponent(StableId = "com.zenecs.position.v1")]
    public readonly struct Position : IEquatable<Position>
    {
        public static readonly Position Default = new Position(float3.zero);
        public readonly float3 Value;

        public Position(in float3 value) => Value = value;

        public Position(float x, float y, float z) : this(new float3(x, y, z))
        {
        }

        public bool Equals(Position other)
        {
            const float eps = 1e-6f;
            return all(abs(Value - other.Value) <= new float3(eps));
        }

        public override bool Equals(object obj) => obj is Position p && Equals(p);
        public override int GetHashCode() => (int)hash(Value);
    }
    
    [ZenFormatterFor(typeof(Position), "com.zenecs.position.v1")]
    public sealed class PositionFormatterV1 : BinaryComponentFormatter<Position>
    {
        public override void Write(in Position value, ISnapshotBackend backend)
        {
            backend.WriteFloat(value.Value.x);
            backend.WriteFloat(value.Value.y);
            backend.WriteFloat(value.Value.z);
        }

        public override Position ReadTyped(ISnapshotBackend backend)
        {
            var x = backend.ReadFloat();
            var y = backend.ReadFloat();
            var z = backend.ReadFloat();
            return new Position(new float3(x, y, z));
        }
    }
}