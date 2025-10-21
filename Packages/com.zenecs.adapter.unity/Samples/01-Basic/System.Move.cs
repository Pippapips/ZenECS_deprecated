// using Unity.Mathematics;
// using ZenECS.Adapter.Unity.Attributes;
// using ZenECS.Adapter.Unity.Components.Common;
// using ZenECS.Core;
// using ZenECS.Core.Extensions;
// using ZenECS.Core.Systems;
//
// namespace ZenECS.Samples.Basic
// {
//     /// <summary>pos += vel * dt</summary>
//     [Watch(typeof(Position), typeof(Velocity))]
//     [UpdateGroup(typeof(SimulationGroup))]
//     public sealed class MoveSystem : IRunSystem
//     {
//         public void Run(World w)
//         {
//             foreach (var e in w.Query<Position, Velocity>())
//             {
//                 var p = w.Read<Position>(e);
//                 var v = w.Read<Velocity>(e);
//                 var np = new Position(p.Value + new float3(v.Value.x, 0, v.Value.y) * w.DeltaTime);
//                 w.Replace(e, np);
//             }
//         }
//     }
// }