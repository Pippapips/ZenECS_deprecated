// Console demo for ZenECS Core Binding pipeline (Unity-free)
// Requires ZenECS.Core assemblies at build time.

using System;
using ZenECS.Core;
using ZenECS.Core.Binding;
using ZenECS.Core.Binding.Systems;
using ZenECS.Core.Binding.Util;
using ZenECS.Core.Extensions;
using ZenECS.Core.Sync;
using ZenECS.Core.Systems;

namespace ZenECS.Binding.ConsoleSample
{
    public readonly struct Position : IEquatable<Position>
    {
        public readonly float X, Y;
        public Position(float x, float y)
        {
            X = x;
            Y = y;
        }
        public bool Equals(Position other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public override string ToString() => $"({X:0.##}, {Y:0.##})";
    }

    public sealed class PositionBinder : IComponentBinder<Position>, IComponentBinder
    {
        public Type ComponentType => typeof(Position);
        public void Bind(World w, Entity e, IViewBinder v) => Console.WriteLine($"[Bind]   e={e} Position");
        public void Apply(World w, Entity e, in Position value, IViewBinder v) => Console.WriteLine($"[Apply]  e={e} Position={value}");
        public void Unbind(World w, Entity e, IViewBinder v) => Console.WriteLine($"[Unbind] e={e} Position");

        void IComponentBinder.Bind(World w, Entity e, IViewBinder t) => Bind(w, e, t);
        void IComponentBinder.Apply(World w, Entity e, object value, IViewBinder t) => Apply(w, e, (Position)value, t);
        void IComponentBinder.Unbind(World w, Entity e, IViewBinder t) => Unbind(w, e, t);
    }

    public sealed class ConsoleViewBinder : IViewBinder
    {
        public string Name { get; }
        public ConsoleViewBinder(string name) => Name = name;
        public override string ToString() => Name;

        // --
        public Entity Entity { get; }
        public int HandleId { get; }
        public void SetEntity(Entity e)
        {
            throw new NotImplementedException();
        }
    }

    internal static class Program
    {
        static void Main()
        {
            IMainThreadGate gate = new DefaultMainThreadGate();
            var changeFeed = new ComponentChangeFeed(gate);
            var binderRegistry = new ComponentBinderRegistry();
            var viewRegistry = new ViewBinderRegistry();

            binderRegistry.RegisterSingleton<Position>(new PositionBinder());

            var world = new World();

            var hub = new ComponentBindingHubSystem(changeFeed);
            var binding = new ViewBindingSystem(viewRegistry, binderRegistry, binderRegistry);
            var dispatch = new ComponentBatchDispatchSystem(changeFeed, binding);

            void RunFrame(Action body)
            {
                body();

                (hub as IPresentationSystem).Run(world);
                (dispatch as IPresentationSystem).Run(world);
                (binding as IPresentationSystem).Run(world);
            }

            var e = world.CreateEntity();
            var view = new ConsoleViewBinder("Player-View");
            viewRegistry.Register(e, view);

            (hub as ISystemLifecycle).Initialize(world);
            (dispatch as ISystemLifecycle).Initialize(world);
            (binding as ISystemLifecycle).Initialize(world);

            RunFrame(() => { world.Add(e, new Position(1, 1)); });
            RunFrame(() => { world.Replace(e, new Position(2.5f, 4)); });
            RunFrame(() => { binding.RequestReconcile(world, e); });
            RunFrame(() => { world.Remove<Position>(e); });

            (binding as ISystemLifecycle).Shutdown(world);
            (dispatch as ISystemLifecycle).Shutdown(world);
            (hub as ISystemLifecycle).Shutdown(world);

            Console.WriteLine("\nDone. Press Enter to exit.");
            Console.ReadLine();
        }
    }
}