using System;

namespace ZenECS.Core.Systems
{
    /// <summary>시스템이 소속될 실행 그룹을 지정</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class UpdateGroupAttribute : Attribute
    {
        public readonly Type GroupType;
        public UpdateGroupAttribute(Type groupType) { GroupType = groupType; }
    }
}