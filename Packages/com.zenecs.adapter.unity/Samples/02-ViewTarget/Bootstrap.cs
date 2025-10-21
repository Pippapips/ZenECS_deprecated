// using UnityEngine;
// using ZenECS.Core;
// using ZenECS.Core.Messaging;
// using ZenECS.Core.Systems;
//
// namespace ZenECS.Samples.ViewTarget
// {
//     public class SampleBootstrap : MonoBehaviour
//     {
//         [SerializeField] private Adapter.Unity.Binding.UnityViewBinder _unityViewBinder;
//         private SystemRunner _runner;
//         
//         private void Start()
//         {
//             var world = new World();
//             _runner = new SystemRunner(world);
//             
//             _runner.Init();
//             
//             var e = world.CreateEntity();
//             _unityViewBinder.SetEntity(e);
//         }
//
//         private void Update()
//         {
//             _runner?.Update(Time.deltaTime);
//         }
//
//         private void FixedUpdate()
//         {
//             _runner?.FixedUpdate(Time.fixedDeltaTime);
//         }
//
//         private void LateUpdate()
//         {
//             _runner?.LateUpdate();
//         }
//
//         private void OnDestroy()
//         {
//             _runner?.Dispose();
//         }
//     }
// }