namespace ZenECS.Core.Binding
{
    public interface IViewBinder
    {
        Entity Entity { get; }
        int HandleId { get; }
        void SetEntity(Entity e);
    }
}