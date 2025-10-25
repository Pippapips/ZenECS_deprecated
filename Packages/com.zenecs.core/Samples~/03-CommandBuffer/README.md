# ZenECS Core — Sample 03: CommandBuffer (Kernel)

A **console** sample demonstrating the ZenECS **CommandBuffer API**,
which enables thread-safe, batched component modifications — applied either **deferred** or **immediately**.

* Components: `Health`, `Stunned`
* Systems:

    * `CommandBufferDemoSystem : IVariableRunSystem` — demonstrates deferred & immediate CommandBuffer use
    * `PrintStatusSystem : IPresentationSystem` — read-only view of current entity states
* Kernel loop:

    * `EcsKernel.Start(...)` bootstraps world and registers systems
    * `Pump()` performs variable + fixed-step updates
    * `LateFrame()` runs presentation (read-only)

---

## What this sample shows

1. **Deferred execution (Schedule + RunScheduledJobs)**
   Command operations (`Add`, `Replace`, `Remove`) are collected in a buffer via `BeginWrite()`,
   then scheduled with `world.Schedule(cb)` and later applied all at once via `world.RunScheduledJobs()`.

2. **Immediate execution (ApplyMode.Immediate)**
   A write scope created with `BeginWrite(World.ApplyMode.Immediate)` applies changes instantly on `Dispose()`.

3. **Thread-safe batching**
   CommandBuffers allow multithreaded collection of component changes, safely synchronized at frame boundaries.

---

## TL;DR flow

```
World.BeginWrite()
   → cb.Add / Replace / Remove
   → world.Schedule(cb)
   → world.RunScheduledJobs()   // deferred apply

using (world.BeginWrite(World.ApplyMode.Immediate))
   → cb.Replace(...)
   // immediate apply on Dispose()
```

* **Deferred** = batched safely, applied later
* **Immediate** = applied instantly

---

## File layout

```
CommandBuffer.cs
```

Key excerpts:

### Components

```csharp
public readonly struct Health
{
    public readonly int Value;
    public Health(int value) => Value = value;
    public override string ToString() => Value.ToString();
}

public readonly struct Stunned
{
    public readonly float Seconds;
    public Stunned(float seconds) => Seconds = seconds;
    public override string ToString() => $"{Seconds:0.##}s";
}
```

### Systems

```csharp
[SimulationGroup]
public sealed class CommandBufferDemoSystem : IVariableRunSystem
{
    private bool _done;

    public void Run(World w)
    {
        if (_done) return;

        // Create entities
        var e1 = w.CreateEntity();
        var e2 = w.CreateEntity();

        // Deferred apply
        var cb = w.BeginWrite();
        cb.Add(e1, new Health(100));
        cb.Add(e2, new Health(80));
        cb.Add(e2, new Stunned(1.5f));
        cb.Replace(e2, new Health(75));
        cb.Remove<Stunned>(e2);
        w.Schedule(cb);
        w.RunScheduledJobs();

        // Immediate apply
        using (var cb2 = w.BeginWrite(World.ApplyMode.Immediate))
        {
            cb2.Replace(e1, new Health(42));
        }

        _done = true;
    }
}

[PresentationGroup]
public sealed class PrintStatusSystem : IPresentationSystem
{
    public void Run(World w, float alpha)
    {
        Console.WriteLine($"-- Frame {w.FrameCount} (alpha={alpha:0.00}) --");
        foreach (var e in w.Query<Health>())
        {
            var h = w.Read<Health>(e);
            var stunned = w.Has<Stunned>(e) ? w.Read<Stunned>(e).ToString() : "no";
            Console.WriteLine($"Entity {e.Id,2}: Health={h.Value}, Stunned={stunned}");
        }
    }
}
```

### Frame driver

```csharp
const float fixedDelta = 1f / 60f; // 60Hz
const int   maxSubSteps = 4;

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

Press **any key** to exit.

---

## Example output

```
=== ZenECS Core Sample — CommandBuffer (Kernel) ===
=== CommandBuffer demo (deferred + immediate) ===
Scheduled ops. Before apply, Has<Health>(e1): False
After apply (deferred): e1 Health=100, e2 Health=75, Has<Stunned>(e2)=False
After immediate EndWrite: e1 Health=42
-- Frame 1 (alpha=0.33) --
Entity  1: Health=42, Stunned=no
Entity  2: Health=75, Stunned=no
Shutting down...
Done.
```

---

## APIs highlighted

* **CommandBuffer API**

    * `World.BeginWrite()`, `cb.Add`, `cb.Replace`, `cb.Remove`
    * `World.Schedule(cb)`, `World.RunScheduledJobs()`
    * `World.BeginWrite(World.ApplyMode.Immediate)` (immediate apply)
* **World**

    * `CreateEntity`, `Has<T>`, `Read<T>`
* **Systems**

    * `[SimulationGroup]` (writes)
    * `[PresentationGroup]` (read-only)
* **Kernel loop**

    * `EcsKernel.Start`, `EcsKernel.Pump`, `EcsKernel.LateFrame`, `EcsKernel.Shutdown`

---

## Notes & best practices

* Use **deferred buffers** for thread-safe, batched writes across systems or threads.
  Apply them once per frame using `RunScheduledJobs()`.
* Use **immediate buffers** when safe in single-threaded contexts for fast inline changes.
* Presentation systems must remain **read-only**.
* CommandBuffers are automatically pooled; avoid long-term retention.

---

## License

MIT © 2025 Pippapips Limited.
