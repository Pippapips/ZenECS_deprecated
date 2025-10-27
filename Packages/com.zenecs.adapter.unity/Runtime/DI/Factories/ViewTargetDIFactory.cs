// using UnityEngine;
// using ZenECS.Adapter.Unity.Binding;
// using ZenECS.Core.Binding;
// #if ZENECS_ZENJECT
// using Zenject;
// #endif
//
// namespace ZenECS.Adapter.DI.Factories
// {
// #if ZENECS_ZENJECT
//     internal class ViewTargetPlaceDIFactory : PlaceholderFactory<GameObject, ISyncTarget>
//     {
//     }
// #endif
//     
//     internal class ViewTargetFactory : IViewBinderFactory
//     {
// #if ZENECS_ZENJECT
//         private ViewTargetPlaceDIFactory _factory;
//
//         public ViewTargetFactory(ViewTargetPlaceDIFactory factory)
//         {
//             _factory = factory;
//         }
// #endif
//
//         public IViewBinder Create(GameObject prefab)
//         {
//             var hasSyncTarget = prefab.GetComponent<IViewBinder>();
//             if (hasSyncTarget == null) return null;
// #if ZENECS_ZENJECT
//             var view = _factory.Create(prefab);
// #else
//             var go = Object.Instantiate(prefab);
//             var view = go.GetComponent<IViewBinder>();
// #endif
//             return view;
//         }
//     }
// }