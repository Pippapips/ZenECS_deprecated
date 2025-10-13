using System;
using System.Diagnostics;

namespace ZenECS.Core.Messaging
{
    [AttributeUsage(AttributeTargets.Struct, Inherited = false)]
    [Conditional("UNITY_EDITOR")]
    public sealed class EventAttribute : Attribute { }
}