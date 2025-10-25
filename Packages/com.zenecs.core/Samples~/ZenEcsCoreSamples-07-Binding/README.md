# ZenECS Core — Sample 07: Binding (Console)

A **console** sample that demonstrates the ZenECS **binding pipeline** end-to-end (Unity-free):

* Minimal component: `Position`
* Binders & view:

    * `PositionBinder : IComponentBinder<Position>` — writes to a console “view”
    * `ConsoleViewBinder : IViewBinder` — stub view object associated with an entity
* Presentation systems (run in **Late** via the kernel’s presentation group) are registered globally by the runtime (Hub/Batch/ViewBinding chain).
* Kernel loop:

    * `EcsKernel.Start(...)` bootstraps world and registers binders/views
    * `Pump()` integrates variable + fixed steps
    * `LateFrame()` flushes presentation/binding

---

## What this sample shows

1. **Entity ↔ View association**
   Registers a `ConsoleViewBinder` for an entity so the binding pipeline can target it.

2. **Component binder lifecycle**
   `Bind` on first appearance, `Apply` on value changes, `Unbind` on removal — logged to console.

3. **Kernel-driven frame loop**
   Uses `Pump(dt, fixedDelta, maxSubSteps, out alpha)` + `LateFrame(alpha)`; pressing any key removes the component and triggers `Unbind`.

---

## TL;DR flow

```
World.Add(Position)    → [Bind] once + [Apply]
World.Replace(Position)→ [Apply] (value changed)
World.Remove(Position) → [Unbind]
```

All view updates occur in **Late** after simulation.

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
    public override string ToString() => $"({X:0.##}, {Y:0.##})";
}
```

### Binder

```csharp
public sealed class PositionBinder : IComponentBinder<Position>, IComponentBinder
{
    public Type ComponentType => typeof(Position);

    public void Bind (World w, Entity e, IViewBinder v)               => Console.WriteLine($"[Bind]   e={e} Position");
    public void Apply(World w, Entity e, in Position value, IViewBinder v) => Console.WriteLine($"[Apply]  e={e} Position={value}");
    public void Unbind(World w, Entity e, IViewBinder v)              => Console.WriteLine($"[Unbind] e={e} Position");

    // Explicit non-generic fallbacks
    void IComponentBinder.Bind(World w, Entity e, IViewBinder t)               => Bind(w, e, t);
    void IComponentBinder.Apply(World w, Entity e, object value, IViewBinder t)=> Apply(w, e, (Position)value, t);
    void IComponentBinder.Unbind(World w, Entity e, IViewBinder t)            => Unbind(w, e, t);
}
```

### View stub

```csharp
public sealed class ConsoleViewBinder : IViewBinder
{
    public string Name { get; }
    public ConsoleViewBinder(string name) => Name = name;
    public override string ToString() => Name;

    public Entity Entity { get; }
    public int HandleId { get; }

    public void SetEntity(Entity e) => throw new NotImplementedException();
}
```

### Kernel setup & frame driver

```csharp
EcsKernel.Start(
    new WorldConfig(initialEntityCapacity: 256),
    systems: null,
    options: null,
    mainThreadGate: null,
    systemRunnerLog: Console.WriteLine,
    configure: (world, bus) =>
    {
        // Register binder
        EcsRuntimeDirectory.ComponentBinderRegistry?.RegisterSingleton<Position>(new PositionBinder());

        // Create entity + attach view
        var e = world.CreateEntity();
        var view = new ConsoleViewBinder("Player-View");
        EcsRuntimeDirectory.ViewBinderRegistry?.Register(e, view);

        // Drive binder lifecycle
        world.Add(e, new Position(1, 1));
        world.Replace(e, new Position(2.5f, 4));
    }
);

const float fixedDelta = 1f / 60f; // 60Hz
const int   maxSubStepsPerFrame = 4;

EcsKernel.Pump(dt, fixedDelta, maxSubStepsPerFrame, out var alpha);
EcsKernel.LateFrame(alpha);
```

Press any key to remove the component and trigger `[Unbind]`.

---

## Build & Run

**Prereqs:** .NET 8 SDK and ZenECS Core assemblies referenced.

```bash
dotnet restore
dotnet build --no-restore
dotnet run --project <your-console-sample-csproj>
```

Press **any key** to remove `Position` (causing `Unbind`) and exit.

---

## Example output

```
Running... press any key to exit.
[Bind]   e=Entity(1:0) Position
[Apply]  e=Entity(1:0) Position=(1, 1)
[Apply]  e=Entity(1:0) Position=(2.5, 4)
[Unbind] e=Entity(1:0) Position
Shutting down...
Done.
```

---

## APIs highlighted

* **Kernel & loop:** `EcsKernel.Start`, `EcsKernel.Pump`, `EcsKernel.LateFrame`, `EcsKernel.Shutdown`
* **Binding runtime:**

    * `ComponentBinderRegistry.RegisterSingleton<T>()`
    * `ViewBinderRegistry.Register(entity, viewBinder)`
* **Binder contract:** `IComponentBinder<T>.Bind/Apply/Unbind` (+ non-generic `IComponentBinder`)
* **World ops:** `CreateEntity`, `Add<T>`, `Replace<T>`, `Remove<T>`

---

## Notes & best practices

* Keep binders **idempotent**; repeated `Apply` with the same value should not cause side-effects.
* Drive **all view updates in Late**; simulation systems should not touch views.
* For real apps, ensure binding/dispatch runs on the **main thread** (e.g., Unity).
* If you see duplicate `[Apply]`, ensure batch dispatch and reconcile paths don’t both apply in the same Late frame (coalesce or skip already-handled entities).

---

## License

MIT © 2025 Pippapips Limited.
