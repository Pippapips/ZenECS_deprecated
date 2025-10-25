# ZenECS Core — Sample 07: Write Hooks & Validators (Kernel)

A **console** sample demonstrating how ZenECS uses **per-world hooks** for
write validation, permission policies, and runtime safeguards.

* Component: `Mana`
* Systems:

    * `WriteHooksDemoSystem : IVariableRunSystem` — installs validators & permissions, performs writes
    * `PrintManaSystem : IPresentationSystem` — prints results read-only in Late
* Kernel loop:

    * `EcsKernel.Start(...)` registers systems
    * `Pump()` performs variable + fixed-step updates
    * `LateFrame()` runs presentation systems

---

## What this sample shows

1. **Type-level validation (AddValidator)**
   Adds a per-type validator — forbids invalid component data.
   Example: `w.AddValidator<Mana>(m => m.Value >= 0)` rejects negative values.

2. **Entity-level write permissions (AddWritePermission)**
   Defines policies deciding whether a write is allowed for each entity/type.
   Example: only **even entity IDs** can be written to.

3. **Runtime logging of policy failures**
   Failed writes and validator rejections are logged via `EcsRuntimeOptions.Log`
   when `WriteFailurePolicy.Log` is set.

---

## TL;DR flow

```
Validator<Mana>: Value >= 0
WritePermission: (EntityId % 2 == 0)

→ Add e1(Mana=10)   → denied (odd id)
→ Add e2(Mana=-5)   → denied (invalid value)
→ Add e2(Mana=25)   → OK
→ Replace / Remove also respect same hooks
```

---

## File layout

```
WriteHooks_Validators.cs
```

Key excerpts:

```csharp
// Type-level validator
w.AddValidator<Mana>(m => m.Value >= 0);

// Write permission: allow even entity IDs only
w.AddWritePermission((world, e, t) => (e.Id & 1) == 0);
```

### Sample system logic

```csharp
[SimulationGroup]
public sealed class WriteHooksDemoSystem : IVariableRunSystem
{
    public void Run(World w)
    {
        w.AddValidator<Mana>(m => m.Value >= 0);
        w.AddWritePermission((world, e, t) => (e.Id & 1) == 0);

        var e1 = w.CreateEntity(); // odd id
        var e2 = w.CreateEntity(); // even id

        TryAdd(w, e1, new Mana(10));   // denied by write permission
        TryAdd(w, e2, new Mana(-5));   // denied by validator
        TryAdd(w, e2, new Mana(25));   // allowed
    }
}
```

### Presentation system

```csharp
[PresentationGroup]
public sealed class PrintManaSystem : IPresentationSystem
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
=== ZenECS Core Sample — WriteHooks & Validators (Kernel) ===
=== Write Hooks & Validators demo ===
Add<Mana> FAIL on e:1 :: Write denied by policy
Add<Mana> FAIL on e:2 :: Validation failed
Add<Mana> OK on e:2 -> Mana=25
Replace FAIL e:1 :: Write denied by policy
Remove FAIL  e:1 :: Write denied by policy
-- Frame 1 (alpha=0.33) --
Entity  2: 25
Shutting down...
Done.
```

---

## APIs highlighted

* **Validators:**
  `World.AddValidator<T>(Predicate<T> predicate)`
  `World.RemoveValidator(...)`
* **Permissions:**
  `World.AddWritePermission(Func<World,Entity,Type,bool>)`
  `World.RemoveWritePermission(...)`
* **Runtime logging:**
  `EcsRuntimeOptions.Log`, `EcsRuntimeOptions.WriteFailurePolicy.Log`
* **Read-only systems:**
  `[PresentationGroup]` runs only in Late

---

## Notes & best practices

* Use **validators** to prevent invalid state (e.g., HP < 0).
* Use **permissions** to restrict who can modify data (e.g., systems, team logic).
* Keep presentation systems **read-only** — they must never write.
* Always remove hooks in teardown if your simulation restarts or loads new scenes.
* Set `EcsRuntimeOptions.WriteFailurePolicy.Throw` during tests to catch violations.

---

## License

MIT © 2025 Pippapips Limited.