namespace ZenECS.Core.ViewBinding
{
    public sealed class NoOpEnsurePolicy : IEnsurePolicy
    {
        public void EnsureFor(World w, Entity e, IViewBinder b) { }
    }
}