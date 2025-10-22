# ZenECS.Binding.ConsoleSample

Minimal console demo wiring **Binding** pipeline without Unity:

- `DefaultMainThreadGate` (Core-only)
- `ComponentChangeFeed` (posts via gate)
- `ComponentBindingHubSystem` → `ComponentBatchDispatchSystem` → `ViewBindingSystem`
- `IComponentBinder<T>` fast path binder
- `RequestReconcile(...)` projection pull

## Build
Add a reference to your ZenECS Core project/DLL so `World`, `Entity`, and the systems resolve.

Then:
```bash
dotnet run
```
