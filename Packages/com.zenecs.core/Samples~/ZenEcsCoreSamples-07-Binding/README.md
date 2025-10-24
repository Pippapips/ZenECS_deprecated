# ZenECS Core — Sample 01: Basic (Kernel)

A minimal **console** sample demonstrating the ZenECS **Kernel** loop with simulation and presentation systems.

* Minimal components: `Position`, `Velocity`
* Systems:

    * `MoveSystem : IVariableRunSystem` — integrates `Position += Velocity * dt`
    * `PrintPositionsSystem : IPresentationSystem` — read-only presentation
* Demonstrates:

    * `EcsKernel.Start()` for world bootstrapping
    * `Pump()` for variable-step + fixed-step integration
    * `LateFrame()` for presentation phase execution

---

## What this sample shows

1. **World creation and entity setup**
   The ECS world is created, two entities are spawned, and each is assigned `Position` and `Velocity` components.

2. **Simulation and presentation flow**
   `MoveSystem` performs simulation updates (writes), while `PrintPositionsSystem` prints results (read-only).

3. **Frame loop with variable + fixed timestep**
   Uses `EcsKernel.Pump(dt, fixedDelta, maxSubSteps, out alpha)` to handle variable time delta, fixed simulation steps, and interpolation.

---

## TL;DR flow

```
World.Start()
   ├─ SimulationGroup (MoveSystem)
   │     Position += Velocity * dt
   └─ PresentationGroup (PrintPositionsSystem)
         Console output of all positions
```

Simulation runs first; presentation follows during the **Late** phase for read-only safety.

---

## File layout

```
Basic.cs
```

Key excerpts:

### Components

```csharp
public readonly struct Position
{
    public readonly float X, Y;
    public Position(float x, float y) { X = x; Y = y; }
    public override string ToString() => $"({X:0.###}, {Y:0.###})";
}

public readonly struct Velocity
{
    public readonly float X, Y;
    public Velocity(float x, float y) { X = x; Y = y; }
}
```

### Systems

```csharp
[SimulationGroup]
public sealed class MoveSystem : IVariableRunSystem
{
    public void Run(World w)
    {
        var dt = w.DeltaTime;
        foreach (var e in w.Query<Position, Velocity>())
        {
            var p = w.Read<Position>(e);
            var v = w.Read<Velocity>(e);
            w.Replace(e, new Position(p.X + v.X * dt, p.Y + v.Y * dt));
        }
    }
}

[PresentationGroup]
public sealed class PrintPositionsSystem : IPresentationSystem
{
    public void Run(World w, float alpha)
    {
        Console.WriteLine($"-- FrameCount: {w.FrameCount} (alpha={alpha:0.00}) --");
        foreach (var e in w.Query<Position>())
        {
            var p = w.Read<Position>(e); // read-only
            Console.WriteLine($"Entity {e.Id,3}: pos={p}");
        }
    }
}
```

### Main loop

```csharp
const float fixedDelta = 1f / 60f; // 60Hz
const int maxSubSteps = 4;

EcsKernel.Pump(dt, fixedDelta, maxSubSteps, out var alpha);
EcsKernel.LateFrame(alpha);
```

---

## Build & Run

**Prereqs:** .NET 8 SDK, and ZenECS Core assemblies referenced.

```bash
dotnet restore
dotnet build --no-restore
dotnet run --project <your-console-sample-csproj>
```

Press any key to exit the running loop.

---

## Example output

```
=== ZenECS Core Console Sample 01: Basic (Kernel) ===
Running... press any key to exit.
-- FrameCount: 1 (alpha=0.32) --
Entity   1: pos=(0.02, 0.00)
Entity   2: pos=(2.00, 1.00)
-- FrameCount: 2 (alpha=0.53) --
Entity   1: pos=(0.04, 0.00)
Entity   2: pos=(2.00, 0.99)
...
Shutting down...
Done.
```

---

## APIs highlighted

* **Kernel lifecycle**

    * `EcsKernel.Start()`
    * `EcsKernel.Pump(dt, fixedDelta, maxSubSteps, out alpha)`
    * `EcsKernel.LateFrame(alpha)`
    * `EcsKernel.Shutdown()`

* **World operations**

    * `World.CreateEntity()`
    * `World.Add<T>()`, `World.Read<T>()`, `World.Replace<T>()`
    * `World.Query<T>()`

* **System attributes**

    * `[SimulationGroup]` — executed during fixed/variable updates
    * `[PresentationGroup]` — executed during Late phase (read-only)

---

## Notes & best practices

* Simulation systems **write**, presentation systems **read-only**.
* Presentation phase should avoid modifying components to ensure frame determinism.
* Use **fixed timestep** for stable simulations, and **alpha interpolation** for smooth rendering.
* Add a small sleep (e.g., `Thread.Sleep(1)`) in console loops to reduce CPU load.

---

## License

MIT © 2025 Pippapips Limited.
