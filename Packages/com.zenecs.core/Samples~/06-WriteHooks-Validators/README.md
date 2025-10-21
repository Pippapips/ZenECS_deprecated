# ZenECS Core — 06 Write Hooks & Validators

Demonstrates per‑world write permissions and value validators:

* `world.AddWritePermission((w,e,t)=>...)`
* `world.AddValidator<T>(predicate)`
* Guards **Add/Replace/Remove**

---

## What this sample does

Installs a validator (`Mana >= 0`) and a permission rule (only even entity IDs). Attempts various writes and prints outcomes.

---

## Notes & best practices

* Keep validators **pure** and fast; avoid allocations.
* Prefer per‑world hooks; global hooks (`EcsActions.*`) affect every world.

---

## License

MIT © 2025 Pippapips Limited
