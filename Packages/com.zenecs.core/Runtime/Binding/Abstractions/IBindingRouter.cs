namespace ZenECS.Core.Binding
{
    public enum AttachOptions
    {
        Default = 0,
        Strict  = 1, // throw if required context is missing
        WarnOnly = 2 // log warning and skip attach
    }
    
    public interface IBindingRouter
    {
        void Attach(Entity e, IBinder binder, AttachOptions options = AttachOptions.Default);
        void Detach(Entity e, IBinder binder);
        void DetachAll(Entity e);
        void OnEntityDestroyed(Entity e);
        void ApplyAll();
        void Dispatch<T>(in ComponentDelta<T> d) where T : struct;
    }
}