#!/usr/bin/env python3
"""Downloads and unpacks one of the pinned Dawn release's prebuilt archives.

Usage: fetch.py <windows|ubuntu|macos|android|apple|headers> <output dir>
Prints the directory the archive unpacked to. Version: version.json beside this script.
"""
import json
import os
import sys
import tarfile
import urllib.request

ASSETS = {
    "windows": "Dawn-{commit}-windows-latest-Release.tar.gz",
    "ubuntu": "Dawn-{commit}-ubuntu-latest-Release.tar.gz",
    "macos": "Dawn-{commit}-macos-latest-Release.tar.gz",
    "android": "dawn-android-{commit}.tar.gz",
    "apple": "dawn-apple-{commit}.xcframework.tar.gz",
    "headers": "dawn-headers-{commit}.tar.gz",
}


def main():
    if len(sys.argv) != 3 or sys.argv[1] not in ASSETS:
        sys.exit(__doc__)
    kind, out = sys.argv[1], sys.argv[2]
    version = json.load(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "version.json")))
    asset = ASSETS[kind].format(commit=version["commit"])
    os.makedirs(out, exist_ok=True)
    archive = os.path.join(out, asset)
    if not os.path.exists(archive):
        url = f"https://github.com/google/dawn/releases/download/{version['tag']}/{asset}"
        print(f"Downloading {url}", file=sys.stderr)
        urllib.request.urlretrieve(url, archive + ".part")
        os.replace(archive + ".part", archive)
    with tarfile.open(archive) as tar:
        # The archives each hold one top-level folder, not always named after the archive.
        root = os.path.join(out, tar.next().name.split("/")[0])
        if not os.path.isdir(root):
            # The "data" filter rejects absolute paths and links out of the folder; Python before 3.12 lacks it.
            if hasattr(tarfile, "data_filter"):
                tar.extractall(out, filter="data")
            else:
                tar.extractall(out)
    print(root)


if __name__ == "__main__":
    main()
