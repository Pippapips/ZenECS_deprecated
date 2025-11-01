using System;
using Unity.Mathematics;
using ZenECS.Adapter.Unity.Attributes;
using ZenECS.Core;
using ZenECS.Core.Serialization;
using ZenECS.Core.Serialization.Formats.Binary;

namespace ZenECS.Adapter.Unity.Components.Common
{
    [ZenComponent(StableId = "com.zenecs.scale.v1")]
    public readonly struct Scale : IEquatable<Scale>
    {
        public static readonly Scale Default = new Scale(1);
        public readonly float3 Value;

        public Scale(in float3 value) => Value = value;

        public Scale(float uniform) : this(new float3(uniform))
        {
        }

        public static Scale Identity => new Scale(1f);

        public bool Equals(Scale other)
        {
            const float eps = 1e-6f;
            return math.all(math.abs(Value - other.Value) <= new float3(eps));
        }

        public override bool Equals(object obj) => obj is Scale other && Equals(other);
        public override int GetHashCode() => (int)math.hash(Value);
    }
    
    [ZenFormatterFor(typeof(Scale), "com.zenecs.scale.v1")]
    public sealed class ScaleFormatterV1 : BinaryComponentFormatter<Scale>
    {
        public override void Write(in Scale value, ISnapshotBackend backend)
        {
            backend.WriteFloat(value.Value.x);
            backend.WriteFloat(value.Value.y);
            backend.WriteFloat(value.Value.z);
        }

        public override Scale ReadTyped(ISnapshotBackend backend)
        {
            float x = backend.ReadFloat();
            float y = backend.ReadFloat();
            float z = backend.ReadFloat();
            return new Scale(new float3(x, y, z));
        }
    }
}