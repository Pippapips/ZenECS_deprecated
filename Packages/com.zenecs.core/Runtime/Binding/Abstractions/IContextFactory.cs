namespace ZenECS.Core.Binding
{
    public interface IContextFactory<out T> where T:class, IContext
    {
        T Create(World w, Entity e);
    }
}
