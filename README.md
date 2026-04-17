# Moonlight / GH Dark Mode

Grasshopper 1 plugin for **Rhino 8** (primarily documented for **Mac**) that toggles the Grasshopper GUI between **dark** and **light** themes—same idea as the Windows plugin [Moonlight](https://food4rhino.com/en/app/moonlight) by ekimroyrp. This repo holds the Mac-oriented reimplementation plus reference material for the original Windows `.gha`.

## Screenshot

![Grasshopper dark mode with GH Dark Mode and GH Dark Mode Override on the canvas](docs/gh-darkmode-screenshot.jpg)

Dark theme on the canvas using **GH Dark Mode** (**Params → Util**). Optional **GH Dark Mode Override** components feed the **OVR** input for extra XML color keys.

---

## What’s in this repo

| Item | Description |
|------|-------------|
| **src/GHDarkMode/** | Plugin source: **GH Dark Mode** + **GH Dark Mode Override**; build via `./scripts/build-and-install.sh`. |
| **icons/** | 24×24 PNG icons with alpha, embedded in the `.gha`. |
| **scripts/build-and-install.sh** | Release build and copy `dist/GHDarkMode.gha` into Grasshopper **Libraries**. |
| **scripts/build.sh** | Build only; fills **`dist/`** with `.gha`, **`manifest.yml`**, and Package Manager icon for Yak. |
| **scripts/yak-pack.sh** | Runs **`build.sh`** then **`yak build`** in **`dist/`** → **`*.yak`** for the Rhino Package Manager. |
| **packaging/manifest.yml** | Yak manifest (**`version`** must match **`GHDarkModeInfo.Version`**); set **`url`** before publish. |
| **docs/** | **`gh-darkmode-screenshot.jpg`** (above), **SDK_VERSION_AND_COMPATIBILITY.md**, **DEVELOPMENT.md**, task notes. |
| **REPLICATION_SPEC.md** | Spec for reimplementing Moonlight on Mac (`GH_Skin`, dark/light behavior). |
| **moonlight1-0.gha** | Original Windows Moonlight plugin (reference only; does not run on Mac). |

---

## Quick start (Mac)

1. **Prerequisites:** Rhino 8 for Mac, .NET 7 SDK (or later), Grasshopper 1.
2. **Build and install:**
   ```bash
   ./scripts/build-and-install.sh
   ```
3. **Restart Rhino and Grasshopper.** In **Params → Util** add **GH Dark Mode** and optionally **GH Dark Mode Override**.
4. **Inputs (main component):** **Mode (M)** — `true` = dark, `false` = restore **baseline**; **Overrides (OVR)** — optional list of tokens from override components; **Reset (R)** — baseline or factory reset; **Invert (I)** — optional debug invert of XML colors. After a dark solve, **Out** lists merged XML overrides and patch counts.

Persistence uses `grasshopper_gui.xml` in the Grasshopper plugin data folder; the plugin also keeps `ghdarkmode_baseline_gui.xml` for restore. You may need to **restart Grasshopper** (or reopen the definition) for the canvas to fully refresh.

**Details:** [src/GHDarkMode/README.md](src/GHDarkMode/README.md) · **SDK paths:** [docs/SDK_VERSION_AND_COMPATIBILITY.md](docs/SDK_VERSION_AND_COMPATIBILITY.md)

### Yak / Rhino Package Manager

1. Bump **`version`** in **`packaging/manifest.yml`** and **`GHDarkModeInfo.Version`** together for each release.
2. Set **`url`** in the manifest to your public GitHub or Food4Rhino page.
3. From repo root: **`./scripts/yak-pack.sh`** (optional: **`--platform mac`** or **`--platform win`**). Override the CLI with **`YAK=/path/to/yak`** if needed.
4. Authenticate once: **`yak login`**, then publish per [Pushing a package to the server](https://developer.rhino3d.com/guides/yak/pushing-a-package-to-the-server/). Overview: [Creating a Grasshopper plug-in package](https://developer.rhino3d.com/guides/yak/creating-a-grasshopper-plugin-package/).

The **`dist/`** folder is gitignored; **`build.sh`** populates it with everything needed for **`yak build`**.

---

## Project status

- [x] Dark/light via `GH_Skin` + baseline restore.
- [x] **GH Dark Mode Override** + **OVR** list (built-in dark XML defaults + optional patches).
- [x] **Invert** XML transform on `grasshopper_gui.xml` (distinct-color debug input removed from UI).
- [x] **build/** outputs, **dist/** `.gha`, Yak **`packaging/manifest.yml`** + **`yak-pack.sh`**.
- [x] 24×24 PNG component icons with alpha.

See **REPLICATION_SPEC.md** and **docs/DEVELOPMENT.md** for implementation notes.

---

## License

The original Moonlight plugin is proprietary (free). This reimplementation (GH Dark Mode) is provided for use with Rhino and Grasshopper.
