#!/bin/bash
set -x #echo on
set -euo pipefail
IFS=$'\n\t'

export UITEST_RUNTIME_TEST_GROUP=${UITEST_RUNTIME_TEST_GROUP:-}

export UNO_TESTS_FAILED_LIST=$BUILD_SOURCESDIRECTORY/build/uitests-failure-results/failed-tests-skia-linux-runtimetests-$UITEST_RUNTIME_TEST_GROUP.txt
export TEST_RESULTS_FILE=$BUILD_SOURCESDIRECTORY/build/skia-linux-runtime-tests-results.xml
export UITEST_RUNTIME_CURRENT_TEST_FILE=$BUILD_SOURCESDIRECTORY/build/runtime-current-test-linux-$UITEST_RUNTIME_TEST_GROUP.txt

# CI uses this file to report the last started test when a shard hangs.
export UITEST_RUNTIME_CURRENT_TEST_FILE

if [ -f "$UNO_TESTS_FAILED_LIST" ]; then
	export UITEST_RUNTIME_TESTS_FILTER=`cat $UNO_TESTS_FAILED_LIST | base64 -w 0`

	# echo the failed filter list, if not empty
	if [ -n "$UITEST_RUNTIME_TESTS_FILTER" ]; then
		echo "Tests to run: $UITEST_RUNTIME_TESTS_FILTER"
	fi
fi

cd $SamplesAppArtifactPath
xvfb-run --auto-servernum --server-args='-screen 0 1280x1024x24' sh -c '{ fluxbox & } ; dotnet SamplesApp.Skia.Generic.dll --runtime-tests=$TEST_RESULTS_FILE' || true # sometimes we crash during app shutdown, so we're forcing a 0 exit code

if [ -f "$UITEST_RUNTIME_CURRENT_TEST_FILE" ]; then
	if command -v iconv >/dev/null 2>&1; then
		echo "Last runtime test heartbeat: $(iconv -f utf-16 -t utf-8 "$UITEST_RUNTIME_CURRENT_TEST_FILE")"
	else
		echo "Last runtime test heartbeat: $(cat "$UITEST_RUNTIME_CURRENT_TEST_FILE")"
	fi
else
	echo "No runtime test heartbeat file found."
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
