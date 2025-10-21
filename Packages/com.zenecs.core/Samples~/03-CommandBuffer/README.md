# ZenECS Core — 03 Command Buffer

Demonstrates `World.BeginWrite()` → `Schedule(cb)` / `RunScheduledJobs()` vs `EndWrite(cb)` immediate apply:

* `Add/Replace/Remove` queued as ops in a thread‑safe buffer
* Choose **deferred** or **immediate** apply
* Structural changes are safe across frames

---

## What this sample does

Creates two entities, stages several ops, schedules them, applies at `RunScheduledJobs()`, then uses `EndWrite(cb)` to apply immediately.

---

## Notes & best practices

* In systems, prefer **deferred** structural changes; flush at controlled boundaries.
* Keep write scopes short‑lived.
* For hot paths, batch ops into one buffer.

---

## License

MIT © 2025 Pippapips Limited
