# ZenECS Core — 08 System Runner

End‑to‑end SystemRunner pipeline:

* Register `Simulation` and `Presentation` systems
* Choose structural flush & write‑guard options
* Run fixed/variable simulation and read‑only presentation

---

## What this sample does

Moves entities in simulation (`MoveSystem`) and prints positions in presentation (`PrintSystem`) using a tiny realtime loop.

---

## Notes & best practices

* Use `GuardWritesInPresentation` to enforce read‑only presentation.
* Mix fixed and variable steps as needed; keep presentation side‑effect‑free.

---

## License

MIT © 2025 Pippapips Limited
