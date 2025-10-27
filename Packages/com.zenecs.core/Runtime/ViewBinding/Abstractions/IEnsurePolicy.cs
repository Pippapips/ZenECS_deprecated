namespace ZenECS.Core.ViewBinding
{
    public interface IEnsurePolicy
    {
        void EnsureFor(World w, Entity e, IViewBinder binder);
    }
}