using System;

namespace ZenECS.Core.Systems
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class OrderBeforeAttribute : Attribute
    {
        public Type Target { get; }
        public OrderBeforeAttribute(Type target) => Target = target;
    }
}