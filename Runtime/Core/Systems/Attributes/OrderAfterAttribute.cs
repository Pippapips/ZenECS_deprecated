using System;

namespace ZenECS.Core.Systems
{
    /// <summary>지정한 시스템보다 나중에 실행</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class OrderAfterAttribute : Attribute
    {
        public readonly Type Target;
        public OrderAfterAttribute(Type target) { Target = target; }
    }
}