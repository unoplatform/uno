#!/bin/bash
set -x #echo on
set -euo pipefail
IFS=$'\n\t'

if [ `uname` = "Darwin" ]; then
	echo "uname -a:"
	uname -a
	echo "arch:"
	arch
fi

export UITEST_RUNTIME_TEST_GROUP=${UITEST_RUNTIME_TEST_GROUP:-}

export UNO_TESTS_FAILED_LIST=$BUILD_SOURCESDIRECTORY/build/uitests-failure-results/failed-tests-skia-macos-runtimetests-$UITEST_RUNTIME_TEST_GROUP.txt
export TEST_RESULTS_FILE=$BUILD_SOURCESDIRECTORY/build/skia-macos-runtime-tests-results.xml

## Create the failed-tests directory up front: every abort path below (a crashed harness,
## a killed app, a non-zero transform tool) otherwise skips the mkdir and leaves
## `PublishBuildArtifacts@1` retrying a missing PathtoPublish for minutes.
mkdir -p $(dirname ${UNO_TESTS_FAILED_LIST})

if [ -f "$UNO_TESTS_FAILED_LIST" ]; then
	export UITEST_RUNTIME_TESTS_FILTER=`cat $UNO_TESTS_FAILED_LIST | base64 -b 0`

	# echo the failed filter list, if not empty
	if [ -n "$UITEST_RUNTIME_TESTS_FILTER" ]; then
		echo "Tests to run: $UITEST_RUNTIME_TESTS_FILTER"
	fi
fi

export UITEST_DIAGNOSTICS_DIR=$BUILD_SOURCESDIRECTORY/build/uitests-diagnostics
export UNO_WATCHDOG_HARD_TIMEOUT_SECONDS=${UNO_WATCHDOG_HARD_TIMEOUT_SECONDS:-3300}
mkdir -p $UITEST_DIAGNOSTICS_DIR
APP_LOG=$UITEST_DIAGNOSTICS_DIR/runtime-tests-console.log

chmod +x $BUILD_SOURCESDIRECTORY/build/test-scripts/stall-watchdog.sh
$BUILD_SOURCESDIRECTORY/build/test-scripts/stall-watchdog.sh \
	"$APP_LOG" "$UITEST_DIAGNOSTICS_DIR" "dotnet.*SamplesApp\.dll" &
WATCHDOG_PID=$!
trap 'kill $WATCHDOG_PID 2>/dev/null || true' EXIT

cd $SamplesAppArtifactPath

mkdir -p "$BUILD_SOURCESDIRECTORY/build/uitests-failure-results"

# https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/xplat-minidump-generation.md#configurationpolicy
export DOTNET_DbgEnableMiniDump=1
export DOTNET_DbgMiniDumpName="$BUILD_SOURCESDIRECTORY/build/uitests-failure-results/coredump-macos.%p"
export DOTNET_CreateDumpDiagnostics=1
export DOTNET_CreateDumpLogToFile="$BUILD_SOURCESDIRECTORY/build/uitests-failure-results/createdump-macos.log"
export DOTNET_EnableCrashReport=1

## The app exit code must not abort the script: the failed-test list below is what lets a
## pipeline retry run only the failures instead of the whole suite.
set +e
dotnet SamplesApp.dll --runtime-tests=$TEST_RESULTS_FILE 2>&1 | tee "$APP_LOG"
APP_EXIT=${PIPESTATUS[0]}
set -e

kill $WATCHDOG_PID 2>/dev/null || true
echo "Runtime tests app exited with code $APP_EXIT"

## Export the failed tests list for reuse in a pipeline retry
pushd $BUILD_SOURCESDIRECTORY/src/Uno.NUnitTransformTool

echo "Running NUnitTransformTool"

## Fail the build when no test results could be read
dotnet run fail-empty $TEST_RESULTS_FILE

if [ $? -eq 0 ]; then
	dotnet run list-failed $TEST_RESULTS_FILE $UNO_TESTS_FAILED_LIST
fi

popd
