# ZenECS Core — Sample 01: Basic (Kernel)

A **console** sample demonstrating the ZenECS **Kernel loop** with a minimal simulation and presentation setup.

* Minimal components: `Position`, `Velocity`
* Systems:

    * `MoveSystem : IVariableRunSystem` — integrates `Position += Velocity * dt`
    * `PrintPositionsSystem : IPresentationSystem` — prints entity positions (read-only)
* Kernel loop:

    * `EcsKernel.Start(...)` registers systems
    * `Pump()` performs variable-step + fixed-step integration
    * `LateFrame()` runs presentation (read-only)

---

## What this sample shows

1. **World creation and system registration**
   The ECS world and systems are bootstrapped through `EcsKernel.Start`.

2. **Simulation → Presentation flow**
   `MoveSystem` updates `Position` each tick (write phase), and
   `PrintPositionsSystem` reads and prints entity positions in the Late phase.

3. **Variable + fixed timestep integration**
   The frame loop combines variable delta and fixed simulation updates, ensuring smooth deterministic results.

---

## TL;DR flow

```
[SimulationGroup] MoveSystem
    → integrates Position += Velocity * dt

[PresentationGroup] PrintPositionsSystem
    → reads Position and prints results

EcsKernel.Pump(dt, fixedDelta, maxSubSteps, out alpha)
EcsKernel.LateFrame(alpha)
```

Simulation writes; Presentation reads.
Presentation always runs in **Late** and is **read-only**.

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
            var p = w.Read<Position>(e); // read-only access
            Console.WriteLine($"Entity {e.Id,3}: pos={p}");
        }
    }
}
```

### Frame driver

```csharp
const float fixedDelta = 1f / 60f; // 60Hz simulation
const int maxSubStepsPerFrame = 4;

EcsKernel.Pump(dt, fixedDelta, maxSubStepsPerFrame, out var alpha);
EcsKernel.LateFrame(alpha);
```

---

## Build & Run

**Prereqs:** .NET 8 SDK and ZenECS Core assemblies referenced.

```bash
dotnet restore
dotnet build --no-restore
dotnet run --project <your-console-sample-csproj>
```

Press **any key** to exit.

---

## Example output

```
=== ZenECS Core Console Sample 01: Basic (Kernel) ===
Running... press any key to exit.
-- FrameCount: 1 (alpha=0.33) --
Entity   1: pos=(0.02, 0)
Entity   2: pos=(2, 1.00)
-- FrameCount: 2 (alpha=0.48) --
Entity   1: pos=(0.04, 0)
Entity   2: pos=(2, 0.99)
...
Shutting down...
Done.
```

---

## APIs highlighted

* **Kernel & loop:** `EcsKernel.Start`, `EcsKernel.Pump`, `EcsKernel.LateFrame`, `EcsKernel.Shutdown`
* **World:** `CreateEntity`, `Add<T>`, `Read<T>`, `Replace<T>`, `Query<T>`
* **Systems:** `[SimulationGroup]` for simulation writes, `[PresentationGroup]` for read-only display
* **Timing:** Fixed timestep (`fixedDelta`) + variable delta for interpolation

---

## Notes & best practices

* Separate systems into **Simulation** (write) and **Presentation** (read-only) groups.
* Use **fixed timestep** for deterministic logic and physics; use **alpha** for interpolation or smoothing.
* Keep presentation code stateless; it should reflect the data, not modify it.
* Add a small sleep (e.g., `Thread.Sleep(1)`) in console loops to reduce CPU load.

---

## License

MIT © 2025 Pippapips Limited.
