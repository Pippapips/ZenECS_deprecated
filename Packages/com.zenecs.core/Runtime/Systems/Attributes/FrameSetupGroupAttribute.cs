using System;

namespace ZenECS.Core.Systems
{
    /// <summary>프레임 준비(입력 스냅샷, 시간/큐 스왑 등)</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class FrameSetupGroupAttribute : Attribute { }
}
