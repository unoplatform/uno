#!/usr/bin/env python3
"""Writes the list of functions a Dawn shared library exports, in each linker's format.

Dawn's releases ship static libraries only, so we link our own shared library from them and export exactly the
webgpu.h API: every function declared WGPU_EXPORT in include/dawn/webgpu.h.

Usage: exports.py <path to include/dawn/webgpu.h> <output dir>
Writes webgpu_dawn.def (MSVC), webgpu_dawn.map (GNU version script) and webgpu_dawn.exp (Apple exported symbols).
"""
import os
import re
import sys


def exported_functions(header):
    text = open(header, encoding="utf-8").read()
    names = sorted(set(re.findall(r"^WGPU_EXPORT\b[^;(]*?\b(wgpu\w+)\s*\(", text, re.MULTILINE)))
    if len(names) < 100:
        sys.exit(f"Found only {len(names)} exported functions in {header}; the header layout must have changed.")
    return names


def main():
    if len(sys.argv) != 3:
        sys.exit(__doc__)
    header, out = sys.argv[1], sys.argv[2]
    names = exported_functions(header)
    os.makedirs(out, exist_ok=True)
    with open(os.path.join(out, "webgpu_dawn.def"), "w", newline="\n") as f:
        f.write("EXPORTS\n")
        f.writelines(f"    {n}\n" for n in names)
    with open(os.path.join(out, "webgpu_dawn.map"), "w", newline="\n") as f:
        f.write("{ global:\n")
        f.writelines(f"    {n};\n" for n in names)
        f.write("  local: *;\n};\n")
    with open(os.path.join(out, "webgpu_dawn.exp"), "w", newline="\n") as f:
        f.writelines(f"_{n}\n" for n in names)
    print(f"{len(names)} exported functions")


if __name__ == "__main__":
    main()
