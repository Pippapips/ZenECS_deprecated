using System;
using System.Linq;
using ZenECS.Core;
using ZenECS.Core.Messaging;
using ZenECS.Core.Systems;

namespace ZenECS.Core.Testing
{
    /// <summary>
    /// Thin, in‑memory host for tests. Creates a World + Bus and exposes a minimal runner facade.
    /// </summary>
    public sealed class TestWorldHost : IDisposable
    {
        public World World { get; }
        public IMessageBus Bus { get; }

        private ISystem[]? _orderedSystems = Array.Empty<ISystem>();

        public TestWorldHost(World? world = null, IMessageBus? bus = null)
        {
            World = world ?? new World();
            Bus = bus ?? new ZenECS.Core.Messaging.MessageBus();
        }

        /// <summary>Registers systems (in any order). Order is resolved by <see cref="SystemPlanner"/>.</summary>
        public void RegisterSystems(params ISystem[] systems)
        {
            var plan = SystemPlanner.Build(systems); // deterministic order
            _orderedSystems = plan?.AllInExecutionOrder.ToArray();
            if (_orderedSystems == null || _orderedSystems.Length == 0) return;

            foreach (var s in _orderedSystems)
                (s as ISystemLifecycle)?.Initialize(World);
        }

        /// <summary>Simulate one frame: FrameSetup → Fixed(n) → Update → LateFrame.</summary>
        public void TickFrame(int fixedSteps = 0)
        {
            if (_orderedSystems == null || _orderedSystems.Length == 0) return;

            foreach (var s in _orderedSystems)
                (s as IFrameSetupSystem)?.Run(World);

            for (int i = 0; i < fixedSteps; i++)
                foreach (var s in _orderedSystems)
                    (s as IFixedRunSystem)?.Run(World);

            foreach (var s in _orderedSystems)
                (s as IVariableRunSystem)?.Run(World);

            foreach (var s in _orderedSystems)
                (s as IPresentationSystem)?.Run(World);

            // Deliver queued bus messages once per frame
            Bus.PumpAll();
        }

        public void Dispose()
        {
            if (_orderedSystems == null || _orderedSystems.Length == 0) return;

            foreach (var s in _orderedSystems)
                (s as ISystemLifecycle)?.Shutdown(World);
        }
    }
}