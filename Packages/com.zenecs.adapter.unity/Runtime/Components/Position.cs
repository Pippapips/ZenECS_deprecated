using System;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using ZenECS.Core;
using ZenECS.Core.Serialization;
using ZenECS.Core.Serialization.Formats.Binary;
using float3 = Unity.Mathematics.float3;

namespace ZenECS.Adapter.Unity.Components.Common
{
    [ZenComponent(StableId = "com.zenecs.position.v2")]
    public readonly struct Position : IEquatable<Position>
    {
        public static readonly Position Default = new Position(float3.zero, 10);
        public readonly float3 Value;
        public readonly int IntValue;

        public Position(in float3 value)
        {
            Value = value;
            IntValue = Default.IntValue;
        }

        public Position(in float3 value, int intValue)
        {
            Value = value;
            IntValue = intValue;
        }

        public Position(float x, float y, float z) : this(new float3(x, y, z)) { }

        public Position(float x, float y, float z, int intValue) : this(new float3(x, y, z), intValue) { }

        public bool Equals(Position other)
        {
            const float eps = 1e-6f;
            return all(abs(Value - other.Value) <= new float3(eps));
        }

        public override bool Equals(object obj) => obj is Position p && Equals(p);
        public override int GetHashCode() => (int)hash(Value);
    }

    [ZenFormatterFor(typeof(Position), "com.zenecs.position.v1")]
    public sealed class PositionFormatter : BinaryComponentFormatter<Position>
    {
        public override void Write(in Position value, ISnapshotBackend backend)
        {
            backend.WriteFloat(value.Value.x);
            backend.WriteFloat(value.Value.y);
            backend.WriteFloat(value.Value.z);
        }

        public override Position ReadTyped(ISnapshotBackend backend)
        {
            float x = backend.ReadFloat();
            float y = backend.ReadFloat();
            float z = backend.ReadFloat();
            return new Position(new float3(x, y, z));
        }
    }
    
    [ZenFormatterFor(typeof(Position), "com.zenecs.position.v2", isLatest: true)]
    public sealed class PositionFormatterV2 : BinaryComponentFormatter<Position>
    {
        public override void Write(in Position value, ISnapshotBackend backend)
        {
            backend.WriteFloat(value.Value.x);
            backend.WriteFloat(value.Value.y);
            backend.WriteFloat(value.Value.z);
            backend.WriteInt(value.IntValue);
        }

        public override Position ReadTyped(ISnapshotBackend backend)
        {
            float x = backend.ReadFloat();
            float y = backend.ReadFloat();
            float z = backend.ReadFloat();
            int intValue = backend.ReadInt();
            return new Position(new float3(x, y, z), intValue);
        }
    }
}