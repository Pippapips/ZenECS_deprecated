# ZenECS Core — 05 World Reset

Compares two world reset strategies:

* `ResetButKeepCapacity()` — clears **data only**, keeps arrays/pools sized (fast)
* `HardReset()` — rebuilds internal structures using `WorldConfig` (cold start)

---

## What this sample does

Creates a world, adds data, performs `ResetButKeepCapacity()`, reseeds, then does a full `HardReset()` and prints counts.

---

## Notes & best practices

* Prefer **keep-capacity** between scenes/tools when structure is unchanged.
* Use **hard reset** for cold boots or when initial capacities must be restored.

---

## License

MIT © 2025 Pippapips Limited
