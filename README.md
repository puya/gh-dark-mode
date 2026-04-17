# Moonlight / GH Dark Mode

Grasshopper 1 plugin for **Mac** that toggles the Grasshopper GUI between **Dark Mode** and **Light Mode** (same idea as the Windows plugin [Moonlight](https://food4rhino.com/en/app/moonlight) by ekimroyrp). This repo contains both reference material for the original Windows plugin and the Mac reimplementation.

---

## What’s in this repo

| Item | Description |
|------|-------------|
| **src/GHDarkMode/** | Grasshopper 1 (Mac) plugin: **GH Dark Mode** + **GH Dark Mode Override**; build via `./scripts/build-and-install.sh`. |
| **scripts/build-and-install.sh** | Build and copy `dist/GHDarkMode.gha` into your Grasshopper Libraries folder. |
| **scripts/build.sh** | Build only; writes `dist/GHDarkMode.gha` plus `manifest.yml` and Package Manager icon for Yak. |
| **scripts/yak-pack.sh** | Runs `build.sh` then Rhino’s **yak build** in `dist/` → `*.yak` for the Package Manager. |
| **packaging/manifest.yml** | Yak package manifest (**version** must match `GHDarkModeInfo.Version`); edit **url** before publish. |
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
4. **Use:** Wire **M** (bool). **M = true** applies dark mode; **M = false** restores the **baseline** snapshot from the first run. Optional **OVR** (from **GH Dark Mode Override**) patches XML colors on top of built-in dark defaults. **Out** reports status, write counts, and the merged override list after dark apply.

Persistence uses `grasshopper_gui.xml` in the Grasshopper plugin data folder; the plugin also keeps `ghdarkmode_baseline_gui.xml` for restore. You may need to **restart Grasshopper** (or reopen the definition) for the canvas to fully refresh.

For component-level details see **[src/GHDarkMode/README.md](src/GHDarkMode/README.md)**.

### Yak / Rhino Package Manager

1. Bump **`version`** in **`packaging/manifest.yml`** and **`GHDarkModeInfo.Version`** together for each release.
2. Set **`url`** in the manifest to your public GitHub or Food4Rhino page.
3. From repo root: **`./scripts/yak-pack.sh`** (optional: `--platform mac` or `--platform win`). Requires the **Yak** CLI shipped with Rhino 8 (override with **`YAK=/path/to/yak`**).
4. Publish: McNeel’s guide [Pushing a package to the server](https://developer.rhino3d.com/guides/yak/pushing-a-package-to-the-server/). Overview: [Creating a Grasshopper plug-in package](https://developer.rhino3d.com/guides/yak/creating-a-grasshopper-plugin-package/).

The **`dist/`** folder is gitignored; **`build.sh`** populates it with everything needed for **`yak build`**.

---

## Project status

- [x] Dark/light via GH_Skin + baseline restore.
- [x] Modular **GH Dark Mode Override** + **OVR** list (built-in dark XML defaults + optional patches).
- [x] Debug: **Invert** XML color transform (assign-distinct-colors debug input removed from UI for now).
- [x] Build outputs under `build/`, distributable `dist/GHDarkMode.gha`, Yak **`packaging/manifest.yml`** + **`yak-pack.sh`**.

See **REPLICATION_SPEC.md** and **docs/SDK_VERSION_AND_COMPATIBILITY.md** for details.

---

## License

The original Moonlight plugin is proprietary (free). This Mac reimplementation (GH Dark Mode) is provided for use with Rhino/Grasshopper on Mac.
