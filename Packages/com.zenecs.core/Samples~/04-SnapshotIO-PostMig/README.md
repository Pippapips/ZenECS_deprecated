# ZenECS Core — 04 Snapshot IO + PostMigration

Shows binary snapshot save/load with runtime **StableId** registry and an `IPostLoadMigration`:

* Register components + formatter with `ComponentRegistry`
* `World.SaveFullSnapshotBinary(Stream)` / `LoadFullSnapshotBinary(Stream)`
* Convert `PositionV1` → `PositionV2` in `Run(World)`

---

## What this sample does

Saves a world containing `PositionV1` to a `MemoryStream`, loads into a fresh world, then runs a migration to replace `V1` with `V2(layer=1)`.

---

## Notes & best practices

* Keep `IPostLoadMigration` **idempotent** (safe to run multiple times).
* StableId strings should be versioned (e.g., `com.zenecs.samples.position.v2`).
* Ensure the same registry entries exist before loading snapshots.

---

## License

MIT © 2025 Pippapips Limited
