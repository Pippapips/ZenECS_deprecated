# ZenECS Core — 01 Core Console (Realtime loop)

A tiny **console** sample that demonstrates a realtime ZenECS Core loop:

* Minimal components: `Position`, `Velocity`
* Two systems with clear roles:

    * `MoveSystem : IVariableRunSystem` (Simulation — writes)
    * `PrintPositionsSystem : IPresentationSystem` (Presentation — read-only)
* World queries via `World.Query<…>()`, reading/updating with `Read`/`Replace`
* A main loop that runs until **any key** is pressed
* Mixed timing: per-frame variable step + fixed-timestep accumulator

---

## What this sample does

1. Builds a `World`, creates two entities:

    * `e1`: `Position(0,0)`, `Velocity(1,0)` → moves +X
    * `e2`: `Position(2,1)`, `Velocity(0,-0.5)` → moves −Y
2. Composes a `SystemRunner` with:

    * `MoveSystem` (Simulation)
    * `PrintPositionsSystem` (Presentation, read-only)
3. Runs a main loop:

    * `BeginFrame(dt)` once per frame (variable step)
    * `FixedStep(fixedDelta)` zero or more times per frame (accumulator, 60 Hz)
    * `LateFrame(alpha)` for rendering/binding; `alpha` comes from the accumulator
4. Exits cleanly when **any key** is pressed, then shuts down systems.

---

## File layout

```
Program.cs
```

Key parts (trimmed for clarity):

* **Components**

  ```csharp
  public readonly struct Position { public readonly float X, Y; /* … */ }
  public readonly struct Velocity { public readonly float X, Y; /* … */ }
  ```

* **Systems**

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
          Console.WriteLine($"-- Tick: {w.Tick} (alpha={alpha:0.00}) --");
          foreach (var e in w.Query<Position>())
          {
              var p = w.Read<Position>(e); // ✅ read-only
              Console.WriteLine($"Entity {e.Id,3}: pos={p}");
          }
      }
  }
  ```

* **Driver loop (excerpt)**

  ```csharp
  var runner = new SystemRunner(
      world,
      new List<ISystem> { new MoveSystem(), new PrintPositionsSystem() },
      new SystemRunnerOptions(),   // EndOfSimulation + guard writes in Presentation
      Console.WriteLine);

  const float fixedDelta = 1f / 60f;
  float accumulator = 0f;
  var sw = Stopwatch.StartNew();
  double prev = sw.Elapsed.TotalSeconds;

  while (!Console.KeyAvailable)
  {
      double now = sw.Elapsed.TotalSeconds;
      float dt = (float)(now - prev);
      prev = now;

      runner.BeginFrame(dt);              // variable step once

      accumulator += dt;                  // fixed step 0..N times
      int steps = 0;
      while (accumulator >= fixedDelta && steps < 4)
      {
          runner.FixedStep(fixedDelta);
          accumulator -= fixedDelta;
          steps++;
      }

      float alpha = fixedDelta > 0 ? Math.Clamp(accumulator / fixedDelta, 0f, 1f) : 1f;
      runner.LateFrame(alpha);            // read-only presentation

      Thread.Sleep(1);                    // be gentle to CPU
  }

  runner.ShutdownSystems();
  world.RunScheduledJobs();               // safety flush
  ```

---

## Prerequisites

* **.NET 8 SDK** (or newer)
* ZenECS Core sources in your solution (Core + Systems + Runner)

---

## Build & Run

From the repository root (adjust the path to your project):

```bash
dotnet restore
dotnet build --no-restore
dotnet run --project src/Samples/ZenEcsCore-01-Core-Console.csproj
```

> **IDE tip (Rider/VS):** Select **01 Core Console** as the startup project.

---

## Example output

```
=== ZenECS Core Console Sample ===
Running... press any key to exit.
-- Tick: 1 (alpha=0.33) --
Entity   1: pos=(0.02, 0)
Entity   2: pos=(2, 1.00)
-- Tick: 2 (alpha=0.45) --
Entity   1: pos=(0.04, 0)
Entity   2: pos=(2, 0.99)
...
Shutting down...
Done.
```

*(Entity IDs and exact numbers depend on timing on your machine.)*

---

## APIs showcased

* `World.CreateEntity()`, `World.Add(entity, component)`
* `World.Query<T1[,T2]>()`, `World.Read<T>()`, `World.Replace<T>(…, value)`
* `SystemRunner.BeginFrame(dt)`, `FixedStep(fixedDelta)`, `LateFrame(alpha)`
* `SystemRunnerOptions` with `EndOfSimulation` flush and Presentation write-guard
* Separation of concerns:

    * **Simulation**: writes/components changes (recorded, then flushed at barrier)
    * **Presentation**: **read-only** (query + read)

---

## Notes & best practices

* **Write at Simulation, Read at Presentation.** Presentation is guarded against writes.
* Use **fixed-timestep** for determinism (physics/AI), **variable step** for per-frame logic.
* If you don’t use interpolation, pass `alpha = 1f` so you display the latest state.

---

## License

MIT © 2025 Pippapips Limited
