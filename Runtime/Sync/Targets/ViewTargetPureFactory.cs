using UnityEngine;
using ZenECS.Core.Sync;

namespace ZenECS.Adapter.Unity.Sync.Targets
{
    public class ViewTargetPureFactory : IViewTargetFactory
    {
        public ISyncTarget Create(GameObject prefab)
        {
            if (prefab == null) return null;
            var hasComponent = prefab.GetComponent<ISyncTarget>();
            if (hasComponent == null) return null;
            var o = Object.Instantiate(prefab);
            return o.GetComponent<ISyncTarget>();
        }
    }
}