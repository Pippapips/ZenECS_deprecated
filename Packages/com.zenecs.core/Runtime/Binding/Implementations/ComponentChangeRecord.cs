using System;

namespace ZenECS.Core.Binding
{
    [Flags]
    public enum ComponentChangeMask
    {
        Added = 1,
        Changed = 2,
        Removed = 4
    }

    public readonly struct ComponentChangeRecord
    {
        public readonly Entity Entity;
        public readonly Type ComponentType;
        public readonly ComponentChangeMask Mask;

        public ComponentChangeRecord(Entity e, Type t, ComponentChangeMask m)
        {
            Entity = e;
            ComponentType = t;
            Mask = m;
        }
    }
}