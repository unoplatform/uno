#!/usr/bin/env bash

VERSION=${VERSION:-2.88.7}

filename="skiasharp_"${VERSION}"_nativeassets"
if [ ! -f "${filename}"/runtimes/osx/native/libSkiaSharp.dylib ]; then
    curl -o "${filename}.zip" -L "https://www.nuget.org/api/v2/package/SkiaSharp.NativeAssets.macOS/${VERSION}"
    unzip -d "${filename}" "${filename}.zip"
fi
cp "${filename}/runtimes/osx/native/libSkiaSharp.dylib" .
