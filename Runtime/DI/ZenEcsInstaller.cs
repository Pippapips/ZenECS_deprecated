#if ZENECS_ZENJECT
using System;
using UnityEngine;
using ZenECS.Adapter.DI.Factories;
using ZenECS.Adapter.DI.Handlers;
using ZenECS.Adapter.Unity.Sync.Handlers;
using Zenject;
using ZenECS.Core;
using ZenECS.Core.Systems;
using ZenECS.Core.Messaging;
using ZenECS.Core.Sync.Feed;
#if ZENECS_UNIRX
using ZenECS.Adapter.Unity.Rx;
#endif
using ZenECS.Adapter.Unity.Components.Common;
using ZenECS.Adapter.Unity.Sync.Targets;
using ZenECS.Core.Messaging.Diagnostics;
using ZenECS.Core.Sync;
using ZenECS.Core.Sync.Systems;
#if ZENECS_TRACE
using ZenECS.Core.Diagnostics;
#endif

namespace ZenECS.Adapter.Unity.DI
{
    public sealed class ZenEcsInstaller : MonoInstaller
    {
        public bool enablePresentation = true;
        public bool autoSkipOnBatchMode = true;
        
        public override void InstallBindings()
        {
            Container.Bind<World>().AsSingle();
#if ZENECS_TRACE
    #if ZENECS_UNIRX
            Container.Bind<IMessageBus>().WithId("Inner").To<UniRxMessageBus>().AsSingle();
    #else
            Container.Bind<IMessageBus>().WithId("Inner").To<SimpleMessageBus>().AsSingle();
    #endif
            Container.Bind<MessageCounters>().AsSingle();
            Container.Bind<IMessageBus>().To<TracingMessageBus>().AsSingle().NonLazy();
            Container.Bind<EcsTraceCenter>().AsSingle().NonLazy();
#else
    #if ZENECS_UNIRX
            Container.Bind<IMessageBus>().To<UniRxMessageBus>().AsSingle();
    #else
            Container.Bind<IMessageBus>().To<SimpleMessageBus>().AsSingle();
    #endif
#endif
            
            Container.Bind<SystemRunner>().AsSingle();
            Container.BindExecutionOrder<SystemRunnerBootstrap>(-10000);
            Container.BindInterfacesAndSelfTo<SystemRunnerBootstrap>().AsSingle().NonLazy();
            Container.Bind<IChangeFeed>().To<EndOfFrameChangeFeed>().AsSingle();
            Container.Bind<ISyncTargetRegistry>().To<SyncTargetRegistry>().AsSingle();
            Container.Bind<ISyncHandlerRegistry>().To<SyncHandlerRegistry>().AsSingle();
            Container.BindInitializableExecutionOrder<SyncHandlerBootstrap>(-9999);
            Container.BindInterfacesAndSelfTo<SyncHandlerBootstrap>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<ChangeCaptureSystem>().AsSingle();
            Container.BindInterfacesAndSelfTo<ChangeBatchDispatchSystem>().AsSingle();
            Container.BindInterfacesAndSelfTo<SyncHostSystem>().AsSingle();
            
            if (enablePresentation && !(autoSkipOnBatchMode && UnityEngine.Application.isBatchMode))
            {
                Container.BindFactory<GameObject, ISyncTarget, ViewTargetPlaceDIFactory>().FromFactory<PrefabFactory<ViewTarget>>();
                Container.Bind<IViewTargetFactory>().To<ViewTargetFactory>().AsSingle();
            }
        }
    }
    
    internal sealed class SystemRunnerBootstrap : IInitializable, ITickable, IFixedTickable, ILateTickable, IDisposable
    {
        private readonly SystemRunner _runner;

        public SystemRunnerBootstrap(SystemRunner runner)
        {
            _runner = runner;
        }
        public void Initialize() => _runner.Init(); // 이 시점에 IEnumerable<ISystem> Resolve 완료
        public void Tick()       => _runner.Update(UnityEngine.Time.deltaTime);
        public void FixedTick()  => _runner.FixedUpdate(UnityEngine.Time.fixedDeltaTime);
        public void LateTick()   => _runner.LateUpdate();
        public void Dispose()    => _runner.Dispose();
    }
}
#endif