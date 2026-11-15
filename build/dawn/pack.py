#!/usr/bin/env python3
"""Packs Uno.WinUI.Composition.WebGpu.Dawn from the linked libraries.

Usage: pack.py <payload dir> <exports dir> <Dawn source dir> <output dir>

<payload dir> holds one folder per RID, as the link scripts write them (win-x64/webgpu.dll, ..., ios-arm64/libwebgpu_dawn.a).
<exports dir> is exports.py's output. <Dawn source dir> is a checkout of the pinned commit with its dependencies fetched
(build-source-*.sh/ps1 make one), for Dawn's license and those of the third-party code compiled into the libraries.
The package version is the Dawn release tag, 0.<date>.<time>.
"""
import glob
import json
import os
import shutil
import subprocess
import sys

RIDS = {
    "win-x64": "webgpu.dll", "win-arm64": "webgpu.dll",
    "linux-x64": "libwebgpu.so", "linux-arm64": "libwebgpu.so",
    "osx-x64": "libwebgpu.dylib", "osx-arm64": "libwebgpu.dylib",
    "android-arm64": "libwebgpu.so", "android-arm": "libwebgpu.so", "android-x64": "libwebgpu.so", "android-x86": "libwebgpu.so",
    "ios-arm64": "libwebgpu_dawn.a", "iossimulator-arm64": "libwebgpu_dawn.a", "iossimulator-x64": "libwebgpu_dawn.a",
}


def licenses(source):
    """Dawn's license, then each fetched dependency's, under a heading naming it."""
    parts = [open(os.path.join(source, "LICENSE"), encoding="utf-8").read()]
    for path in sorted(glob.glob(os.path.join(source, "third_party", "*", "LICENSE*")) + glob.glob(os.path.join(source, "third_party", "*", "src", "LICENSE*"))):
        name = os.path.relpath(path, os.path.join(source, "third_party")).replace(os.sep, "/")
        parts.append(f"\n\n========== third_party/{name} ==========\n\n" + open(path, encoding="utf-8", errors="replace").read())
    return "".join(parts)


def main():
    if len(sys.argv) != 5:
        sys.exit(__doc__)
    payload, exports, source, out = (os.path.abspath(a) for a in sys.argv[1:])
    here = os.path.dirname(os.path.abspath(__file__))
    tag = json.load(open(os.path.join(here, "version.json")))["tag"]
    version = "0." + tag.lstrip("v")

    missing = [f"{rid}/{name}" for rid, name in RIDS.items() if not os.path.exists(os.path.join(payload, rid, name))]
    if missing:
        sys.exit("Missing from the payload: " + ", ".join(missing))

    staging = os.path.join(out, "staging")
    shutil.rmtree(staging, ignore_errors=True)
    for rid in RIDS:
        shutil.copytree(os.path.join(payload, rid), os.path.join(staging, rid))
    shutil.copy(os.path.join(here, "package", "README.md"), staging)
    with open(os.path.join(staging, "LICENSE"), "w", encoding="utf-8", newline="\n") as f:
        f.write(licenses(source))
    # The iOS targets declare every webgpu.h function so the link keeps them (see the package's .targets).
    with open(os.path.join(exports, "webgpu_dawn.def")) as d, open(os.path.join(staging, "dawn-ios.symbols"), "w", newline="\n") as f:
        f.writelines(line.strip() + "\n" for line in d if line.startswith("    "))

    subprocess.run(["dotnet", "pack", os.path.join(here, "package", "Uno.WinUI.Composition.WebGpu.Dawn.csproj"),
                    "-o", out, f"-p:DawnPackageVersion={version}", f"-p:DawnTag={tag}", f"-p:DawnPayload={staging}"], check=True)
    print(f"Packed Uno.WinUI.Composition.WebGpu.Dawn {version} into {out}")


if __name__ == "__main__":
    main()
