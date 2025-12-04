#!/bin/bash

set -e

rm -rf build
cd UnoNativeMac
chmod +x getSkiaSharpDylib.sh
./getSkiaSharpDylib.sh
cd ..
xcodebuild $@
mkdir -p ../runtimes/osx/native
cp -R build/Release/libUnoNativeMac.* ../runtimes/osx/native || true
