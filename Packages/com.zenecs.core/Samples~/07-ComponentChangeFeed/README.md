# ZenECS Core — 07 Component Change Feed

Aggregates component **Add/Changed/Removed** events into end‑of‑frame batches suitable for view updates:

* `ComponentBindingHubSystem` gathers changes
* `IComponentChangeFeed.SubscribeRaw` receives a compact batch

---

## What this sample does

Creates a world, instantiates the hub + feed, performs several component changes, and runs one presentation tick to print a single aggregated batch.

---

## Notes & best practices

* Batch at presentation to avoid per‑change view churn.
* Keep view logic read‑only; let systems perform writes during simulation.

---

## License

MIT © 2025 Pippapips Limited
