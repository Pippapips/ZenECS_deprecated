// using UnityEngine;
// using ZenECS.Adapter.Unity.Attributes;
// using ZenECS.Core;
// using ZenECS.Core.Extensions;
// using ZenECS.Core.Systems;
//
// namespace ZenECS.Samples.Basic
// {
//     /// <summary>vel.y += g * dt / m</summary>
//     [Watch(typeof(Mass), typeof(Velocity))]
//     [UpdateGroup(typeof(SimulationGroup))]
//     public sealed class GravitySystem : IRunSystem
//     {
//         private readonly float _g;
//
//         public GravitySystem(float g = -9.81f)
//         {
//             _g = g;
//         }
//
//         public void Run(World w)
//         {
//             foreach (var e in w.Query<Velocity, Mass>())
//             {
//                 var v = w.Read<Velocity>(e);
//                 var m = w.Read<Mass>(e);
//                 var nv = new Velocity(new Vector2(v.Value.x, v.Value.y + (_g * w.DeltaTime) / (m.Value <= 0 ? 1f : m.Value)));
//                 w.Replace(e, nv);
//             }
//         }
//     }
// }