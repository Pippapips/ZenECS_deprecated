namespace ZenECS.Core.Binding
{
    public interface IContextEnsurer
    {
        bool EnsureForBinder(World w, Entity e, IBinder binder);
    }
}
