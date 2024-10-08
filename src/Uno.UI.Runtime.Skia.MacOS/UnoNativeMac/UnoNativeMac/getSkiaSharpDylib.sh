#!/bin/bash

VERSION=${VERSION:-2.88.7}

nuget install SkiaSharp.NativeAssets.macOS -Version ${VERSION}
cp SkiaSharp.NativeAssets.macOS.${VERSION}/runtimes/osx/native/libSkiaSharp.dylib .
rm -rf SkiaSharp.NativeAssets.macOS.${VERSION}/
