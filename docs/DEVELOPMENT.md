# GH Dark Mode — Development Document

This document records the approach, findings, progress, roadmap, API details, build/packaging, and references for the **GH Dark Mode** Grasshopper 1 plugin (**Rhino 8**, **Windows and Mac**). It is the single source of truth for development implementation.

---

## 1. Project overview

- **Name:** GH Dark Mode (repository: GHDarkMode)
- **Platform:** Rhino 8 (**Windows and macOS**), Grasshopper 1 (not Grasshopper 2)
- **Purpose:** Toggle Grasshopper’s GUI between **dark** and **light** themes via the **GH_Skin** API and **grasshopper_gui.xml** (same persistence model as the stock Grasshopper skin system).
- **Status:** Dark and light themes implemented (apply via GH_Skin and SaveSkin). Some aspects (e.g. grid lines) may not fully come through; see §3.3 for planned improvements (save user theme, grid/layout in dark, skin system).
- **License:** Open source under **GNU GPL v3.0** — full text in repository root **LICENSE** (verbatim from gnu.org). SPDX: **GPL-3.0-or-later** (standard license terms). **Rhino/Grasshopper** remain proprietary; the GPL applies to this plugin’s sources and your distribution of builds derived from them.

---

## 2. Approach and findings

### 2.1 Prior art

- Other dark-mode plugins for Grasshopper have used the same public surface: **GH_Skin** and **`grasshopper_gui.xml`**. This project is an independent implementation for **Rhino 8** on **Windows and Mac** (single **net7.0** `.gha`).

### 2.2 Why .gha and not .dll

- Grasshopper **only discovers add-ons with the `.gha` extension** in the Libraries folder.
- A `.gha` file is the **same .NET assembly** as a `.dll`; we build `GHDarkMode.dll` and **copy it as `GHDarkMode.gha`** on install. No extra packaging step.

### 2.3 SDK version compatibility

- Grasshopper loads a .gha only if it was **built against the same or an older minor SDK version** than the one running (e.g. Local SDK 8.15 rejects Referenced SDK 8.28).
- **Solution:** Build against the **assemblies shipped with your Rhino 8 app** when possible. The `.csproj` default **`RhinoRefPath`** is **macOS-shaped** (`Rhino 8.app/.../ref/net48`). **Windows** developers typically get the **NuGet Grasshopper** reference instead (no Mac path on disk), unless they extend the project to set **`RhinoRefPath`** to their Windows **ref/net48** folder—see **SDK_VERSION_AND_COMPATIBILITY.md**.
- **Fallback:** If that path does not exist (e.g. CI), the project uses NuGet **Grasshopper 8.26.25349.19001**; that build runs only on Rhino 8.26+.

### 2.4 Probe-first strategy

- Early development used a **probe** that calls `GH_Skin.LoadSkin()`, reads `GH_Skin.canvas_back`, and calls `GH_Skin.SaveSkin()` to confirm the API exists on the target OS. No colors are changed in the probe; the **Out** parameter and component message report success or failure.

---

## 3. Roadmap and progress

### 3.1 Completed

| Item | Notes |
|------|------|
| Project scaffold | C# Grasshopper 1 project, namespace `GHDarkMode`, assembly name `GHDarkMode`. |
| Single component | **GHDarkModeComponent**: Params → Util, input **M** (bool), output **Out** (text). |
| Assembly info | **GHDarkModeInfo** (subclass of `GH_AssemblyInfo`), empty constructor, Name/Version/Description. |
| Probe logic | `LoadSkin()` → read `canvas_back` → `SaveSkin()`; report via Message, Remark, and **Out**. |
| Dark theme | `ApplyDarkTheme()` sets all GH_Skin fields (canvas, wires, panel/group, palettes as GH_PaletteStyle(Fill,Edge,Text), ZUI) to VS/Adobe-style dark; then `SaveSkin()`. |
| Light theme | `ApplyLightTheme()` sets all GH_Skin fields to Grasshopper-style default light (canvas 212,208,199; palettes with light fills and dark text); then `SaveSkin()`. |
| Mode behavior | **M** = true → apply dark; **M** = false → restore baseline (pre-dark) skin. Theme is applied and saved on each solve; restart Grasshopper (or reopen definition) to see canvas update. |
| Build script | `scripts/build-and-install.sh`: `dotnet build -c Release`, copy `bin/GHDarkMode.dll` → Libraries as `GHDarkMode.gha`. |
| SDK compatibility | Reference Rhino app `ref/net48` (Grasshopper, GH_IO, RhinoCommon); fallback NuGet 8.26. |
| Optional output | **Out** (text) for routing full status to a Panel. |
| Repo setup | .gitignore, README, public GitHub repo (GHDarkMode), **LICENSE** (GPL-3.0). |

### 3.2 Remaining

| Item | Notes |
|------|------|
| Icons | 24×24 PNG + alpha embedded in `.gha` (see `icons/`). |
| Packaging (Yak) | **`packaging/manifest.yml`** + **`scripts/yak-pack.sh`**; see §7. |
| **Save user theme before dark** | Implemented in baseline snapshot form: on first run, the plugin copies `grasshopper_gui.xml` to `ghdarkmode_baseline_gui.xml` and uses it as the restore target when switching back to “light”. |
| **Dark mode: grid and layout** | See §3.3: dark mode should set grid spacing and other non-color settings so they are visible/consistent. |
| **Skin system** | See §3.3: move from hardcoded values to a skin system (config/serialized themes). |

### 3.3 Planned approach (not yet implemented)

The following behaviour is desired and should be implemented in a future iteration. No code changes are made for these in the current release; this section records the design.

#### 3.3.1 Save user’s setup first; “light” = restore their parameters

- This is now implemented using a baseline snapshot file:
  1. **On first run:** Ensure `grasshopper_gui.xml` exists (create it from defaults if needed), then copy it to `ghdarkmode_baseline_gui.xml`.
  2. **When reverting to “light”:** Copy `ghdarkmode_baseline_gui.xml` back onto `grasshopper_gui.xml`, then `LoadSkin()` and `SaveSkin()`.
- **Edge cases to consider:** First run (no saved state yet): either treat “light” as no-op / LoadSkin from current `grasshopper_gui.xml`, or save current state on first switch to dark. If the user has never switched to dark, “light” can simply leave the current skin as-is or reload from `grasshopper_gui.xml`.

#### 3.3.2 Dark mode must include grid spacing and other settings

- **Current state:** Dark mode sets colours explicitly, and also keeps grid spacing consistent by reading `canvas_grid_col` and `canvas_grid_row` from `ghdarkmode_baseline_gui.xml` and applying them.
- **Desired behaviour:** When switching to dark mode, set **all** relevant `GH_Skin` fields that affect the canvas and UI, including:
  - **Grid:** `canvas_grid`, `canvas_grid_col`, `canvas_grid_row` (and any other grid-related fields in the API).
  - **Canvas options:** `canvas_mono`, `canvas_mono_color`, `canvas_shade`, `canvas_shade_size`, etc.
- When saving the user’s theme (§3.3.1), persist these same fields so that restoring “light” also restores their grid spacing and other layout/preference values.

#### 3.3.3 Skin system instead of hardcoded values

- **Current state:** Dark and light themes are implemented by setting `GH_Skin` fields directly in C# with literal colours and values (e.g. `Color.FromArgb(...)`, fixed integers for grid).
- **Desired state:** Introduce a **skin system** so that themes are data-driven, not hardcoded:
  - **Definition of themes:** Store each theme (e.g. “dark”, and the saved user theme) as structured data: e.g. a config file (JSON/XML), a small DSL, or a serialisable skin model that lists every `GH_Skin` field (colors, palette Fill/Edge/Text, grid col/row, canvas_mono, shade size, etc.).
  - **Runtime:** Code loads the active theme from that structure and applies it to `GH_Skin` (e.g. “apply skin from file” or “apply skin from in-memory model”), then calls `SaveSkin()` when persisting. No large blocks of literal `Color.FromArgb(...)` or magic numbers in the component.
  - **Benefits:** Easier to add new themes, tweak colours/spacing without recompiling, and keep “user’s saved theme” in the same format as built‑in themes (e.g. one file per theme, or one file with named entries).

---

## 4. API reference — exact names and types

Use these exact identifiers when implementing dark/light logic.

### 4.1 Namespace and class

- **Namespace:** `Grasshopper.GUI.Canvas`
- **Class:** `GH_Skin` (sealed; all members static)
- **Assembly:** Grasshopper.dll

```csharp
using Grasshopper.GUI.Canvas;
// Then: GH_Skin.LoadSkin(); GH_Skin.SaveSkin(); GH_Skin.canvas_back; etc.
```

### 4.2 Methods (static, no parameters)

| Method | Signature | Description |
|--------|-----------|-------------|
| **LoadSkin** | `public static void LoadSkin()` | Reads colour values from `grasshopper_gui.xml` into the static GH_Skin fields. |
| **SaveSkin** | `public static void SaveSkin()` | Writes current static GH_Skin field values to `grasshopper_gui.xml`. |

### 4.3 Fields (static; types)

- Colour fields: **`System.Drawing.Color`**
- Grid/options: **int** or **bool** as per API (e.g. `canvas_mono` is bool).

**Canvas**

| Field | Type | Description |
|-------|------|-------------|
| `canvas_back` | Color | Canvas background colour. |
| `canvas_edge` | Color | Canvas edge colour. |
| `canvas_grid` | Color | Canvas grid colour. |
| `canvas_grid_col` | int | Interval of canvas grid columns. |
| `canvas_grid_row` | int | Interval of canvas grid rows. |
| `canvas_mono` | bool | If true, canvas background is a solid colour. |
| `canvas_mono_color` | Color | Solid background when canvas_mono is true. |
| `canvas_shade` | Color | Canvas drop shadow colour. |
| `canvas_shade_size` | (numeric) | Size of canvas drop shadow. |

**Wires**

| Field | Type |
|-------|------|
| `wire_default` | Color |
| `wire_empty` | Color |
| `wire_selected_a` | Color |
| `wire_selected_b` | Color |

**Panel / group**

| Field | Type |
|-------|------|
| `panel_back` | Color |
| `group_back` | Color |

**Palettes** (each has `*_selected` and `*_standard`)

- Type is **`GH_PaletteStyle`**, not Color. Constructor: **`new GH_PaletteStyle(Fill, Edge, Text)`** (all `System.Drawing.Color`). Properties: `Fill`, `Edge`, `Text`.
- Fields: `palette_normal_*`, `palette_black_*`, `palette_blue_*`, `palette_brown_*`, `palette_error_*`, `palette_grey_*`, `palette_hidden_*`, `palette_locked_*`, `palette_pink_*`, `palette_trans_*`, `palette_warning_*`, `palette_white_*`.

**ZUI**

| Field | Type |
|-------|------|
| `zui_edge` | Color |
| `zui_edge_highlight` | Color |
| `zui_fill` | Color |
| `zui_fill_highlight` | Color |

Full list: [GH_Skin Class](https://developer.rhino3d.com/api/grasshopper/html/T_Grasshopper_GUI_Canvas_GH_Skin.htm).

### 4.4 Component and data access

- **GetData:** Use **`ref`** for the out parameter with this SDK (e.g. `bool run = false; da.GetData(0, ref run);`). `out` can cause CS1620 on some SDK versions.
- **SetData:** `da.SetData(0, stringValue)` for the first (and currently only) output parameter.

---

## 5. Build and install (current)

### 5.0 Build outputs vs distributables

- **`build/`**: build outputs (compiled assemblies + intermediate files). This replaces the default `src/**/bin` and `src/**/obj`.
- **`dist/`**: distributable artifacts you share/install (currently: `dist/GHDarkMode.gha`).

### 5.1 Prerequisites

- .NET 7 SDK (or later); Rhino 8 with Grasshopper 1 (**Windows or Mac**).
- Rhino 8 app at default path `/Applications/Rhino 8.app` (or set `RhinoAppPath`).

### 5.2 Commands

```bash
# From repo root: build and install to Grasshopper Libraries
./scripts/build-and-install.sh
```

If you only want a distributable build artifact (no install), run:

```bash
./scripts/build.sh
```

The script:

1. Finds `dotnet` (PATH or `/usr/local/share/dotnet`, `/opt/homebrew/share/dotnet`).
2. Runs `dotnet build -c Release` in `src/GHDarkMode`.
3. Creates a distributable artifact at `dist/GHDarkMode.gha`.
4. Copies `dist/GHDarkMode.gha` to the Libraries folder as **`GHDarkMode.gha`**.
5. Removes any `GHDarkMode.dll` from Libraries.

**Libraries path (auto-detected):**

```
~/Library/Application Support/McNeel/Rhinoceros/8.0/Plug-ins/Grasshopper*/Libraries
```

**Override Libraries path (if auto-detect fails or you have multiple installs):**

```bash
GH_LIBRARIES="/path/to/Libraries" ./scripts/build-and-install.sh
```

### 5.3 Custom Rhino path

```bash
cd src/GHDarkMode
dotnet build -c Release -p:RhinoAppPath="/path/to/Rhino 8.app"
```

Then copy `bin/GHDarkMode.dll` to Libraries as `GHDarkMode.gha` manually or by adjusting the script.

### 5.4 Project layout (relevant)

| Path | Purpose |
|------|---------|
| `src/GHDarkMode/GHDarkMode.csproj` | Project file; RhinoRefPath, References, fallback PackageReference. |
| `src/GHDarkMode/GHDarkModeInfo.cs` | Assembly metadata (GH_AssemblyInfo). |
| `src/GHDarkMode/GHDarkModeComponent.cs` | Single component: M (input), Out (output), SolveInstance (probe or future theme logic). |
| `src/GHDarkMode.sln` | Solution (optional). |
| `dist/GHDarkMode.gha` | Built distributable (output of `scripts/build.sh` / `scripts/build-and-install.sh`). **`dist/`** is gitignored. |
| `packaging/manifest.yml` | Yak manifest (canonical copy in repo); **`version: $version`** filled by **`yak build`** from the **`.gha`**. |
| `Directory.Build.props` | Repo root MSBuild props; **`<Version>`** is the single release semver for the plugin and Yak. |
| `scripts/build.sh` | Build; populate **`dist/`** with `.gha`, **`manifest.yml`**, and **`gh-darkmode-main-a.png`** for Yak. |
| `scripts/read-version.sh` | Prints **`<Version>`** via **`dotnet msbuild -getProperty:Version`** (no compile). |
| `scripts/yak-pack.sh` | Runs **`build.sh`**, then **`yak build`** in **`dist/`** → **`*.yak`**. |
| `scripts/release-github.sh` | Local **`build.sh`**, then **`gh release create`** / upload **`GHDarkMode.gha`**. |
| `scripts/publish.sh` | **`yak-pack.sh`** → **`yak push`** → **`release-github.sh`** (single maintainer entry point). |
| `scripts/build-and-install.sh` | Build + install to Libraries (also refreshes **`dist/`** Yak files). |

---

## 6. GitHub Releases (binary download)

**GitHub Actions** are **not** used to compile this plugin: hosted **Ubuntu** builds failed (e.g. `Microsoft.WindowsDesktop.App.WindowsForms` / Grasshopper package graph). Release **`.gha`** files are built on **Windows or Mac** with a normal Rhino 8 / .NET environment.

Publish **`GHDarkMode.gha`** with **`./scripts/release-github.sh`** (optional **`vX.Y.Z`**; default tag comes from **`Directory.Build.props`**) — see root **README.md** — or build locally and attach the file on the Releases page.

---

## 7. Packaging and distribution (Yak)

For the Rhino **Package Manager**, packages are built with the **Yak** CLI and described by **`manifest.yml`**.

### 7.1 Source vs build output

- **In repo (tracked):** **`packaging/manifest.yml`** — edit **`url`**, authors, description, keywords; keep **`version: $version`** so Yak infers semver from the assembly ([package manifest](https://developer.rhino3d.com/guides/yak/the-package-manifest/)).
- **Version source of truth:** **`Directory.Build.props`** **`<Version>`** only. **`GHDarkModeInfo.Version`** reads the built assembly’s informational version; **`scripts/read-version.sh`** prints the same value without building.
- **After `./scripts/build.sh` or `./scripts/build-and-install.sh`:** **`dist/`** (gitignored) contains everything Yak needs at the **top level**:
  - **`GHDarkMode.gha`**
  - **`manifest.yml`** (copy of **`packaging/manifest.yml`**)
  - **`gh-darkmode-main-a.png`** (Package Manager icon; name must match **`icon:`** in the manifest)

Optional extras (e.g. **`misc/LICENSE.txt`**) can be added under **`dist/`** before **`yak build`** if you want them inside the package.

### 7.2 Manifest reference

- Required: **`name`**, **`version`** (literal semver or **`$version`** inferred from plugin contents), **`authors`**, **`description`** ([Package manifest](https://developer.rhino3d.com/guides/yak/the-package-manifest/)).
- Recommended: **`url`** (set to your GitHub or Food4Rhino page before publish), **`icon`**, **`keywords`**.
- **`yak build`** may append a **`guid:`** keyword for package restore.

### 7.3 Build the `.yak` file

From repo root:

```bash
./scripts/yak-pack.sh
```

This runs **`scripts/build.sh`**, then **`yak build`** inside **`dist/`**. Override the Yak executable if needed:

```bash
YAK="/Applications/Rhino 8.app/Contents/Resources/bin/yak" ./scripts/yak-pack.sh
```

Platform-specific packages (optional):

```bash
./scripts/yak-pack.sh --platform mac
./scripts/yak-pack.sh --platform win
```

The output **`*.yak`** name includes a **distribution tag** (Rhino/Grasshopper version + platform) inferred from the built **`.gha`**. See [Anatomy of a package](https://developer.rhino3d.com/guides/yak/the-anatomy-of-a-package/).

### 7.4 Publish

- [Pushing a package to the server](https://developer.rhino3d.com/guides/yak/pushing-a-package-to-the-server): authenticate with Yak, then **`yak push`** the **`.yak`** file(s).
- Food4Rhino and other listings are separate: create a product page and point users at Package Manager and/or direct downloads as you prefer.

---

## 8. Important identifiers (quick reference)

| What | Name / value |
|------|------------------|
| Namespace | `Grasshopper.GUI.Canvas` |
| Skin class | `GH_Skin` |
| Load from XML | `GH_Skin.LoadSkin()` |
| Save to XML | `GH_Skin.SaveSkin()` |
| Canvas background field | `GH_Skin.canvas_back` (Color) |
| Assembly info class | `GHDarkModeInfo` : `GH_AssemblyInfo` |
| Component class | `GHDarkModeComponent` : `GH_Component` |
| Component Guid | `B1C2D3E4-F5A6-4780-BCDE-F12345678901` |
| Input param | **M** (bool), nickname "Mode" |
| Output param | **Out** (text), nickname "Out" |
| Category / Subcategory | **Params** / **Util** |
| Settings file | `grasshopper_gui.xml` (Grasshopper → File → Special Folders → Settings Folder) |

---

## 9. References

### 9.1 Grasshopper API

- [GH_Skin Class](https://developer.rhino3d.com/api/grasshopper/html/T_Grasshopper_GUI_Canvas_GH_Skin.htm)
- [GH_Skin.LoadSkin](https://developer.rhino3d.com/api/grasshopper/html/M_Grasshopper_GUI_Canvas_GH_Skin_LoadSkin.htm)
- [GH_Skin.SaveSkin](https://developer.rhino3d.com/api/grasshopper/html/M_Grasshopper_GUI_Canvas_GH_Skin_SaveSkin.htm)
- [GH_AssemblyInfo](https://developer.rhino3d.com/api/grasshopper/html/T_Grasshopper_Kernel_GH_AssemblyInfo.htm)

### 9.2 Mac development

- [Installing Tools (Mac)](https://developer.rhino3d.com/guides/grasshopper/installing-tools-mac/)
- [Your First Component (Mac)](https://developer.rhino3d.com/guides/grasshopper/your-first-component-mac/)
- [Using NuGet](https://developer.rhino3d.com/guides/rhinocommon/using-nuget/) (RhinoCommon / Grasshopper packages)

### 9.3 Packaging (Yak)

- [The Anatomy of a Package](https://developer.rhino3d.com/guides/yak/the-anatomy-of-a-package/)
- [Creating a Grasshopper Plug-In Package](https://developer.rhino3d.com/guides/yak/creating-a-grasshopper-plugin-package/)
- [The Package Manifest](https://developer.rhino3d.com/guides/yak/the-package-manifest/) (manifest.yml reference)
- [Pushing a Package to the Server](https://developer.rhino3d.com/guides/yak/pushing-a-package-to-the-server/)

### 9.4 In-repo docs

- **docs/SDK_VERSION_AND_COMPATIBILITY.md** — Why we reference the Rhino app SDK and how to override the path.
- **README.md** — Repo overview, quick start, status.

---

## 10. License (distribution)

- **Full text:** **LICENSE** at repo root (downloaded from [https://www.gnu.org/licenses/gpl-3.0.txt](https://www.gnu.org/licenses/gpl-3.0.txt)); do not edit the license text itself.
- **Copyright:** Puya Khalili; detailed authorship is in **git history**.
- **Plugin vs host:** GPLv3 governs **this repository** (source, embedded resources, scripts, docs you ship with it). It does **not** change McNeel’s terms for Rhino or Grasshopper. Distributors who publish modified binaries must comply with GPLv3 (e.g. source offer / corresponding source).

---

## 11. Changelog (summary)

- **Initial:** Scaffold, probe component (LoadSkin / canvas_back / SaveSkin), build script, install as .gha, SDK fix (Rhino app refs), Out parameter, .gitignore, README, GitHub repo (GHDarkMode), GPLv3 **LICENSE**.
- **Ongoing:** Theme tweaks, Food4Rhino listing; the same **`.gha`** is used on **Windows and Mac** Rhino 8.
