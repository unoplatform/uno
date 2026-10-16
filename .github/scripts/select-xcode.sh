#!/bin/bash
# GitHub Actions take on build/ci/templates/ios-build-select-version.yml: select the Xcode .vsts-ci.yml
# pins (or the newest one of the same major when the image lacks it), then make sure the simulator
# runtime for the given platform is installed.
# Usage: select-xcode.sh <platform: iOS|tvOS> <simulator sdk: iphonesimulator|appletvsimulator>
set -euo pipefail

PLATFORM="${1:-tvOS}"
SIMULATOR_SDK="${2:-appletvsimulator}"

PINNED=$(sed -n "s/^  xCodeRoot: '\(.*\)'.*/\1/p" .vsts-ci.yml | head -n 1)
echo "Pinned Xcode: $PINNED"
echo "Installed Xcodes:"
ls -d /Applications/Xcode*.app

XCODE_ROOT="$PINNED"
if [ ! -d "$XCODE_ROOT" ]; then
	MAJOR=$(basename "$PINNED" .app | sed -E 's/^Xcode_([0-9]+).*/\1/')
	XCODE_ROOT=$(ls -d /Applications/Xcode_"$MAJOR"*.app 2>/dev/null | grep -v -i beta | sort -V | tail -n 1 || true)
	if [ -z "$XCODE_ROOT" ]; then
		echo "::error::Neither $PINNED nor any Xcode $MAJOR is installed on this image."
		exit 1
	fi
	echo "::warning::$PINNED is not on this image, using $XCODE_ROOT"
fi

echo "Selecting $XCODE_ROOT"
echo "MD_APPLE_SDK_ROOT=$XCODE_ROOT" >> "${GITHUB_ENV:-/dev/null}"
sudo xcode-select --switch "$XCODE_ROOT/Contents/Developer"
sudo xcodebuild -runFirstLaunch
xcodebuild -version

SDK_BUILD_VERSION=$(xcrun --sdk "$SIMULATOR_SDK" --show-sdk-build-version 2>/dev/null || true)
SDK_VERSION=$(xcrun --sdk "$SIMULATOR_SDK" --show-sdk-version 2>/dev/null || true)
echo "$PLATFORM simulator SDK: $SDK_VERSION ($SDK_BUILD_VERSION)"

has_runtime() {
	local runtimes
	runtimes=$(xcrun simctl list runtimes)
	if [ -n "$SDK_BUILD_VERSION" ]; then
		grep -Fq "$PLATFORM $SDK_VERSION ($SDK_VERSION - $SDK_BUILD_VERSION)" <<<"$runtimes" \
			|| grep -Fq "$SDK_BUILD_VERSION" <<<"$runtimes"
	elif [ -n "$SDK_VERSION" ]; then
		grep -Fq "$PLATFORM $SDK_VERSION" <<<"$runtimes"
	else
		grep -q "$PLATFORM" <<<"$runtimes"
	fi
}

xcrun simctl list runtimes

if has_runtime; then
	echo "Required $PLATFORM simulator runtime already installed."
	exit 0
fi

echo "$PLATFORM simulator runtime missing, downloading."
for attempt in 1 2 3; do
	if sudo xcodebuild -downloadPlatform "$PLATFORM"; then
		break
	fi
	if [ "$attempt" -eq 3 ]; then
		echo "::error::$PLATFORM simulator runtime download failed after $attempt attempts."
		exit 1
	fi
	echo "Download failed (attempt $attempt). Retrying in 30s..."
	sleep 30
done

xcrun simctl list runtimes
if ! has_runtime; then
	echo "::error::$PLATFORM simulator runtime still missing after the download."
	exit 1
fi
