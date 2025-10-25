# ZenECS.Core.Tests (Aligned to Core Runtime)

A focused test suite validating **ZenECS Core** behavior using the TestFramework.

## Coverage
- **World**: `Add/Read/Replace/Remove`, `Has`, `TryRead`, `GetOrAdd`
- **Entities**: `CreateEntity`, `DestroyEntity`, `IsAlive`, id reuse + generation bump
- **MessageBus**: `Publish` then `PumpAll()` deferred delivery
- **Systems**: group/ordering attributes with a simple runner integration

## Run
```bash
dotnet test
```

### Notes
- These tests target the **Core runtime APIs**. Adjust when Core APIs change.
- Snapshot tests belong in a **separate project** (e.g., `ZenECS.Core.Tests.Snapshots`) since they depend on sample-level helpers.
