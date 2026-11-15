#!/usr/bin/env bash
# Cross-builds Dawn's static library from source for linux-arm64, which Dawn's releases do not cover, with clang
# against an arm64 sysroot (see make-sysroot.sh). Same configuration as Dawn's release build
# (.github/workflows/dawn-ci.cmake), without samples, tests or tools.
# Usage: build-source-linux.sh <dir to clone into> <arm64 sysroot> <out dir> [dir holding ld.lld]
# Writes <out>/libwebgpu_dawn.a. Then: link-linux.sh <out>/libwebgpu_dawn.a ... with the same cross toolchain.
set -euo pipefail
src=$1; sysroot=$(cd "$2" && pwd); out=$3; lld_dir=${4:-}
here=$(cd "$(dirname "$0")" && pwd)
commit=$(python3 -c "import json,sys; print(json.load(open(sys.argv[1]))['commit'])" "$here/version.json")
if [ ! -d "$src/.git" ]; then
	git init -q "$src"
	git -C "$src" remote add origin https://dawn.googlesource.com/dawn
fi
if [ "$(git -C "$src" rev-parse HEAD 2>/dev/null)" != "$commit" ]; then
	git -C "$src" fetch -q --depth 1 origin "$commit"
	git -C "$src" checkout -q --force FETCH_HEAD
fi

build="$src/out/linux-arm64"
mkdir -p "$build"
linker="-fuse-ld=lld"
[ -n "$lld_dir" ] && linker="$linker --ld-path=$lld_dir/ld.lld"
cat > "$build/toolchain.cmake" <<EOF
set(CMAKE_SYSTEM_NAME Linux)
set(CMAKE_SYSTEM_PROCESSOR aarch64)
set(CMAKE_SYSROOT "$sysroot")
set(CMAKE_C_COMPILER clang)
set(CMAKE_CXX_COMPILER clang++)
set(CMAKE_C_COMPILER_TARGET aarch64-linux-gnu)
set(CMAKE_CXX_COMPILER_TARGET aarch64-linux-gnu)
set(CMAKE_EXE_LINKER_FLAGS_INIT "$linker")
set(CMAKE_SHARED_LINKER_FLAGS_INIT "$linker")
set(CMAKE_FIND_ROOT_PATH_MODE_PROGRAM NEVER)
set(CMAKE_FIND_ROOT_PATH_MODE_LIBRARY ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_INCLUDE ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_PACKAGE ONLY)
set(ENV{PKG_CONFIG_SYSROOT_DIR} "$sysroot")
set(ENV{PKG_CONFIG_LIBDIR} "$sysroot/usr/lib/aarch64-linux-gnu/pkgconfig:$sysroot/usr/share/pkgconfig")
EOF
cmake -S "$src" -B "$build" -G Ninja \
	-C "$src/.github/workflows/dawn-ci.cmake" \
	-DCMAKE_TOOLCHAIN_FILE="$build/toolchain.cmake" \
	-DCMAKE_BUILD_TYPE=Release \
	-DCMAKE_C_COMPILER_LAUNCHER= -DCMAKE_CXX_COMPILER_LAUNCHER= \
	-DDAWN_BUILD_MONOLITHIC_LIBRARY=STATIC \
	-DDAWN_BUILD_SAMPLES=OFF -DDAWN_BUILD_TESTS=OFF -DDAWN_USE_GLFW=OFF -DDAWN_USE_WAYLAND=OFF \
	-DTINT_BUILD_TESTS=OFF -DTINT_BUILD_CMD_TOOLS=OFF -DTINT_BUILD_IR_BINARY=OFF -DDAWN_BUILD_PROTOBUF=OFF
cmake --build "$build" --target webgpu_dawn
mkdir -p "$out"
cp "$(find "$build" -name libwebgpu_dawn.a | head -1)" "$out/"
echo "Built $out/libwebgpu_dawn.a"
