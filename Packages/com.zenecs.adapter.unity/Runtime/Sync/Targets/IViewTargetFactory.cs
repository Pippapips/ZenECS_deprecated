using UnityEngine;
using ZenECS.Core;
using ZenECS.Core.Sync;

namespace ZenECS.Adapter.Unity.Sync.Targets
{
    public interface IViewTargetFactory
    {
        ISyncTarget Create(GameObject prefab);
    }
}