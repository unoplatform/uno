#!/bin/bash

set -e

rm -rf build
xcodebuild $@
mkdir -p ../runtimes/osx/native
cp -R build/Release/libUnoNativeMac.* ../runtimes/osx/native || true
