using System;

namespace ZenECS.Core.Systems
{
    /// <summary>지정한 시스템보다 먼저 실행</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class OrderBeforeAttribute : Attribute
    {
        public readonly Type Target;
        public OrderBeforeAttribute(Type target) { Target = target; }
    }
}