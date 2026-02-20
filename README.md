# Moonlight / GH Dark Mode

Grasshopper 1 plugin for **Mac** that toggles the Grasshopper GUI between **Dark Mode** and **Light Mode** (same idea as the Windows plugin [Moonlight](https://food4rhino.com/en/app/moonlight) by ekimroyrp). This repo contains both reference material for the original Windows plugin and the Mac reimplementation.

---

## What’s in this repo

| Item | Description |
|------|-------------|
| **src/GHDarkMode/** | Grasshopper 1 (Mac) plugin: **GH Dark Mode**. One component under Params → Util; build and install via `./scripts/build-and-install.sh`. |
| **scripts/build-and-install.sh** | Builds the plugin and copies `GHDarkMode.gha` into your Grasshopper Libraries folder. |
| **docs/** | Notes on .gha vs .dll, SDK version compatibility, and replication spec. |
| **REPLICATION_SPEC.md** | Spec for reimplementing Moonlight on Mac (GH_Skin API, dark/light logic). |
| **moonlight1-0.gha** | Original Windows Moonlight plugin (reference only; does not run on Mac). |

---

## Quick start (GH Dark Mode on Mac)

1. **Prerequisites:** Rhino 8 for Mac, .NET 7 SDK (or later), Grasshopper 1.
2. **Build and install:**
   ```bash
   ./scripts/build-and-install.sh
   ```
3. **Restart Rhino and Grasshopper.** Find **Params → Util → GH Dark Mode**.
4. **Use:** Connect a Boolean or Button to input **M**. Set **M** true to run. Route output **Out** to a Panel to see the full status message.

The plugin is currently a **probe**: it verifies that the GH_Skin API works on your Mac (LoadSkin, SaveSkin, canvas_back). Canvas colors do not change until we implement the actual dark/light theme in a later step.

---

## Project status

- [x] Scaffold and probe component (GH_Skin test).
- [x] Build and install script; install as `.gha` for Grasshopper to load.
- [x] Build against your Rhino 8 app SDK for compatibility (e.g. 8.15).
- [x] Optional **Out** parameter for routing status to a Panel.
- [ ] Implement dark and light theme (set GH_Skin fields and call SaveSkin).

See **REPLICATION_SPEC.md** and **docs/SDK_VERSION_AND_COMPATIBILITY.md** for details.

---

## License

The original Moonlight plugin is proprietary (free). This Mac reimplementation (GH Dark Mode) is provided for use with Rhino/Grasshopper on Mac.
