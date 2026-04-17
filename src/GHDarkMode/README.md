# GH Dark Mode

Grasshopper 1 plugin (Mac): dark/light GUI via `GH_Skin` and `grasshopper_gui.xml`.

## Build and install

From repo root:

```bash
./scripts/build-and-install.sh
```

Artifact only (no install): `./scripts/build.sh` → `dist/GHDarkMode.gha`. Restart Rhino/Grasshopper after install.

**SDK:** Builds against the Grasshopper SDK from your installed Rhino 8 app (default `/Applications/Rhino 8.app`). See [docs/SDK_VERSION_AND_COMPATIBILITY.md](../../docs/SDK_VERSION_AND_COMPATIBILITY.md).

## Components (Params → Util)

### GH Dark Mode

- **M** — `true` = apply dark theme; `false` = restore **baseline** skin (snapshot from first run).
- **R** — restore baseline if present, else factory reset `grasshopper_gui.xml`.
- **Invert** — debug: invert all colors in the GUI XML (use sparingly; restore with **R** or **M=false**).
- **Debug** — debug: assign high-contrast test colors (background keys skipped) to map XML keys to UI.
- **Optional colors** — `BG`, `CF`, `CSF`, `CE`, `CNT`, `ST`, `CST`, `OF`, `OSF`, `WD`, `WSI`, `WSO`. Disconnect to use built-in defaults.
- **Overrides (OVR)** — list: connect outputs from **GH Dark Mode Override** for per-key XML color patches after dark mode applies.

**Out** — status plus summary of active optional overrides.

Settings live under the Grasshopper plugin folder, e.g. `grasshopper_gui.xml` and `ghdarkmode_baseline_gui.xml`.

### GH Dark Mode Override

Emits one override token from **Key** + **Color** (+ **Enable**). Connect **Override** into **GH Dark Mode**’s **OVR** input. **Key** can be a short alias (`BG`, `WD`, …) or a raw XML item name (e.g. `normal.std.text`).
