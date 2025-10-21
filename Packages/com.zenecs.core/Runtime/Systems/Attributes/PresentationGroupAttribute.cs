using System;

namespace ZenECS.Core.Systems
{
    /// <summary>표시/렌더/UI/바인딩 (읽기 전용)</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class PresentationGroupAttribute : Attribute { }
}