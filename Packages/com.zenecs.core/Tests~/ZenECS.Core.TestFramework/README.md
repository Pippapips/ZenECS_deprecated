# ZenECS.Core.TestFramework (Aligned)

A lightweight **helper library** to write tests against **ZenECS Core** only (no Hosting dependency).

## What it provides
- **TestWorldHost**: creates a `World` + `IMessageBus` and wraps your system runner to step frames deterministically.
- **MessageBusSpy** (and small asserts): subscribe and count deliveries for `Publish`/`PumpAll()` flows.

## Usage
1. Reference this project from your test project(s).
2. Create `TestWorldHost`, register systems, call `TickFrame()`.
3. Use the spy helpers to assert bus traffic.

> Keep the surface small so the framework stays stable as Core evolves.
