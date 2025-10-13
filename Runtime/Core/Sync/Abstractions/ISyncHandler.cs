using System;

namespace ZenECS.Core.Sync
{
    public interface ISyncHandler
    {
        Type ComponentType { get; }
        void Bind(World w, Entity e, ISyncTarget t);
        void Apply(World w, Entity e, object value, ISyncTarget t);
        void Unbind(World w, Entity e, ISyncTarget t);
    }
}