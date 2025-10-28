namespace ZenECS.Core.Binding
{
    public interface IBindingRouter
    {
        void Attach(Entity e, IBinder b);
        void Detach(Entity e, IBinder b);
        
        void DispatchAdded<T>(Entity e, in T value) where T : struct;
        void DispatchChanged<T>(Entity e, in T value) where T : struct;
        void DispatchRemoved<T>(Entity e) where T : struct;
        void DispatchEntityDestroyed(Entity e);
        void RunApply();
    }
}