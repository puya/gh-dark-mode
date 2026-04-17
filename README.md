# GH Dark Mode

## What it does

**GH Dark Mode** adds a **dark theme** to **Grasshopper** in **Rhino 8** on **Windows and Mac**. Turn it on for a ready-made dark canvas and UI, or switch back to your **saved light look** anytime. Changes persist between sessions. Optional tools let you **customize colors** if you want finer control.

On the first run, the plugin stores a snapshot of your current skin as **`ghdarkmode_baseline_gui.xml`**. **Mode = false** restores that snapshot (your pre–dark-mode look). **Mode = true** applies a tuned dark theme and writes a set of default XML color overrides (wires, component fills/edges, etc.).

**GH Dark Mode Override** is a second component that picks individual **skin color keys** (from an embedded manifest) and outputs **tokens** you connect to the main component’s **OVR** input—so you can fine-tune colors without editing XML by hand.

---

## How to use

1. **Install** **`GHDarkMode.gha`**: download the latest **`GHDarkMode.gha`** from [**GitHub Releases**](https://github.com/puya/gh-dark-mode/releases) (or build from source below). Copy it into Grasshopper’s **Libraries** folder, or use the **Rhino Package Manager** if a Yak package is published. Restart Rhino/Grasshopper.
2. In Grasshopper, open **Params → Util** and place **GH Dark Mode**.
3. Wire **Mode (M)** with a boolean or button: **`true`** applies dark mode; **`false`** restores the baseline snapshot.
4. Optional: place **GH Dark Mode Override**, choose a **Target** key and **Color**, and connect its output into **Overrides (OVR)** on the main component (list input). Built-in dark defaults and your **OVR** tokens are merged; same key from **OVR** wins.
5. **Reset (R)** restores the baseline file if it exists, otherwise resets GUI XML toward factory defaults (restart Grasshopper to refresh everything).
6. **Invert (I)** is an optional debug action that inverts colors in `grasshopper_gui.xml`—use sparingly; use **Reset** or **Mode = false** to recover.

After a dark solve, **Out** shows status, how many XML color keys were written, and the full merged override list (use a **Panel** to read it).

You may need to **restart Grasshopper** or reopen the definition for the canvas to fully match the new skin.

**More detail:** [src/GHDarkMode/README.md](src/GHDarkMode/README.md)

---

## Screenshot

![Grasshopper dark mode with GH Dark Mode and GH Dark Mode Override on the canvas](docs/gh-darkmode-screenshot.jpg)

---

## Build from source

**Prerequisites:** Rhino 8, .NET 7 SDK (or later).

**macOS** — from repo root:

```bash
./scripts/build-and-install.sh
```

Builds **`dist/GHDarkMode.gha`** and copies it into Grasshopper **Libraries**. Artifact only: `./scripts/build.sh`.

**Windows** — install **.NET 7 SDK**, then from repo root:

```bat
dotnet build src\GHDarkMode\GHDarkMode.csproj -c Release
mkdir dist 2>nul
copy /Y build\GHDarkMode.dll dist\GHDarkMode.gha
```

Copy **`dist\GHDarkMode.gha`** (or **`build\GHDarkMode.dll`** renamed to **`.gha`**) into Grasshopper’s **Libraries** folder (Grasshopper: **File → Special Folders → Components Libraries Folder**).

**SDK / Rhino references:** [docs/SDK_VERSION_AND_COMPATIBILITY.md](docs/SDK_VERSION_AND_COMPATIBILITY.md)

### Publish to Yak + GitHub Releases (maintainers)

Builds are **local** (Rhino 8 / .NET on your machine). There is **one** script that runs **`yak-pack.sh`** → **`yak push`** → **`release-github.sh`**:

1. Bump **`<Version>`** in **`Directory.Build.props`**; commit and push **`main`** when you are ready.
2. **Once:** **`gh auth login`** and **`yak login`** (Rhino Package Manager).
3. From repo root:

   ```bash
   ./scripts/publish.sh
   ```

   Optional: **`./scripts/publish.sh v1.2.3`** to force the GitHub release tag (otherwise the tag comes from **`Directory.Build.props`** via **`read-version.sh`**).

That uploads the **`.yak`** to [yak.rhino3d.com](https://yak.rhino3d.com/) and creates or updates the **GitHub Release** with **`GHDarkMode.gha`**. If **`gh`** only created the tag on the remote, run **`git fetch origin --tags`**.

**Piecemeal:** use **`./scripts/yak-pack.sh`** and **`yak push`** (Yak only) or **`./scripts/release-github.sh`** (GitHub only). You can also attach **`dist/GHDarkMode.gha`** by hand on GitHub.

**Yak manifest:** **`packaging/manifest.yml`** uses **`version: $version`** (filled from the **`.gha`**). See [Creating a Grasshopper plug-in package](https://developer.rhino3d.com/guides/yak/creating-a-grasshopper-plugin-package/) and [Pushing a package](https://developer.rhino3d.com/guides/yak/pushing-a-package-to-the-server/).

---

## Repository layout

| Path | Purpose |
|------|---------|
| **src/GHDarkMode/** | Plugin source |
| **icons/** | 24×24 PNG icons (embedded in `.gha`) |
| **scripts/** | **`publish.sh`** (Yak + GitHub), `build.sh`, `read-version.sh`, `yak-pack.sh`, `release-github.sh`, `build-and-install.sh` |
| **packaging/manifest.yml** | Yak package metadata |
| **docs/** | Screenshot, SDK notes, **DEVELOPMENT.md** |
| **LICENSE** | GNU GPL v3.0 (verbatim from [gnu.org](https://www.gnu.org/licenses/gpl-3.0.txt)) |
| **REPLICATION_SPEC.md** | Historical spec for the skin / `GH_Skin` approach |

---

## License

Copyright © Puya Khalili. This project is open source under the **GNU General Public License v3.0** (standard FSF text: you may use **version 3** or **any later version** published by the FSF — see [LICENSE](LICENSE)). Source is in this repository; if you distribute a modified **`.gha`**, GPLv3 requires you to make corresponding source available under the same license.

**Rhino / Grasshopper** remain **McNeel**’s proprietary software; this license applies to **GH Dark Mode**’s own code and assets in this repo, not to the host application.
