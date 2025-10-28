namespace ZenECS.Core.Binding
{
   public enum ComponentDeltaKind { Added, Changed, Removed }

   public readonly struct ComponentDelta<T> where T:struct
   {
      public Entity Entity { get; }
      public ComponentDeltaKind Kind { get; }
      public T Value { get; }
      public ComponentDelta(Entity e, ComponentDeltaKind k, in T v=default){ Entity=e; Kind=k; Value=v; }
   }
   
   public interface IBinds<T> where T:struct { void OnDelta(in ComponentDelta<T> delta); }
}