#!/usr/bin/env bash
# Links Dawn's per-ABI static libraries into libwebgpu.so for each Android ABI.
# Usage: link-android.sh <unpacked dawn-android dir> <dir with webgpu_dawn.map> <out dir>
# Needs ANDROID_NDK_HOME (or ANDROID_NDK_ROOT). Writes <out>/<rid>/libwebgpu.so for android-arm64, -arm, -x64, -x86.
set -euo pipefail
src=$1; exports=$2; out=$3
ndk=${ANDROID_NDK_HOME:-${ANDROID_NDK_ROOT:?Set ANDROID_NDK_HOME}}
host=$(ls "$ndk/toolchains/llvm/prebuilt" | head -1)
bin="$ndk/toolchains/llvm/prebuilt/$host/bin"
# The lowest Android version an Uno app supports (Uno.Sdk's SupportedOSPlatformVersion).
api=24
for entry in "arm64-v8a aarch64-linux-android android-arm64" "armeabi-v7a armv7a-linux-androideabi android-arm" \
	"x86_64 x86_64-linux-android android-x64" "x86 i686-linux-android android-x86"; do
	set -- $entry
	abi=$1; triple=$2; rid=$3
	mkdir -p "$out/$rid"
	cxx="$bin/clang++"
	[ -x "$cxx" ] || cxx="$bin/clang++.exe"
	"$cxx" --target="$triple$api" -shared -o "$out/$rid/libwebgpu.so" \
		-Wl,--whole-archive "$src/$abi/libwebgpu_dawn.a" -Wl,--no-whole-archive \
		-Wl,--version-script="$exports/webgpu_dawn.map" \
		-Wl,--no-undefined -Wl,--gc-sections -Wl,-soname,libwebgpu.so \
		-Wl,-z,max-page-size=16384 -static-libstdc++ -llog -landroid -ldl -lm
	"$bin/llvm-strip" --strip-unneeded "$out/$rid/libwebgpu.so" 2>/dev/null || "$bin/llvm-strip.exe" --strip-unneeded "$out/$rid/libwebgpu.so"
	echo "Linked $out/$rid/libwebgpu.so"
done
