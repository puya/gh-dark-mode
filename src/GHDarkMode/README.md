# GH Dark Mode

Grasshopper 1 plugin for **Rhino 8** (**Windows and Mac**): dark/light GUI via `GH_Skin` and `grasshopper_gui.xml`.

Screenshot (repo root): [../docs/gh-darkmode-screenshot.jpg](../docs/gh-darkmode-screenshot.jpg) · Overview: [../../README.md](../../README.md)

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
- **OVR** — optional list: connect outputs from **GH Dark Mode Override** for per-key XML colors (same key as a built-in default replaces that default).
- **R** — restore baseline if present, else factory reset `grasshopper_gui.xml`.
- **Invert** (`I`) — debug: invert all colors in the GUI XML (use sparingly; restore with **R** or **M=false**).

Dark mode applies built-in XML colors for wires, normal/hidden std/sel edges and fills, etc.; use **GH Dark Mode Override** for anything else.

**Out** — status, XML write count, any keys missing from XML, and the full merged override list (built-in + **OVR**) after a dark apply.

**Icons** — embedded 24×24 PNGs with alpha: main toggles **main** vs **gear** art with last dark/light solve; override uses the **gear** icon.

Settings live under the Grasshopper plugin folder, e.g. `grasshopper_gui.xml` and `ghdarkmode_baseline_gui.xml`.

### GH Dark Mode Override

- **Target** (`T`) — named-value list of skin keys: **favorites first**, then every `gh_drawing_color` key from the embedded `skin-keys-manifest.json` (regenerate with `scripts/extract_skin_keys_from_xml.py` from your `grasshopper_gui.xml` when needed). Click **T** on the canvas to open the picker; if you still see only a plain integer after updating, reinstall the `.gha` and place a **new** component (old instances keep the previous parameter type).
- **Color** (`C`) — override ARGB for the chosen key.
- **Custom key** (`K`, optional) — raw XML item name; when set, overrides **Target**.

Connect **Override** into **GH Dark Mode**’s **OVR** input.

**Refresh manifest keys from your Grasshopper XML**

```bash
python3 scripts/extract_skin_keys_from_xml.py \
  --xml "$HOME/Library/Application Support/McNeel/Rhinoceros/8.0/Plug-ins/Grasshopper (b45a29b1-4343-4035-989e-044e8580d9cf)/grasshopper_gui.xml" \
  --manifest src/GHDarkMode/Resources/skin-keys-manifest.json
```

Then rebuild/reinstall so the updated JSON is embedded in the `.gha`.
