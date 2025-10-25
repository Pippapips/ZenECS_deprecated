# ZenECS Core — Sample 06: World Hooks (Kernel)

A **console** sample demonstrating **per-world hooks** in ZenECS:
how to restrict read/write access and validate component data dynamically at runtime.

* Component: `Mana`
* Systems:

    * `WorldHooksDemoSystem : IVariableRunSystem` — installs read/write hooks, validators, and tests them
    * `PrintSummarySystem : IPresentationSystem` — read-only Late system for final state logging
* Kernel loop:

    * `EcsKernel.Start(...)` registers systems
    * `Pump()` performs variable + fixed-step updates
    * `LateFrame()` runs presentation systems (read-only)

---

## What this sample shows

1. **Write permissions**
   Restricts which entities/types can be written.
   Example: only **even entity IDs** can be written.

2. **Validators**
   Ensures component data meets defined conditions.
   Example: `Mana.Value >= 0` must hold true for all writes.

3. **Read permissions**
   Controls which components can be read by the world.
   Example: only `Mana` type is readable — all others denied.

4. **Hook cleanup**
   Hooks can be removed dynamically to restore unrestricted world behavior.

---

## TL;DR flow

```
WritePermission: (EntityId % 2 == 0)
Validator: Mana.Value >= 0
ReadPermission: type == Mana

→ Add e1(Mana=10)  → denied (odd id)
→ Add e2(Mana=-10) → denied (invalid)
→ Add e2(Mana=5)   → OK
→ Read e2(Mana)    → allowed
→ Remove all hooks → unrestricted again
```

---

## File layout

```
WorldHook.cs
```

Key excerpts:

### Installing hooks

```csharp
// Write: even IDs only
w.AddWritePermission((world, e, t) => (e.Id & 1) == 0);

// Validator: Mana must be >= 0
w.AddValidator<Mana>(m => m.Value >= 0);

// Read: allow only Mana type
w.AddReadPermission((world, e, t) => t == typeof(Mana));
```

### System logic

```csharp
[SimulationGroup]
public sealed class WorldHooksDemoSystem : IVariableRunSystem
{
    public void Run(World w)
    {
        var e1 = w.CreateEntity(); // odd id
        var e2 = w.CreateEntity(); // even id

        TryAdd(w, e1, new Mana(10));   // denied by write perm
        TryAdd(w, e2, new Mana(-10));  // denied by validator
        TryAdd(w, e2, new Mana(5));    // OK

        if (w.TryRead<Mana>(e2, out var mana))
            Console.WriteLine($"Read OK: e:{e2.Id} Mana={mana.Value}");
        else
            Console.WriteLine("Read denied");

        w.ClearReadPermissions();
        w.RemoveAllValidators();
        w.ClearWritePermissions();
    }
}
```

### Read-only presentation

```csharp
[PresentationGroup]
public sealed class PrintSummarySystem : IPresentationSystem
{
    public void Run(World w, float alpha)
    {
        foreach (var e in w.Query<Mana>())
            Console.WriteLine($"Entity {e.Id}: Mana={w.Read<Mana>(e).Value}");
    }
}
```

### Frame driver

```csharp
const float fixedDelta = 1f / 60f;
const int   maxSubSteps = 4;
EcsKernel.Pump(dt, fixedDelta, maxSubSteps, out var alpha);
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
=== ZenECS Core Sample — World Hooks (Kernel) ===
=== World Hooks demo (read/write permissions, validator) ===
Add<Mana> FAIL on e:1 :: Write denied by policy
Add<Mana> FAIL on e:2 :: Validation failed
Add<Mana> OK on e:2 -> Mana=5
Read OK (e:2) -> Mana=5
All hooks removed.
[Late] Frame 1, alive=2
Entity  2: Mana=5
Shutting down...
Done.
```

---

## APIs highlighted

* **World Hook APIs**

    * `World.AddValidator<T>(Predicate<T>)`
    * `World.AddWritePermission(Func<World,Entity,Type,bool>)`
    * `World.AddReadPermission(Func<World,Entity,Type,bool>)`
    * `World.RemoveValidator`, `World.RemoveWritePermission`, `World.ClearReadPermissions`
* **Runtime logging**

    * `EcsRuntimeOptions.Log`
    * `EcsRuntimeOptions.WriteFailurePolicy`
* **System grouping**

    * `[SimulationGroup]` (write)
    * `[PresentationGroup]` (read-only)

---

## Notes & best practices

* Use **validators** to enforce invariant constraints (e.g., non-negative HP).
* Use **permissions** to limit who/what can modify or observe ECS data.
* Presentation systems must always remain **read-only**.
* Hooks are **per-world** — each world instance can have independent rules.
* Clear hooks before world reset or reuse to avoid side effects.
* Combine with `EcsRuntimeOptions.WriteFailurePolicy.Throw` during tests for safety.

---

## License

MIT © 2025 Pippapips Limited.
