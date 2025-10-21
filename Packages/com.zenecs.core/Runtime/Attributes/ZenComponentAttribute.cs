#nullable enable
using System;
using System.Diagnostics; // Conditional

namespace ZenECS.Core
{
    /// <summary>Editor/툴링 수집용. 런타임 메타데이터는 제외됩니다.</summary>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    [Conditional("UNITY_EDITOR")]
    public sealed class ZenComponentAttribute : Attribute
    {
        public string? StableId { get; set; } // 세이브/네트워킹용(옵션)
    }
}