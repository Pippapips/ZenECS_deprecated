# ZenECS Core — Sample 03: View→Data via MessageBus

A **console** sample that demonstrates the ZenECS philosophy where the **view never writes ECS data directly**.
Instead, the view publishes **messages**; simulation systems consume them and mutate ECS state; presentation reads state (read-only).

* Minimal component: `Health`

* Message: `DamageRequest : IMessage`

* Systems:

    * `DamageSystem : IInitializeSystem, IVariableRunSystem` (Simulation — writes via messages)
    * `PrintHealthSystem : IPresentationSystem` (Presentation — read-only)

* Kernel loop:

    * `EcsKernel.Start(...)` registers systems
    * `Pump()` integrates variable step + fixed step
    * `LateFrame()` runs presentation

---

## What this sample shows

1. **View → Message**
   The “view” layer (here, console key input) never touches the world. It only calls `bus.Publish(new DamageRequest(...))`.

2. **Message → System → World**
   `DamageSystem` subscribes to `DamageRequest`, then updates `Health` components in Simulation.

3. **World → Presentation (read-only)**
   `PrintHealthSystem` queries `Health` and prints it during Late (no writes).

---

## TL;DR flow

```
[View/Input] → publish(DamageRequest) → [MessageBus]
      → [DamageSystem] (SimulationGroup) → update Health
      → [PrintHealthSystem] (PresentationGroup) → print HP (read-only)
```

All writes happen in **Simulation**; **Presentation** is read-only and runs in **Late**.

---

## File layout

```
Messages.cs
```

Key excerpts:

### Component

```csharp
public readonly struct Health
{
    public readonly int Value;
    public Health(int value) => Value = value;
    public override string ToString() => $"HP={Value}";
}
```

### Message

```csharp
public readonly struct DamageRequest : IMessage
{
    public readonly int EntityId;
    public readonly int Amount;
    public DamageRequest(int entityId, int amount)
    {
        EntityId = entityId;
        Amount = amount;
    }
}
```

### Systems

```csharp
[SimulationGroup]
public sealed class DamageSystem : IInitializeSystem, IVariableRunSystem
{
    private MessageBus? _bus;
    private IDisposable? _sub;

    public void Initialize(World w)
    {
        _bus = EcsKernel.Bus;
        _sub = _bus.Subscribe<DamageRequest>(m =>
        {
            if (!w.Exists(m.EntityId) || !w.Has<Health>(m.EntityId)) return;
            var hp = w.Read<Health>(m.EntityId);
            w.Replace(m.EntityId, new Health(Math.Max(0, hp.Value - m.Amount)));
            Console.WriteLine($"[Logic] e:{m.EntityId} took {m.Amount} → HP={Math.Max(0, hp.Value - m.Amount)}");
        });
    }

    public void Run(World w) => _bus?.PumpAll(); // drain messages per simulation tick
}

[PresentationGroup]
public sealed class PrintHealthSystem : IPresentationSystem
{
    public void Run(World w, float alpha)
    {
        Console.WriteLine($"-- Frame {w.FrameCount} (alpha={alpha:0.00}) --");
        foreach (var e in w.Query<Health>())
            Console.WriteLine($"Entity {e.Id,2}: {w.Read<Health>(e)}");
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

Press **1** or **2** to publish `DamageRequest` to entity 1 or 2.
Press **ESC** to exit.

---

## Example output

```
=== ZenECS Core Sample — View→Data via MessageBus (Kernel) ===
Running... press [1]/[2] to deal damage, [ESC] to quit.
[View] Sent DamageRequest → e:1
[Logic] e:1 took 12 → HP=88
-- Frame 5 (alpha=0.60) --
Entity  1: HP=88
Entity  2: HP=75
[View] Sent DamageRequest → e:2
[Logic] e:2 took 7 → HP=68
-- Frame 10 (alpha=0.41) --
Entity  1: HP=88
Entity  2: HP=68
Shutting down...
Done.
```

---

## APIs highlighted

* **Kernel & loop:** `EcsKernel.Start/ Pump/ LateFrame/ Shutdown`
* **Bus:** `MessageBus.Publish`, `MessageBus.Subscribe<T>`, `PumpAll`
* **World:** `CreateEntity`, `Add<T>`, `Has<T>`, `Read<T>`, `Replace<T>`, `Query<T>`
* **Systems:** `[SimulationGroup]` (writes), `[PresentationGroup]` (read-only)

---

## Notes & best practices

* UI/View code should **never** mutate ECS data directly—**always** publish messages.
* Keep message handlers small and focused (one message → one responsibility).
* Presentation is **Late** and **read-only** for determinism and clarity.
* Use a fixed timestep for stable simulation; use `alpha` for interpolation if needed.

---

## License

MIT © 2025 Pippapips Limited.
