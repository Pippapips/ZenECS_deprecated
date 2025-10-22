namespace ZenECS.Core.Binding
{
    public interface IProjection
    {
        void Reconcile(World w, Entity e, IViewBinder v, IComponentBinderResolver resolver);
    }
}
