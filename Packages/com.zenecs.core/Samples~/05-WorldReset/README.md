# ZenECS Core — Sample 05: World Reset (Kernel)

A **console** sample demonstrating **`World.Reset(keepCapacity)`** behaviors:

* `Reset(true)`  : **fast clear** — removes all entities/components but **preserves** internal arrays/pools
* `Reset(false)` : **hard reset** — rebuilds internal structures from the **initial config**

- Component: `Health`
- Systems:

    * `WorldResetDemoSystem : IVariableRunSystem` — seeds, resets (keep vs hard), and logs results
    * `PrintSummarySystem : IPresentationSystem` — read-only Late logs
- Kernel loop:

    * `EcsKernel.Start(...)` registers systems
    * `Pump()` integrates variable + fixed steps
    * `LateFrame()` runs presentation (read-only)

---

### What this sample shows

1. **Fast reset (keep capacity)**
   Clear all data while preserving memory pools and internal arrays for quick reuse.

2. **Hard reset (reinitialize)**
   Rebuild internal storage to the initial configuration for a fully fresh world.

3. **Read-only presentation**
   Keep presentation in **Late** and **read-only** to maintain deterministic flow.

---

### TL;DR flow

```
Seed world (e1,e2 with Health)
→ Reset(keepCapacity:true)
→ Re-seed (e3 with Health)
→ Reset(keepCapacity:false)
```

Presentation continuously prints a short summary in Late.

---

### File layout

```
WorldReset.cs
```

Key excerpts:

```csharp
[SimulationGroup]
public sealed class WorldResetDemoSystem : IVariableRunSystem
{
    public void Run(World w)
    {
        var e1 = w.CreateEntity();
        var e2 = w.CreateEntity();
        w.Add(e1, new Health(100));
        w.Add(e2, new Health(50));

        Console.WriteLine($"Before reset: alive={w.AliveCount}, e1.Has(Health)={w.Has<Health>(e1)}");

        w.Reset(keepCapacity: true);
        Console.WriteLine($"After Reset(keepCapacity:true): alive={w.AliveCount}");

        var e3 = w.CreateEntity();
        w.Add(e3, new Health(77));
        Console.WriteLine($"Re-seed: alive={w.AliveCount}, e3.Has(Health)={w.Has<Health>(e3)}");

        w.Reset(keepCapacity: false);
        Console.WriteLine($"After Reset(keepCapacity:false): alive={w.AliveCount}");
    }
}

[PresentationGroup]
public sealed class PrintSummarySystem : IPresentationSystem
{
    public void Run(World w, float alpha)
        => Console.WriteLine($"[Late] Frame {w.FrameCount}, alive={w.AliveCount}");
}
```

Frame driver (Basic style):

```csharp
const float fixedDelta = 1f / 60f; // 60Hz
const int   maxSubSteps = 4;
EcsKernel.Pump(dt, fixedDelta, maxSubSteps, out var alpha);
EcsKernel.LateFrame(alpha);
```

---

### Build & Run

**Prereqs:** .NET 8 SDK and ZenECS Core assemblies referenced.

```bash
dotnet restore
dotnet build --no-restore
dotnet run --project <your-console-sample-csproj>
```

Press **any key** to exit.

---

### Example output

```
=== ZenECS Core Sample — World Reset (Kernel) ===
Running... press any key to exit.
=== World.Reset demo (keepCapacity vs hard reset) ===
Before reset: alive=2, e1.Has(Health)=True
After Reset(keepCapacity:true): alive=0
Re-seed: alive=1, e3.Has(Health)=True
After Reset(keepCapacity:false): alive=0
[Late] Frame 1, alive=0
Shutting down...
Done.
```

---

### APIs highlighted

* **World reset:** `World.Reset(bool keepCapacity)`
* **World ops:** `CreateEntity`, `Add<T>`, `Has<T>`, `AliveCount`
* **Kernel loop:** `EcsKernel.Start`, `Pump`, `LateFrame`, `Shutdown`
* **System phases:** `[SimulationGroup]` (writes), `[PresentationGroup]` (read-only)

---

### Notes & best practices

* Prefer **`Reset(true)`** for scene/level transitions to reuse memory and reduce GC churn.
* Use **`Reset(false)`** when you need a fully reinitialized world (e.g., config changes).
* Keep presentation systems **read-only** and **Late** to avoid race conditions.
* Consider exposing reset options in your game’s state manager (menus, editor tooling, etc.).

---

더 필요한 샘플(예: `World.ClearAllComponents()`, `Snapshot + Reset` 조합 등)이 있으면 같은 형식으로 바로 만들어 드릴게요.
