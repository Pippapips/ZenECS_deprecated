using System;

namespace ZenECS.Core.Systems
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class OrderAfterAttribute : Attribute
    {
        public Type Target { get; }
        public OrderAfterAttribute(Type target) => Target = target;
    }
}