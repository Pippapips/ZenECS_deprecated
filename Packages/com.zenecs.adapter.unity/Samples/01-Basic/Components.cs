using System;
using UnityEngine;
using ZenECS.Core;

namespace ZenECS.Samples.Basic
{
    [ZenComponent]
    public readonly struct Velocity : IEquatable<Velocity>
    {
        public readonly Vector2 Value;
        public Velocity(Vector2 value) => Value = value;
        public bool Equals(Velocity other) => Value.Equals(other.Value);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => $"vel({Value.x:0.00},{Value.y:0.00})";
    }

    [ZenComponent]
    public readonly struct Mass : IEquatable<Mass>
    {
        public readonly float Value;
        public Mass(float value) => Value = value;
        public bool Equals(Mass other) => Value.Equals(other.Value);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => $"m({Value:0.00})";
    }
}