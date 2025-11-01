using System;
using Unity.Mathematics;
using ZenECS.Adapter.Unity.Attributes;
using static Unity.Mathematics.math;
using ZenECS.Core;
using ZenECS.Core.Serialization;
using ZenECS.Core.Serialization.Formats.Binary;
using quaternion = Unity.Mathematics.quaternion;

namespace ZenECS.Adapter.Unity.Components.Common
{
    [ZenComponent(StableId = "com.zenecs.rotation.v1")]
    public readonly struct Rotation : IEquatable<Rotation>
    {
        public static readonly Rotation Default = new Rotation(quaternion.identity);
        public readonly quaternion Value;

        public Rotation(in quaternion value) => Value = value;

        // 부호무시 비교( q ≡ -q )
        public bool Equals(Rotation other)
        {
            const float eps = 1e-6f;
            // 안전: 둘 다 정규화한 뒤 부호 무시 dot 비교
            var a = normalize(Value);
            var b = normalize(other.Value);
            var d = abs(dot(a.value, b.value)); // a·b ∈ [-1,1], 부호 무시
            return d >= 1f - eps;
        }

        public override bool Equals(object obj) => obj is Rotation r && Equals(r);

        public override int GetHashCode()
        {
            // 부호 정규화( w<0이면 뒤집어 q≡-q 해시 동일 )
            var q = Value;
            if (q.value.w < 0f) q = new quaternion(-q.value.x, -q.value.y, -q.value.z, -q.value.w);
            return (int)hash(q.value);
        }
    }
    
    [ZenFormatterFor(typeof(Rotation), "com.zenecs.rotation.v1")]
    public sealed class RotationFormatterV1 : BinaryComponentFormatter<Rotation>
    {
        public override void Write(in Rotation value, ISnapshotBackend backend)
        {
            backend.WriteFloat(value.Value.value.x);
            backend.WriteFloat(value.Value.value.y);
            backend.WriteFloat(value.Value.value.z);
            backend.WriteFloat(value.Value.value.w);
        }

        public override Rotation ReadTyped(ISnapshotBackend backend)
        {
            float x = backend.ReadFloat();
            float y = backend.ReadFloat();
            float z = backend.ReadFloat();
            float w = backend.ReadFloat();
            return new Rotation(new quaternion(x, y, z, w));
        }
    }
}