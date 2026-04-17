#!/usr/bin/env python3
"""
Extract gh_drawing_color item keys from grasshopper_gui.xml and merge into skin-keys-manifest.json.

Usage:
  python3 scripts/extract_skin_keys_from_xml.py \\
    --xml "$HOME/.../grasshopper_gui.xml" \\
    --manifest src/GHDarkMode/Resources/skin-keys-manifest.json

Keeps the existing "favorites" array in the manifest; replaces "keys" with all color keys
not already listed in favorites (sorted). Writes back to --manifest.

Requires Python 3.9+ (stdlib only).
"""

from __future__ import annotations

import argparse
import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def extract_color_keys(xml_path: Path) -> list[str]:
    tree = ET.parse(xml_path)
    keys: set[str] = set()
    for item in tree.findall(".//item"):
        name = item.get("name")
        type_name = item.get("type_name")
        if name and type_name == "gh_drawing_color":
            keys.add(name)
    return sorted(keys, key=str.lower)


def main() -> int:
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--xml", type=Path, required=True, help="Path to grasshopper_gui.xml")
    p.add_argument("--manifest", type=Path, required=True, help="Path to skin-keys-manifest.json to update")
    args = p.parse_args()

    if not args.xml.is_file():
        print(f"Error: XML not found: {args.xml}", file=sys.stderr)
        return 1
    if not args.manifest.is_file():
        print(f"Error: manifest not found: {args.manifest}", file=sys.stderr)
        return 1

    extracted = extract_color_keys(args.xml)
    text = args.manifest.read_text(encoding="utf-8")
    data = json.loads(text)

    favorites = data.get("favorites") or []
    fav_keys = {str(f.get("key", "")).strip() for f in favorites if isinstance(f, dict)}
    fav_keys.discard("")

    rest = [k for k in extracted if k not in fav_keys]
    data["keys"] = rest
    data["version"] = int(data.get("version", 1))

    args.manifest.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")
    print(f"Updated {args.manifest}: {len(favorites)} favorites, {len(rest)} other keys (from {len(extracted)} color items).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
