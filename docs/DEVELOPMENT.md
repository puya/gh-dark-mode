# GH Dark Mode — Development Document

This document records the approach, findings, progress, roadmap, API details, build/packaging, and references for the GH Dark Mode Grasshopper 1 plugin (Mac). It is the single source of truth for development implementation.

---

## 1. Project overview

- **Name:** GH Dark Mode (repository: GHDarkMode)
- **Platform:** Rhino 8 for Mac, Grasshopper 1 (not Grasshopper 2)
- **Purpose:** Toggle Grasshopper’s GUI between **Dark Mode** and **Light Mode** by writing to the same mechanism the Windows plugin [Moonlight](https://food4rhino.com/en/app/moonlight) (by ekimroyrp) uses: the **GH_Skin** API and **grasshopper_gui.xml**.
- **Status:** Probe phase complete (API verified on Mac). Dark/light theme implementation is next.

---

## 2. Approach and findings

### 2.1 Original Moonlight (Windows)

- **Format:** Single .gha (Windows PE32 .NET assembly, 32-bit). Does not run on Mac.
- **Behavior:** One component under Params → Util. Input **M** (Mode): boolean/button. When true → apply dark theme; when false → apply default light theme. Persistence via **grasshopper_gui.xml** (Grasshopper settings folder). No outputs in the original; we added **Out** for debugging.
- **Tech:** C#, uses **GH_Skin** (namespace `Grasshopper.GUI.Canvas`). Reads/writes `grasshopper_gui.xml` via `LoadSkin()` and `SaveSkin()`.
- **Source:** Closed-source. No Mac build available; no open-source clone found. We reimplement from the public API and behavior description.

### 2.2 Why .gha and not .dll

- Grasshopper **only discovers add-ons with the `.gha` extension** in the Libraries folder.
- A `.gha` file is the **same .NET assembly** as a `.dll`; we build `GHDarkMode.dll` and **copy it as `GHDarkMode.gha`** on install. No extra packaging step.

### 2.3 SDK version compatibility

- Grasshopper loads a .gha only if it was **built against the same or an older minor SDK version** than the one running (e.g. Local SDK 8.15 rejects Referenced SDK 8.28).
- **Solution:** Build against the **assemblies shipped with the user’s Rhino 8 app** instead of a fixed NuGet version. Path used:  
  `$(RhinoAppPath)/Contents/Frameworks/RhCore.framework/Versions/A/Resources/ref/net48/`  
  Default `RhinoAppPath`: `/Applications/Rhino 8.app`. Override with `/p:RhinoAppPath="..."` if needed.
- **Fallback:** If that path does not exist (e.g. CI), the project uses NuGet **Grasshopper 8.26.25349.19001**; that build runs only on Rhino 8.26+.

### 2.4 Probe-first strategy

- Before implementing themes, we added a **probe** that calls `GH_Skin.LoadSkin()`, reads `GH_Skin.canvas_back`, and calls `GH_Skin.SaveSkin()` to confirm the API exists and works on Mac. No colors are changed in the probe; the **Out** parameter and component message report success or failure.

---

## 3. Roadmap and progress

### 3.1 Completed

| Item | Notes |
|------|------|
| Project scaffold | C# Grasshopper 1 project, namespace `GHDarkMode`, assembly name `GHDarkMode`. |
| Single component | **GHDarkModeComponent**: Params → Util, input **M** (bool), output **Out** (text). |
| Assembly info | **GHDarkModeInfo** (subclass of `GH_AssemblyInfo`), empty constructor, Name/Version/Description. |
| Probe logic | `LoadSkin()` → read `canvas_back` → `SaveSkin()`; report via Message, Remark, and **Out**. |
| Build script | `scripts/build-and-install.sh`: `dotnet build -c Release`, copy `bin/GHDarkMode.dll` → Libraries as `GHDarkMode.gha`. |
| SDK compatibility | Reference Rhino app `ref/net48` (Grasshopper, GH_IO, RhinoCommon); fallback NuGet 8.26. |
| Optional output | **Out** (text) for routing full status to a Panel. |
| Repo setup | .gitignore, README, private GitHub repo (GHDarkMode). |

### 3.2 Remaining

| Item | Notes |
|------|------|
| Dark theme | Set all `GH_Skin.*` fields to a dark palette (e.g. dark gray canvas, light wires), then call `SaveSkin()`. |
| Light theme | Set all `GH_Skin.*` fields to Grasshopper default light values (hardcode or load from a default `grasshopper_gui.xml`), then `SaveSkin()`. |
| Mode input behavior | When **M** is true → apply dark; when false → apply light. Optionally force canvas refresh after SaveSkin (if API allows). |
| Icons | Optional: 24×24 icon(s) for component and/or assembly. |
| Packaging (Yak) | Optional: create a .yak package for distribution via Rhino Package Manager (see §6). |

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

- `palette_normal_*`, `palette_black_*`, `palette_blue_*`, `palette_brown_*`, `palette_error_*`, `palette_grey_*`, `palette_hidden_*`, `palette_locked_*`, `palette_pink_*`, `palette_trans_*`, `palette_warning_*`, `palette_white_*`.

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

### 5.1 Prerequisites

- .NET 7 SDK (or later); Rhino 8 for Mac with Grasshopper 1.
- Rhino 8 app at default path `/Applications/Rhino 8.app` (or set `RhinoAppPath`).

### 5.2 Commands

```bash
# From repo root: build and install to Grasshopper Libraries
./scripts/build-and-install.sh
```

The script:

1. Finds `dotnet` (PATH or `/usr/local/share/dotnet`, `/opt/homebrew/share/dotnet`).
2. Runs `dotnet build -c Release` in `src/GHDarkMode`.
3. Copies `src/GHDarkMode/bin/GHDarkMode.dll` to the Libraries folder as **`GHDarkMode.gha`**.
4. Removes any `GHDarkMode.dll` from Libraries.

**Libraries path (hardcoded in script):**

```
/Users/puya/Library/Application Support/McNeel/Rhinoceros/8.0/Plug-ins/Grasshopper (b45a29b1-4343-4035-989e-044e8580d9cf)/Libraries
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
| `scripts/build-and-install.sh` | Build + install to Libraries. |

---

## 6. Packaging and distribution (Yak)

For distribution via the Rhino Package Manager (Yak), use the following structure and workflow.

### 6.1 Package layout

- **.gha** (and any **.dll** if needed) must be in the **top-level** of the package (or in a framework-specific folder for multi-targeting). Grasshopper discovers only those.
- Example minimal layout:

```
dist/
├── GHDarkMode.gha
├── manifest.yml
├── icon.png          (optional; referenced in manifest)
└── misc/
    ├── README.md
    └── LICENSE.txt
```

### 6.2 Manifest (manifest.yml)

- **name**, **version**, **authors**, **description**, **url** are required/expected.
- **icon:** optional; e.g. `icon.png`.
- **keywords:** optional; can include component GUID for restore.
- Generate a skeleton with Yak: from the dist folder run `yak spec` (Mac: use the Yak binary from the Rhino app, e.g. `/Applications/Rhino 8.app/Contents/Resources/bin/yak`).

### 6.3 Build package (Mac)

```bash
# From the dist folder that contains manifest.yml and GHDarkMode.gha
/Applications/Rhino\ 8.app/Contents/Resources/bin/yak build
```

- Produces a **.yak** file. The filename includes a **distribution tag** (e.g. `rh8_15-mac` or `rh8_15-any`) inferred from the referenced Grasshopper/RhinoCommon version and optional `--platform mac` (or `win`, or `any`).
- For Mac-only: use `--platform mac` if supported by your Yak version.

### 6.4 Distribution tags

- Format: `rh<version>-<platform>`, e.g. `rh8_15-mac`, `rh8-mac`, `any-any`.
- Ensures the package is offered only to compatible Rhino/Grasshopper versions and platforms. See [The Anatomy of a Package](https://developer.rhino3d.com/guides/yak/the-anatomy-of-a-package/).

### 6.5 Publish

- [Pushing a package to the server](https://developer.rhino3d.com/guides/yak/pushing-a-package-to-the-server): authenticate with Yak, then push the .yak file.

---

## 7. Important identifiers (quick reference)

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

## 8. References

### 8.1 Grasshopper API

- [GH_Skin Class](https://developer.rhino3d.com/api/grasshopper/html/T_Grasshopper_GUI_Canvas_GH_Skin.htm)
- [GH_Skin.LoadSkin](https://developer.rhino3d.com/api/grasshopper/html/M_Grasshopper_GUI_Canvas_GH_Skin_LoadSkin.htm)
- [GH_Skin.SaveSkin](https://developer.rhino3d.com/api/grasshopper/html/M_Grasshopper_GUI_Canvas_GH_Skin_SaveSkin.htm)
- [GH_AssemblyInfo](https://developer.rhino3d.com/api/grasshopper/html/T_Grasshopper_Kernel_GH_AssemblyInfo.htm)

### 8.2 Mac development

- [Installing Tools (Mac)](https://developer.rhino3d.com/guides/grasshopper/installing-tools-mac/)
- [Your First Component (Mac)](https://developer.rhino3d.com/guides/grasshopper/your-first-component-mac/)
- [Using NuGet](https://developer.rhino3d.com/guides/rhinocommon/using-nuget/) (RhinoCommon / Grasshopper packages)

### 8.3 Packaging (Yak)

- [The Anatomy of a Package](https://developer.rhino3d.com/guides/yak/the-anatomy-of-a-package/)
- [Creating a Grasshopper Plug-In Package](https://developer.rhino3d.com/guides/yak/creating-a-grasshopper-plugin-package/)
- [The Package Manifest](https://developer.rhino3d.com/guides/yak/the-package-manifest/) (manifest.yml reference)
- [Pushing a Package to the Server](https://developer.rhino3d.com/guides/yak/pushing-a-package-to-the-server/)

### 8.4 In-repo docs

- **docs/SDK_VERSION_AND_COMPATIBILITY.md** — Why we reference the Rhino app SDK and how to override the path.
- **README.md** — Repo overview, quick start, status.

---

## 9. Changelog (summary)

- **Initial:** Scaffold, probe component (LoadSkin / canvas_back / SaveSkin), build script, install as .gha, SDK fix (Rhino app refs), Out parameter, .gitignore, README, GitHub repo (GHDarkMode).
- **Next:** Implement dark and light theme in `SolveInstance` using the GH_Skin field list above and `SaveSkin()`; optionally add icons and Yak package for distribution.
