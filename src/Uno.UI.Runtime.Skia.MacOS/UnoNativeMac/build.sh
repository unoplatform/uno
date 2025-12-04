#!/bin/bash

set -e

rm -rf build
cd UnoNativeMac
chmod +x getSkiaSharpDylib.sh
./getSkiaSharpDylib.sh
cd ..
if command -v sysctl >/dev/null 2>&1; then
	DEFAULT_JOBS=$(sysctl -n hw.logicalcpu 2>/dev/null || echo 8)
else
	DEFAULT_JOBS=8
fi
JOBS=${XCODEBUILD_JOBS:-$DEFAULT_JOBS}
xcodebuild -parallelizeTargets -jobs "$JOBS" "$@"
mkdir -p ../runtimes/osx/native
cp -R build/Release/libUnoNativeMac.* ../runtimes/osx/native || true
