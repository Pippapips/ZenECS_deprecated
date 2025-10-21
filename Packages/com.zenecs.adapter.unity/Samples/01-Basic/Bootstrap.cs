// using Unity.Mathematics;
// using UnityEngine;
// using ZenECS.Adapter.Unity.Components.Common;
// using ZenECS.Core;
// using ZenECS.Core.Extensions;
// using ZenECS.Core.Messaging;
// using ZenECS.Core.Systems;
//
// namespace ZenECS.Samples.Basic
// {
//     public class SampleBootstrap : MonoBehaviour
//     {
//         private SystemRunner _runner;
//         
//         private void Start()
//         {
//             var world = new World();
//             _runner = new SystemRunner(world, new ISystem[]
//             {
//                 new GravitySystem(-9.81f),
//                 new MoveSystem(),
//             });
//             
//             _runner.Init();
//
//             // 엔티티 생성
//             var e = world.CreateEntity();
//             world.Add(e, new Position(new float3(0, 5, 0)));
//             world.Add(e, new Velocity(new Vector2(1.5f, 0)));
//             world.Add(e, new Mass(1f));
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