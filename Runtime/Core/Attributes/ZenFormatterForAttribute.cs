using System;
using System.Diagnostics; // Conditional

namespace ZenECS.Core
{
    /// <summary>Editor/툴링 수집용. 런타임 메타데이터는 제외됩니다.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
    [Conditional("UNITY_EDITOR")]
    public sealed class ZenFormatterForAttribute : Attribute
    {
        /// <summary>이 포매터가 처리하는 컴포넌트 타입</summary>
        public Type ComponentType { get; }

        /// <summary>이 포매터가 읽고/쓰는 포맷의 StableId (예: com.game.position.v2)</summary>
        public string StableId { get; }

        /// <summary>Save 시 기본으로 사용(최신 포맷) 여부</summary>
        public bool IsLatest { get; }

        public ZenFormatterForAttribute(Type componentType, string stableId, bool isLatest = false)
        {
            ComponentType = componentType ?? throw new ArgumentNullException(nameof(componentType));
            StableId = !string.IsNullOrWhiteSpace(stableId)
                ? stableId
                : throw new ArgumentException("StableId must be non-empty.", nameof(stableId));
            IsLatest = isLatest;
        }
    }
}