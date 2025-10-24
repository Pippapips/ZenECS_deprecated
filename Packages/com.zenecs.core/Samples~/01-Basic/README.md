# ZenECS Core — Sample 01: Basic (Kernel)

A minimal **console** sample showing how to use the ZenECS **Kernel API** to run a simple simulation and presentation loop.

---

## 🧩 Overview

This sample demonstrates:

* ECS **world bootstrapping** with `EcsKernel.Start`
* Two basic components:

    * `Position` — entity coordinates
    * `Velocity` — movement delta per second
* Two systems with clear separation of concerns:

    * `MoveSystem` — simulation (writes)
    * `PrintPositionsSystem` — presentation (read-only)
* A frame loop with **variable delta** + **fixed timestep** integration (`Pump` + `LateFrame`)

---

## ⚙️ Systems

### `MoveSystem` (Simulation)

Integrates velocity into position:

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
```

### `PrintPositionsSystem` (Presentation)

Reads and prints all entity positions every frame:

```csharp
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

---

## 🧠 Main Loop Logic

The program creates two entities:

| Entity | Initial Position | Velocity  | Movement           |
| -----: | ---------------- | --------- | ------------------ |
|   `e1` | (0, 0)           | (1, 0)    | Moves +X direction |
|   `e2` | (2, 1)           | (0, −0.5) | Moves −Y direction |

Frame execution:

```csharp
const float fixedDelta = 1f / 60f;   // 60Hz simulation
const int maxSubStepsPerFrame = 4;

EcsKernel.Pump(dt, fixedDelta, maxSubStepsPerFrame, out var alpha);
EcsKernel.LateFrame(alpha);
```

This performs:

1. **BeginFrame(dt)** — variable step logic
2. **FixedStep(fixedDelta)** — zero or more substeps for deterministic updates
3. **LateFrame(alpha)** — read-only presentation/interpolation

---

## 🖥️ Example Output

```
=== ZenECS Core Console Sample 01: Basic (Kernel) ===
Running... press any key to exit.
-- FrameCount: 1 (alpha=0.33) --
Entity   1: pos=(0.02, 0.00)
Entity   2: pos=(2.00, 1.00)
-- FrameCount: 2 (alpha=0.52) --
Entity   1: pos=(0.04, 0.00)
Entity   2: pos=(2.00, 0.99)
...
Shutting down...
Done.
```

---

## 🧩 APIs Highlighted

| Category             | Key Methods                                                             |
| -------------------- | ----------------------------------------------------------------------- |
| **World**            | `CreateEntity()`, `Add<T>()`, `Read<T>()`, `Replace<T>()`, `Query<T>()` |
| **Kernel**           | `EcsKernel.Start()`, `Pump()`, `LateFrame()`, `Shutdown()`              |
| **System Execution** | `[SimulationGroup]`, `[PresentationGroup]`                              |
| **Timing**           | Variable step (`dt`) + Fixed step (`fixedDelta`) integration            |

---

## ✅ Best Practices

* **Write** components only in **SimulationGroup** systems.
* **Read-only** access in **PresentationGroup** systems ensures deterministic consistency.
* Use fixed-timestep for physics/AI updates and variable step for smooth input or visual logic.
* Limit CPU load in console samples using `Thread.Sleep(1)` inside the loop.

---

## 🧱 Build & Run

### Prerequisites

* .NET 8 SDK or newer
* ZenECS Core assemblies available in the same solution

### Command line

```bash
dotnet restore
dotnet build --no-restore
dotnet run --project Samples/ZenEcsCore-01-Basic.csproj
```

Press **any key** to exit.

---

## 📜 License

MIT © 2025 Pippapips Limited
See [LICENSE](https://opensource.org/licenses/MIT) for details.

---

원하신다면, 이 README를 기반으로 `02-Unity`나 `03-UniRx` 같은 다음 샘플의 문서 템플릿도 동일한 포맷으로 만들어드릴 수 있습니다.
