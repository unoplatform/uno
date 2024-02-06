#!/bin/bash

rm -rf build
cd UnoNativeMac
./getSkiaSharpDylib.sh
cd ..
xcodebuild $@
