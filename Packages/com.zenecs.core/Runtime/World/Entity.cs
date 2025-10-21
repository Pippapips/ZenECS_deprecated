#nullable enable
using System;

namespace ZenECS.Core
{
    /// <summary>
    /// 64-bit packed handle
    /// [  Gen:32  |  Id:32  ]
    /// 값 타입 엔티티 핸들. 모든 데이터 접근은 World 경유.
    /// </summary>
    public readonly struct Entity : IEquatable<Entity>
    {
        public readonly ulong Handle;
        public const int GenShift = 32;
        public const ulong IdMask = 0x00000000FFFFFFFFUL;
        public const ulong GenMask = 0xFFFFFFFF00000000UL;
        public int Id => (int)(Handle & IdMask);
        public int Gen => (int)(Handle >> GenShift);
        public Entity(int id, int gen)
        {
            Handle = Pack(id, gen);
        }

        public static ulong Pack(int id, int gen)
        {
            // 하위 32비트 = Id, 상위 32비트 = Gen
            return ((ulong)(uint)gen << GenShift) | (uint)id;
        }
        public static (int id, int gen) Unpack(ulong handle)
        {
            return ((int)(handle & IdMask), (int)(handle >> GenShift));
        }
        public bool Equals(Entity other) => Handle == other.Handle;
        public override bool Equals(object? obj) => obj is Entity e && e.Handle == Handle;
        public override int GetHashCode() => Handle.GetHashCode();
        public override string ToString() => $"Entity({Id}:{Gen})";
        public static explicit operator int(Entity e) => e.Id;       // 편의: Id만 필요할 때
        public static explicit operator ulong(Entity e) => e.Handle; // 직렬화/키로 활용
    }
}
