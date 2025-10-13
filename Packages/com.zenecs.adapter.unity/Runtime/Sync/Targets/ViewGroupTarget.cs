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
    public interface IViewGroupTarget : ISyncTarget
    {
        GameObject Go { get; }
        System.Collections.Generic.IReadOnlyList<IViewTarget> Items { get; }
    }

    public sealed class ViewGroupTarget : MonoBehaviour, IViewGroupTarget
    {
        public GameObject Go => gameObject;
        [SerializeField] int _entityId;
        [SerializeField] List<MonoBehaviour> _targets = new(); // ISceneObjectTarget 구현들을 드래그&드롭
        public int HandleId => GetInstanceID();

        // 직렬화 리스트를 ISceneObjectTarget로 투영
        private List<IViewTarget>? _items;
        private ISyncTargetRegistry? _syncTargetRegistry;

        public IReadOnlyList<IViewTarget> Items
            => _items ??= _targets.ConvertAll(t => (IViewTarget)t);

#if ZENECS_ZENJECT
        [Inject]
        void ctor([InjectOptional]ISyncTargetRegistry syncTargetRegistry)
        {
            _syncTargetRegistry = syncTargetRegistry;
        }
#endif

        public void SetEntity(Entity e)
        {
            if (_entityId != 0) _syncTargetRegistry?.Unregister(new Entity(_entityId), this);
            _entityId = e.Id;
            _syncTargetRegistry?.Register(e, this, replaceIfExists: true); // 1:1 등록 (루트=그룹만 등록)
        }

        void Awake()
        {
            if (_entityId != 0) _syncTargetRegistry?.Register(new Entity(_entityId), this, true);
        }

        void OnDestroy()
        {
            if (_entityId != 0) _syncTargetRegistry?.Unregister(new Entity(_entityId), this);
        }
    }
}