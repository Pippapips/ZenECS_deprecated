using UnityEngine;
using ZenECS.Adapter.Unity.Sync.Targets;
using ZenECS.Core.Sync;
using Zenject;

namespace ZenECS.Adapter.DI.Factories
{
    internal class ViewTargetPlaceDIFactory : PlaceholderFactory<GameObject, ISyncTarget>
    {
    }
    
    internal class ViewTargetFactory : IViewTargetFactory
    {
        private ViewTargetPlaceDIFactory _factory;

        public ViewTargetFactory(ViewTargetPlaceDIFactory factory)
        {
            _factory = factory;
        }

        public ISyncTarget Create(GameObject prefab)
        {
            var hasSyncTarget = prefab.GetComponent<ISyncTarget>();
            if (hasSyncTarget == null) return null;
            var view = _factory.Create(prefab);
            return view;
        }
    }
}