#!/usr/bin/env bash
# Links Dawn's static library into libwebgpu.so, exporting the webgpu.h API.
# Usage: link-linux.sh <libwebgpu_dawn.a> <dir with webgpu_dawn.map> <out dir> [target triple] [sysroot]
# With a target triple (e.g. aarch64-linux-gnu) it cross-links with clang and lld against the sysroot; LD_PATH, when
# set, points clang at an ld.lld outside the PATH.
# Symbol versions are bound here, so the glibc linked against sets the floor: link against the oldest one supported.
set -euo pipefail
lib=$1; exports=$2; out=$3; target=${4:-}; sysroot=${5:-}
here=$(cd "$(dirname "$0")" && pwd)
if [ -n "$target" ]; then
	flags=(--target="$target" --sysroot="$sysroot")
	link_flags=(-fuse-ld=lld ${LD_PATH:+--ld-path=$LD_PATH})
	strip_tool=llvm-strip
	command -v llvm-strip >/dev/null || strip_tool=$(ls /usr/bin/llvm-strip-* 2>/dev/null | head -1)
else
	flags=()
	link_flags=()
	strip_tool=strip
fi
mkdir -p "$out"
clang "${flags[@]}" -std=c11 -O2 -fPIC -fvisibility=hidden -c "$here/glibc_compat.c" -o "$out/glibc_compat.o"
# libstdc++ goes in statically so the library does not depend on the host's C++ runtime; libgcc_s stays shared, since
# the static unwinder needs a newer glibc (_dl_find_object, 2.35).
clang++ "${flags[@]}" "${link_flags[@]}" -shared -o "$out/libwebgpu.so" "$out/glibc_compat.o" \
	-Wl,--whole-archive "$lib" -Wl,--no-whole-archive \
	-Wl,--version-script="$exports/webgpu_dawn.map" \
	-Wl,--no-undefined -Wl,-z,defs -Wl,--gc-sections -Wl,-soname,libwebgpu.so \
	-static-libstdc++ -lpthread -ldl -lm
rm "$out/glibc_compat.o"
"$strip_tool" --strip-unneeded "$out/libwebgpu.so"
echo "Linked $out/libwebgpu.so"
