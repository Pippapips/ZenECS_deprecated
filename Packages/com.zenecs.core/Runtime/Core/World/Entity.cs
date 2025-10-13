using System;

namespace ZenECS.Core
{
    /// <summary>값 타입 엔티티 핸들. 모든 데이터 접근은 World 경유.</summary>
    public readonly struct Entity : IEquatable<Entity>
    {
        public readonly int Id;
        public Entity(int id) => Id = id;
        public bool Equals(Entity other) => Id == other.Id;
        public override bool Equals(object obj) => obj is Entity e && e.Id == Id;
        public override int GetHashCode() => Id;
        public override string ToString() => $"Entity({Id})";
        public static implicit operator int(Entity e) => e.Id;
    }
}