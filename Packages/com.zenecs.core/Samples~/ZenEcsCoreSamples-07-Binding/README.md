# ZenECS Core — Binding Console Sample

A tiny **console** sample that demonstrates the ZenECS **Binding** pipeline end-to-end, without Unity.

* Minimal component: `Position`
* Concrete binder: `PositionBinder : IComponentBinder<Position>`
* Simple view: `ConsoleViewBinder`
* Presentation systems (run in **Late** only):

    * `ComponentBindingHubSystem` (collects component changes per frame)
    * `ComponentBatchDispatchSystem` (publishes/consumes change batches)
    * `ViewBindingSystem` (Bind/Apply/Unbind to views)

---

## What this sample shows

1. **Entity & View wiring**
   Create an entity, register a view binder, and register a component binder.

2. **Change collection → batch dispatch → view apply**
   Add / replace / remove a component and observe how the three presentation systems route those changes to the view.

3. **Frame loop integration**
   Use `EcsKernel.Pump(...)` for variable step + fixed substeps and `EcsKernel.LateFrame(alpha)` to run presentation.

---

## TL;DR flow

```
World changes  →  ComponentBindingHubSystem (collect per frame)
               →  ComponentBatchDispatchSystem (batch publish)
               →  ViewBindingSystem (Bind / Apply / Unbind)
```

All three systems are in the **Presentation group** and therefore run in **Late**.

---

## File layout

```
Binding.cs
```

Key excerpts:

### Component

```csharp
public readonly struct Position : IEquatable<Position>
{
    public readonly float X, Y;
    public Position(float x, float y) { X = x; Y = y; }
    public bool Equals(Position other) => X.Equals(other.X) && Y.Equals(other.Y);
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public override string ToString() => $"({X:0.##}, {Y:0.##})";
}
```

### Binder

```csharp
public sealed class PositionBinder : IComponentBinder<Position>, IComponentBinder
{
    public Type ComponentType => typeof(Position);
    public void Bind(World w, Entity e, IViewBinder v)
        => Console.WriteLine($"[Bind]   e={e} Position");
    public void Apply(World w, Entity e, in Position value, IViewBinder v)
        => Console.WriteLine($"[Apply]  e={e} Position={value}");
    public void Unbind(World w, Entity e, IViewBinder v)
        => Console.WriteLine($"[Unbind] e={e} Position");

    // non-generic fallback kept for compatibility
    void IComponentBinder.Apply(World w, Entity e, object value, IViewBinder t)
        => Apply(w, e, (Position)value, t);
}
```

### View (console stub)

```csharp
public sealed class ConsoleViewBinder : IViewBinder
{
    public string Name { get; }
    public ConsoleViewBinder(string name) => Name = name;
    public override string ToString() => Name;
}
```

### Frame driver

```csharp
const float fixedDelta = 1f / 60f;   // 60Hz
const int   maxSubSteps = 4;

EcsKernel.Pump(dt, fixedDelta, maxSubSteps, out var alpha);
EcsKernel.LateFrame(alpha);
```

---

## Build & Run

**Prereqs:** .NET 8 SDK, and ZenECS Core assemblies referenced so the types resolve.

```bash
dotnet restore
dotnet build --no-restore
dotnet run --project <your-console-sample-csproj>
```

Stop the app by pressing any key (the sample removes `Position` before shutting down).

---

## Example output

```
Running... press any key to exit.
[Bind]   e=Entity(1:0) Position
[Apply]  e=Entity(1:0) Position=(2.5, 4)
[Unbind] e=Entity(1:0) Position
Shutting down...
Done.
```

* Add → `Bind` + `Apply`
* Replace → `Apply`
* Remove → `Unbind`

---

## APIs highlighted

* **Kernel & loop:** `EcsKernel.Start/InitializeSystems/Pump/LateFrame/Shutdown`
* **Binders:** `IComponentBinder<T>.Bind/Apply/Unbind` (uses `in T` for value-type zero-copy)
* **Registries/Resolver:** `ComponentBinderRegistry`, `ComponentBinderResolver`, `ViewBinderRegistry`
* **Presentation systems:** `ComponentBindingHubSystem`, `ComponentBatchDispatchSystem`, `ViewBindingSystem`

---

## Notes & best practices

* Keep binders **idempotent**: applying the same value shouldn’t cause unintended side-effects.
* Presentation runs in **Late**: game logic mutates components earlier; views update once at the end of the frame.
* For Unity hosts, plug a **Unity main-thread gate** (not shown here) to guarantee dispatcher execution on the main thread.

---

## License

MIT © 2025 Pippapips Limited.
