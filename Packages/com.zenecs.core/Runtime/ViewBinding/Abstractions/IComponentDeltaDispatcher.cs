namespace ZenECS.Core.ViewBinding
{
    // before: IComponentChangeSink
    public interface IComponentDeltaDispatcher
    {
        void Attach(Entity e, IViewBinder b);
        void Detach(Entity e, IViewBinder b);
        
        void DispatchAdded<T>(Entity e, in T value) where T : struct;
        void DispatchChanged<T>(Entity e, in T value) where T : struct;
        void DispatchRemoved<T>(Entity e) where T : struct;
        void DispatchEntityDestroyed(Entity e);
        void RunApply();
    }
}