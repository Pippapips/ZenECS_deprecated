using System;

namespace ZenECS.Core.Systems
{
    /// <summary>본 게임 로직(물리/AI/상태 갱신)</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class SimulationGroupAttribute : Attribute { }
}