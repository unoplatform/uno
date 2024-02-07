#!/bin/bash

rm -rf build
cd UnoNativeMac
./getSkiaSharpDylib.sh
cd ..
xcodebuild $@
mkdir ../runtimes/osx/native
cp UnoNativeMac/build/Release/libUnoNativeMac.* ../runtimes/osx/native
