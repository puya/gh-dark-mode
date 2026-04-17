# Skin Preset System (Future Task)

## Goal

Add a simple preset system so users can save custom color overrides from the GH Dark Mode component and reload them later.

## Why

- Preserve custom looks across sessions.
- Reuse/share named themes without rewiring color inputs every time.
- Keep current baseline restore behavior untouched.

## Proposed UX (Simple)

- `Skin Name` (text): preset identifier.
- `Save Skin` (bool/button): save current overrides under `Skin Name`.
- `Load Skin` (bool/button): load and apply overrides from `Skin Name`.
- Optional later: `Delete Skin` and `List Skins`.

## Data Model

- Store presets as JSON.
- Save only explicitly overridden values (not every possible field).
- Include metadata: `name`, `version`, `createdAt`, optional `notes`.

## Storage Location

- Grasshopper settings folder, plugin subfolder:
  - `.../Grasshopper (...)/GHDarkMode/skins/`

## Scope (v1)

- Save/load for existing override keys:
  - `BG`, `CF`, `CSF`, `CE`, `CNT`, `ST`, `CST`, `OF`, `OSF`, `WD`, `WSI`, `WSO`
- Keep fallback behavior:
  - missing key in preset -> use current dark default for that key.

## Constraints / Safety

- Do not alter baseline restore files/flow.
- Validate preset names (safe filename chars).
- Graceful errors in `Out` when save/load fails.

## Implementation Steps

1. Add preset control inputs (`Skin Name`, `Save Skin`, `Load Skin`).
2. Add JSON read/write helpers.
3. Map current overrides -> preset JSON.
4. Apply loaded preset values to runtime overrides.
5. Report active preset and loaded keys in `Out`.
6. Add docs and a minimal usage example.

## Acceptance Criteria

- User can save a named preset from connected color inputs.
- User can load the same preset later and get the same look.
- Removing an individual color input still falls back to defaults.
- `M = false` baseline restore still works exactly as before.
