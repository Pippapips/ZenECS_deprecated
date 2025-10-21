# ZenECS Core — 02 Messages Bus

A tiny **console** sample that demonstrates `IMessageBus` with frame delivery via `PumpAll()`:

* Define a `DamageTaken : IMessage`
* `Subscribe<T>` two handlers (UI/Logic)
* `Publish()` several messages and deliver them in batches with `PumpAll()`
* Safe to publish while handling — next `PumpAll()` will deliver

---

## What this sample does

Publishes three `DamageTaken` messages, pumps them, then publishes another and pumps again.

---

## Notes & best practices

* Use one central bus per world/game loop.
* Call `PumpAll()` once per frame (e.g., at frame setup or end-of-frame).
* Prefer small, immutable structs as messages.

---

## License

MIT © 2025 Pippapips Limited
