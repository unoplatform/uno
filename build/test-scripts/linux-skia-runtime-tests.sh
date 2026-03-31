#!/bin/bash
set -x #echo on
set -euo pipefail
IFS=$'\n\t'

export UITEST_RUNTIME_TEST_GROUP=${UITEST_RUNTIME_TEST_GROUP:-}

export UNO_TESTS_FAILED_LIST=$BUILD_SOURCESDIRECTORY/build/uitests-failure-results/failed-tests-skia-linux-runtimetests-$UITEST_RUNTIME_TEST_GROUP.txt
export TEST_RESULTS_FILE=$BUILD_SOURCESDIRECTORY/build/skia-linux-runtime-tests-results.xml

if [ -f "$UNO_TESTS_FAILED_LIST" ]; then
	export UITEST_RUNTIME_TESTS_FILTER=`cat $UNO_TESTS_FAILED_LIST | base64 -w 0`

	# echo the failed filter list, if not empty
	if [ -n "$UITEST_RUNTIME_TESTS_FILTER" ]; then
		echo "Tests to run: $UITEST_RUNTIME_TESTS_FILTER"
	fi
fi

USE_XVFB=${1:-true}

# Grant access to DRM and framebuffer devices if they exist, so the app
# can use hardware rendering instead of falling back to headless mode.
sudo chmod a+rw /dev/dri/card* 2>/dev/null || true
sudo chmod a+rw /dev/fb* 2>/dev/null || true

cd $SamplesAppArtifactPath
if [ "$USE_XVFB" = "true" ]; then
	xvfb-run --auto-servernum --server-args='-screen 0 1280x1024x24' sh -c '{ fluxbox & } ; dotnet SamplesApp.Skia.Generic.dll --runtime-tests=$TEST_RESULTS_FILE' || true # sometimes we crash during app shutdown, so we're forcing a 0 exit code
else
	dotnet SamplesApp.Skia.Generic.dll --runtime-tests=$TEST_RESULTS_FILE || true
fi

## Export the failed tests list for reuse in a pipeline retry
pushd $BUILD_SOURCESDIRECTORY/src/Uno.NUnitTransformTool
mkdir -p $(dirname ${UNO_TESTS_FAILED_LIST})

echo "Running NUnitTransformTool"

## Fail the build when no test results could be read
dotnet run fail-empty $TEST_RESULTS_FILE

if [ $? -eq 0 ]; then
	dotnet run list-failed $TEST_RESULTS_FILE $UNO_TESTS_FAILED_LIST
fi

popd
