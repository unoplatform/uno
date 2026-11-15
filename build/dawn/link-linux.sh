#!/usr/bin/env bash
# Links Dawn's static library into libwebgpu.so, exporting the webgpu.h API.
# Usage: link-linux.sh <libwebgpu_dawn.a> <dir with webgpu_dawn.map> <out dir> [C++ compiler, default clang++]
# Symbol versions are bound here, so the host's glibc sets the floor: link on the oldest one the package supports.
set -euo pipefail
lib=$1; exports=$2; out=$3; cxx=${4:-clang++}
cc=${cxx%++}
[ "$cc" = "g" ] && cc=gcc
mkdir -p "$out"
"$cc" -std=c11 -O2 -fPIC -fvisibility=hidden -c "$(dirname "$0")/glibc_compat.c" -o "$out/glibc_compat.o"
# libstdc++ goes in statically so the library does not depend on the host's C++ runtime; libgcc_s stays shared, since
# the static unwinder needs a newer glibc (_dl_find_object, 2.35).
"$cxx" -shared -o "$out/libwebgpu.so" "$out/glibc_compat.o" \
	-Wl,--whole-archive "$lib" -Wl,--no-whole-archive \
	-Wl,--version-script="$exports/webgpu_dawn.map" \
	-Wl,--no-undefined -Wl,-z,defs -Wl,--gc-sections -Wl,-soname,libwebgpu.so \
	-static-libstdc++ -lpthread -ldl -lm
rm "$out/glibc_compat.o"
strip --strip-unneeded "$out/libwebgpu.so"
echo "Linked $out/libwebgpu.so"
