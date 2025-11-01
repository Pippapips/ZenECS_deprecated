#nullable enable

namespace ZenECS.Core.Binding
{
    /// <summary>
    /// Marker for resource containers (data/references) that Binders will use to apply view/state to external systems.
    /// </summary>
    public interface IContext { }

    /// <summary>
    /// Optional lifecycle for contexts. The registry calls these at registration/removal time.
    /// </summary>
    public interface IContextInitialize
    {
        /// <remarks>Called once when the context is first registered for the entity. Lookup can be used to access other contexts.</remarks>
        void Initialize(World world, Entity entity, IContextLookup lookup);


        /// <remarks>Called when the context is removed or the entity/world is being destroyed.</remarks>
        void Deinitialize(World world, Entity entity);
    }

    /// <summary>
    /// Optional fast re-init path. If not implemented, the registry will fallback to Deinitialize→Initialize.
    /// </summary>
    public interface IContextReinitialize : IContextInitialize
    {
        void Reinitialize(World world, Entity entity, IContextLookup lookup);
    }
}