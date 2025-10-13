namespace ZenECS.Core.Sync
{
    public interface ISyncTarget
    {
        int HandleId { get; }
        void SetEntity(Entity e);
    }
}