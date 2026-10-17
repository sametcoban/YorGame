#!/usr/bin/env python3
"""Validate Unity asset identities and serialized references without the Editor."""
from pathlib import Path
import re
import sys


def main():
    root = Path(__file__).resolve().parents[1]
    assets = root / "Assets"
    pattern = re.compile(r"\bguid:\s*([^\s,}]+)")
    valid = re.compile(r"[0-9a-fA-F]{32}\Z")
    # Unity's built-in resources are outside the project. Construct the two
    # built-in resource identities at the required 128-bit width.
    builtins = {"0" * 32, format(0xE000000000000000, "032x"),
                format(0xF000000000000000, "032x")}
    owners = {}
    references = []
    errors = []

    for directory in (assets, root / "ProjectSettings"):
        for path in sorted(directory.rglob("*")):
            if not path.is_file():
                continue
            if path.suffix not in {".meta", ".mat", ".unity", ".asset", ".prefab", ".controller"}:
                continue
            for number, line in enumerate(path.read_text(encoding="utf-8-sig").splitlines(), 1):
                for match in pattern.finditer(line):
                    guid = match.group(1)
                    where = f"{path.relative_to(root)}:{number}"
                    if not valid.fullmatch(guid):
                        errors.append(f"{where}: invalid GUID {guid!r} ({len(guid)} characters; expected 32 hex characters)")
                        continue
                    guid = guid.lower()
                    if path.suffix == ".meta" and line.startswith("guid:"):
                        if guid in owners:
                            errors.append(f"{where}: duplicate asset GUID, also declared in {owners[guid]}")
                        owners[guid] = path.relative_to(root)
                    else:
                        references.append((guid, where))

    for guid, where in references:
        if guid not in owners and guid not in builtins:
            errors.append(f"{where}: GUID {guid} does not identify an asset or a built-in resource")
    if errors:
        print("\n".join(errors), file=sys.stderr)
        return 1
    print(f"PASS: {len(owners)} asset GUIDs and {len(references)} serialized references validated.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
