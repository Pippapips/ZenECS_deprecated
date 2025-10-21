namespace ZenECS.Core.Systems
{
    public interface ISystemLifecycle : ISystem
    {
        void Initialize(World w);
        void Shutdown(World w);
    }
}
