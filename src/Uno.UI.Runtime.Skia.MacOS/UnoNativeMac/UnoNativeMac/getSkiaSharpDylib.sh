#!/usr/bin/env bash

set -e

VERSION=${VERSION:-3.119.1}

filename="skiasharp_"${VERSION}"_nativeassets"
build_dir="${filename}-tmp-build"
NUGET_PACKAGES=${NUGET_PACKAGES:-~/.nuget/packages}

if [ ! -f "${filename}"/libSkiaSharp.dylib ]; then
    # download the SkiaSharp.NativeAssets.macOS using the configured nuget feeds
    mkdir -p "${build_dir}"
    cd "${build_dir}"
    dotnet new console
    dotnet add package SkiaSharp.NativeAssets.macOS --version ${VERSION}
    dotnet build
    cd ..
    rm -fr "${build_dir}"
    rm -fr "${filename}"
    mkdir -p "${filename}"
    cp -r ${NUGET_PACKAGES}/skiasharp.nativeassets.macos/${VERSION}/runtimes/osx/native/* "${filename}"
    rm -fr "${build_dir}"
fi
cp "${filename}/libSkiaSharp.dylib" .
