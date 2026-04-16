# GH Dark Mode

Grasshopper 1 plugin (Mac): toggle GUI between Dark Mode and Light Mode.

- **Build and install:** From repo root run `./scripts/build-and-install.sh`. Restart Rhino/Grasshopper to load.
- **Component:** Params → Util → **GH Dark Mode**.
  - Input **M** (bool): **true** = apply dark; **false** = restore baseline (pre-dark) skin.
  - Input **R** (bool): reset/restore (restores baseline if present; otherwise factory reset).
- **SDK compatibility:** The project builds against the Grasshopper SDK from your **installed Rhino 8 app** (default: `/Applications/Rhino 8.app`), so the plugin matches your Rhino version (e.g. 8.15) and loads correctly. See [docs/SDK_VERSION_AND_COMPATIBILITY.md](../../docs/SDK_VERSION_AND_COMPATIBILITY.md).
