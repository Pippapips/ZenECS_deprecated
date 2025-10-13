using ZenECS.Adapter.Unity.Components.Common;
using ZenECS.Adapter.Unity.Sync.Handlers;
using ZenECS.Core.Sync;
using Zenject;

namespace ZenECS.Adapter.DI.Handlers
{
    internal sealed class SyncHandlerBootstrap : IInitializable
    {
        private ISyncHandlerRegistry _handlerRegistry;

        public SyncHandlerBootstrap(ISyncHandlerRegistry handlerRegistry)
        {
            _handlerRegistry = handlerRegistry;
        }

        public void Initialize()
        {
            _handlerRegistry.RegisterSingleton<Position>(new PositionHandler());
        }
    }
}