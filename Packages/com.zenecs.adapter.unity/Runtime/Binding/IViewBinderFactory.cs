using UnityEngine;
using ZenECS.Core;
using ZenECS.Core.Sync;
using ZenECS.Core.Binding;

namespace ZenECS.Adapter.Unity.Binding
{
    public interface IViewBinderFactory
    {
        IViewBinder Create(GameObject prefab);
    }
}