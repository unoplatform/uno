#!/bin/bash

rm -rf build
cd UnoNativeMac
./getSkiaSharpDylib.sh
cd ..
xcodebuild $@
mkdir -p ../runtimes/osx/native
cp -R build/Release/libUnoNativeMac.* ../runtimes/osx/native || true
