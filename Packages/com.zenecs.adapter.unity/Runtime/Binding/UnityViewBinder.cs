#nullable enable
using System.Collections.Generic;
using UnityEngine;
using ZenECS.Core;
using ZenECS.Core.Binding;
using ZenECS.Core.Sync;
#if ZENECS_ZENJECT
using Zenject;
#endif

namespace ZenECS.Adapter.Unity.Binding
{
    public interface IUnityViewBinder : IViewBinder
    {
        GameObject Go { get; }
    }
    
    public sealed class UnityViewBinder : MonoBehaviour, IUnityViewBinder
    {
        private IViewBinderRegistry? _syncTargetRegistry;

        public GameObject Go => gameObject;
        public Entity Entity { get; private set; }

        public int HandleId => GetInstanceID();

        #if ZENECS_ZENJECT
        [Inject]
        void ctor([InjectOptional]ISyncTargetRegistry syncTargetRegistry)
        {
            _syncTargetRegistry = syncTargetRegistry;
        }
        #endif
        
        public void SetEntity(Entity e)
        {
            // 기존 등록 분리
            Entity = e;
            _syncTargetRegistry?.Register(e, this, replaceIfExists: true);
        }

        void OnDestroy()
        {
            _syncTargetRegistry?.Unregister(Entity, this);
        }
    }
}