# Moonlight / GH Dark Mode

Grasshopper 1 plugin for **Mac** that toggles the Grasshopper GUI between **Dark Mode** and **Light Mode** (same idea as the Windows plugin [Moonlight](https://food4rhino.com/en/app/moonlight) by ekimroyrp). This repo contains both reference material for the original Windows plugin and the Mac reimplementation.

---

## What’s in this repo

| Item | Description |
|------|-------------|
| **src/GHDarkMode/** | Grasshopper 1 (Mac) plugin: **GH Dark Mode** + **GH Dark Mode Override**; build via `./scripts/build-and-install.sh`. |
| **scripts/build-and-install.sh** | Build and copy `dist/GHDarkMode.gha` into your Grasshopper Libraries folder. |
| **scripts/build.sh** | Build only; writes `dist/GHDarkMode.gha`. |
| **docs/** | Development notes, SDK compatibility, task specs (e.g. future skin presets). |
| **REPLICATION_SPEC.md** | Spec for reimplementing Moonlight on Mac (GH_Skin API, dark/light logic). |
| **moonlight1-0.gha** | Original Windows Moonlight plugin (reference only; does not run on Mac). |

---

## Quick start (GH Dark Mode on Mac)

1. **Prerequisites:** Rhino 8 for Mac, .NET 7 SDK (or later), Grasshopper 1.
2. **Build and install:**
   ```bash
   ./scripts/build-and-install.sh
   ```
3. **Restart Rhino and Grasshopper.** In **Params → Util** you’ll find **GH Dark Mode** and **GH Dark Mode Override**.
4. **Use:** Wire **M** (bool). **M = true** applies dark mode; **M = false** restores the **baseline** snapshot from the first run. Optional color inputs and **OVR** (from override components) tune the theme. **Out** summarizes overrides.

Persistence uses `grasshopper_gui.xml` in the Grasshopper plugin data folder; the plugin also keeps `ghdarkmode_baseline_gui.xml` for restore. You may need to **restart Grasshopper** (or reopen the definition) for the canvas to fully refresh.

For component-level details see **[src/GHDarkMode/README.md](src/GHDarkMode/README.md)**.

---

## Project status

- [x] Dark/light via GH_Skin + baseline restore.
- [x] Optional color inputs, modular **GH Dark Mode Override** + **OVR** list.
- [x] Debug: **Invert** / **Debug** XML color transforms for discovery.
- [x] Build outputs under `build/`, distributable `dist/GHDarkMode.gha`.

See **REPLICATION_SPEC.md** and **docs/SDK_VERSION_AND_COMPATIBILITY.md** for details.

---

## License

The original Moonlight plugin is proprietary (free). This Mac reimplementation (GH Dark Mode) is provided for use with Rhino/Grasshopper on Mac.
