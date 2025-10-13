#nullable enable
using System.Collections.Generic;
using UnityEngine;
using ZenECS.Core;
using ZenECS.Core.Sync;
#if ZENECS_ZENJECT
using Zenject;
#endif

namespace ZenECS.Adapter.Unity.Sync.Targets
{
    public interface IViewTarget : ISyncTarget
    {
        GameObject Go { get; }
    }
    
    public sealed class ViewTarget : MonoBehaviour, IViewTarget
    {
        private int _entityId;
        private ISyncTargetRegistry? _syncTargetRegistry;

        public GameObject Go => gameObject;
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
            if (_entityId != 0)
                _syncTargetRegistry?.Unregister(new Entity(_entityId), this);

            _entityId = e.Id;
            _syncTargetRegistry?.Register(e, this, replaceIfExists: true);
        }

        void OnDestroy()
        {
            if (_entityId != 0)
                _syncTargetRegistry?.Unregister(new Entity(_entityId), this);
        }
    }
}