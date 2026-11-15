#!/usr/bin/env bash
# Builds the Apple payload from Dawn's xcframework: libwebgpu.dylib per macOS architecture, and the static library
# per iOS slice (iOS apps link Dawn into the app image).
# Usage: link-apple.sh <unpacked dawn-apple .xcframework dir> <dir with webgpu_dawn.exp> <out dir>
# Writes <out>/osx-arm64, osx-x64 (libwebgpu.dylib) and ios-arm64, iossimulator-arm64, iossimulator-x64 (libwebgpu_dawn.a).
set -euo pipefail
xcf=$1; exports=$2; out=$3
here=$(cd "$(dirname "$0")" && pwd)
frameworks="-framework Metal -framework QuartzCore -framework IOSurface -framework IOKit -framework Foundation -framework CoreFoundation -framework CoreGraphics"

# One architecture of a slice, with the webgpu.h functions made global (see unhide-macho.py).
slice() {
	local src=$1 arch=$2 dst=$3
	if lipo -info "$src" | grep -q "^Non-fat"; then cp "$src" "$dst.thin"; else lipo "$src" -thin "$arch" -output "$dst.thin"; fi
	python3 "$here/unhide-macho.py" "$dst.thin" "$exports/webgpu_dawn.exp" "$dst"
	rm "$dst.thin"
	ranlib "$dst"
}

for entry in "arm64 osx-arm64" "x86_64 osx-x64"; do
	set -- $entry
	arch=$1; rid=$2
	mkdir -p "$out/$rid"
	slice "$xcf/macos-arm64_x86_64/libwebgpu_dawn.a" "$arch" "$out/$rid/libwebgpu_dawn.a"
	# Dawn's objects target macOS 12.
	clang++ -arch "$arch" -mmacosx-version-min=12.0 -dynamiclib -o "$out/$rid/libwebgpu.dylib" \
		-Wl,-force_load,"$out/$rid/libwebgpu_dawn.a" -Wl,-exported_symbols_list,"$exports/webgpu_dawn.exp" \
		-Wl,-dead_strip -install_name @rpath/libwebgpu.dylib $frameworks
	rm "$out/$rid/libwebgpu_dawn.a"
	strip -x "$out/$rid/libwebgpu.dylib"
	echo "Linked $out/$rid/libwebgpu.dylib ($(nm -gU "$out/$rid/libwebgpu.dylib" | grep -c ' _wgpu') exported)"
done

for entry in "ios-arm64 arm64 ios-arm64" "ios-arm64_x86_64-simulator arm64 iossimulator-arm64" "ios-arm64_x86_64-simulator x86_64 iossimulator-x64"; do
	set -- $entry
	mkdir -p "$out/$3"
	slice "$xcf/$1/libwebgpu_dawn.a" "$2" "$out/$3/libwebgpu_dawn.a"
	echo "Wrote $out/$3/libwebgpu_dawn.a"
done
