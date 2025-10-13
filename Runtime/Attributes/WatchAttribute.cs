using System;

namespace ZenECS.Adapter.Unity.Attributes
{
    /// <summary>
    /// 간단 관제 쿼리: AllOf(모두 포함) 조합 기준으로 엔티티를 수집.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class WatchAttribute : Attribute
    {
        public readonly Type[] AllOf;
        public WatchAttribute(params Type[] allOf) { AllOf = allOf; }
    }
}