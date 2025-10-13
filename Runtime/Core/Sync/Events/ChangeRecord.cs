using System;

namespace ZenECS.Core.Sync.Events
{
    [Flags]
    public enum ChangeMask
    {
        Added = 1,
        Changed = 2,
        Removed = 4
    }

    public readonly struct ChangeRecord
    {
        public readonly Entity Entity;
        public readonly Type ComponentType;
        public readonly ChangeMask Mask;

        public ChangeRecord(Entity e, Type t, ChangeMask m)
        {
            Entity = e;
            ComponentType = t;
            Mask = m;
        }
    }
}