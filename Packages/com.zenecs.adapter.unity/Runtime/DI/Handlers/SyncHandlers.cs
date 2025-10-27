using ZenECS.Adapter.Unity.Components.Common;
#if ZENECS_ZENJECT
using Zenject;
#endif

namespace ZenECS.Adapter.DI.Handlers
{
#if ZENECS_ZENJECT
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
#endif
}