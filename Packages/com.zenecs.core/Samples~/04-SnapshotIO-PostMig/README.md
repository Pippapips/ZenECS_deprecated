# ZenECS Core — Sample 04: Snapshot I/O + Post Migration (Kernel)

A **console** sample demonstrating how to use the ZenECS **Snapshot I/O system**
to save and load world data, and how to apply a **Post-Load Migration** when component versions change.

* Components: `PositionV1`, `PositionV2`
* Systems:

    * `SnapshotDemoSystem : IVariableRunSystem` — creates a binary snapshot, reloads it, and migrates data
    * `PrintSummarySystem : IPresentationSystem` — reads and prints final migrated entities
* Kernel loop:

    * `EcsKernel.Start(...)` initializes world and systems
    * `Pump()` performs simulation steps
    * `LateFrame()` runs presentation systems (read-only)

---

## What this sample shows

1. **Saving & loading snapshots**
   Serializes world state into a binary stream using ZenECS `SaveFullSnapshotBinary`
   and loads it into a fresh world with `LoadFullSnapshotBinary`.

2. **Versioned component migration (PostLoadMigration)**
   Demonstrates converting legacy component data (`PositionV1`)
   to a new version (`PositionV2`) after snapshot load.

3. **Post-migration verification**
   After migration, the world contains only `PositionV2` components,
   which are logged in the Presentation phase.

---

## TL;DR flow

```
[SnapshotDemoSystem]
   → Create entity with PositionV1
   → SaveFullSnapshotBinary(stream)
   → LoadFullSnapshotBinary(stream) into new world
   → Run migration (PositionV1 → PositionV2)
   → Verify result (PositionV2 only)

[PrintSummarySystem]
   → Logs migrated PositionV2 components (read-only Late)
```

---

## File layout

```
SnapshotIO_PostMig.cs
```

Key excerpts:

### Versioned Components

```csharp
public readonly struct PositionV1
{
    public readonly float X, Y;
    public PositionV1(float x, float y) { X = x; Y = y; }
}

public readonly struct PositionV2
{
    public readonly float X, Y;
    public readonly int Layer;
    public PositionV2(float x, float y, int layer = 0)
    {
        X = x; Y = y; Layer = layer;
    }
}
```

### Migration logic

```csharp
public sealed class DemoPostLoadMigration : IPostLoadMigration
{
    public int Order => 0;

    public void Run(World world)
    {
        foreach (var e in world.Query<PositionV1>())
        {
            var old = world.Read<PositionV1>(e);
            world.Replace(e, new PositionV2(old.X, old.Y, layer: 1));
            world.Remove<PositionV1>(e);
        }
    }
}
```

### Systems

```csharp
[SimulationGroup]
public sealed class SnapshotDemoSystem : IVariableRunSystem
{
    private bool _done;

    public void Run(World w)
    {
        if (_done) return;
        Console.WriteLine("=== Snapshot I/O + Post-Migration Demo ===");

        // Register StableIds & formatters
        ComponentRegistry.Register("com.zenecs.samples.position.v1", typeof(PositionV1));
        ComponentRegistry.Register("com.zenecs.samples.position.v2", typeof(PositionV2));
        ComponentRegistry.RegisterFormatter(new PositionV1Formatter(), "com.zenecs.samples.position.v1");
        ComponentRegistry.RegisterFormatter(new PositionV2Formatter(), "com.zenecs.samples.position.v2");

        // Create V1 data
        var e = w.CreateEntity();
        w.Add(e, new PositionV1(3, 7));

        // Save snapshot
        using var ms = new MemoryStream();
        w.SaveFullSnapshotBinary(ms);

        // Load snapshot into new world
        var world2 = new World(new WorldConfig(initialEntityCapacity: 8));
        ComponentRegistry.RegisterFormatter(new PositionV1Formatter(), "com.zenecs.samples.position.v1");
        ComponentRegistry.RegisterFormatter(new PositionV2Formatter(), "com.zenecs.samples.position.v2");
        ms.Position = 0;
        world2.LoadFullSnapshotBinary(ms);

        // Post-migration
        new DemoPostLoadMigration().Run(world2);

        // Verify
        foreach (var e2 in world2.Query<PositionV2>())
        {
            var p = world2.Read<PositionV2>(e2);
            Console.WriteLine($"Migrated entity {e2.Id} → {p}");
        }

        _done = true;
    }
}

[PresentationGroup]
public sealed class PrintSummarySystem : IPresentationSystem
{
    public void Run(World w, float alpha)
    {
        foreach (var e in w.Query<PositionV2>())
        {
            var p = w.Read<PositionV2>(e);
            Console.WriteLine($"Frame {w.FrameCount} Entity {e.Id}: PositionV2={p}");
        }
    }
}
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
=== ZenECS Core Sample — SnapshotIO + PostMigration (Kernel) ===
=== Snapshot I/O + Post-Migration Demo ===
Saved snapshot bytes: 56
Migrated entity 1 → (3, 7, layer:1)
Frame 1 Entity 1: PositionV2=(3, 7, layer:1)
Shutting down...
Done.
```

---

## APIs highlighted

* **Serialization**

    * `World.SaveFullSnapshotBinary(Stream)`
    * `World.LoadFullSnapshotBinary(Stream)`
* **Migration**

    * `IPostLoadMigration`
    * `World.Replace`, `World.Remove`
* **World & Registry**

    * `ComponentRegistry.Register`, `RegisterFormatter`
* **Kernel**

    * `EcsKernel.Start`, `Pump`, `LateFrame`, `Shutdown`

---

## Notes & best practices

* Maintain **StableId strings** for every component version.
  (`com.zenecs.samples.position.v1`, `...v2`, etc.)
* Register all formatters **before snapshot I/O**.
* Always perform **migration** after load to reconcile old formats.
* Keep migration idempotent — running it twice shouldn’t corrupt data.
* Presentation systems remain **read-only** and display results only.

---

## License

MIT © 2025 Pippapips Limited.
