#/usr/bin/env bash
set -euo pipefail

mkdir -p angle_binaries/osx
pushd angle_binaries/osx
curl -O -L https://github.com/dotnet/Silk.NET/raw/a9c75a61528ee7b4e55442f5141cb132f10d72ff/src/Native/Silk.NET.OpenGLES.ANGLE.Native/runtimes/osx/native/libEGL.dylib
curl -O -L https://github.com/dotnet/Silk.NET/raw/a9c75a61528ee7b4e55442f5141cb132f10d72ff/src/Native/Silk.NET.OpenGLES.ANGLE.Native/runtimes/osx/native/libGLESv2.dylib
popd
