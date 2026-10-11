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

# A label (e.g. -webgpu) keeps a variant lane's results and retry list apart from the default lane's.
export UNO_TEST_RESULT_LABEL=${UNO_TEST_RESULT_LABEL:-}
export UNO_TESTS_FAILED_LIST=$BUILD_SOURCESDIRECTORY/build/uitests-failure-results/failed-tests-skia-macos${UNO_TEST_RESULT_LABEL}-runtimetests-$UITEST_RUNTIME_TEST_GROUP.txt
export TEST_RESULTS_FILE=$BUILD_SOURCESDIRECTORY/build/skia-macos${UNO_TEST_RESULT_LABEL}-runtime-tests-results.xml

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

cd $SamplesAppArtifactPath

mkdir -p "$BUILD_SOURCESDIRECTORY/build/uitests-failure-results"

# https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/xplat-minidump-generation.md#configurationpolicy
export DOTNET_DbgEnableMiniDump=1
export DOTNET_DbgMiniDumpName="$BUILD_SOURCESDIRECTORY/build/uitests-failure-results/coredump-macos.%p"
export DOTNET_CreateDumpDiagnostics=1
export DOTNET_CreateDumpLogToFile="$BUILD_SOURCESDIRECTORY/build/uitests-failure-results/createdump-macos.log"
export DOTNET_EnableCrashReport=1

## The runtime's crash report only symbolizes managed frames. macOS writes its own .ips report, with
## symbolized AppKit/Foundation frames and the faulting address, so collect it when the app dies.
CRASH_REPORTS_MARKER=$(mktemp)

set +e
dotnet SamplesApp.dll --runtime-tests=$TEST_RESULTS_FILE
APP_EXIT_CODE=$?
set -e

if [ $APP_EXIT_CODE -ne 0 ]; then
	echo "SamplesApp exited with code $APP_EXIT_CODE, collecting macOS crash reports"

	# ReportCrash writes the report asynchronously after the process is gone.
	for i in $(seq 1 30); do
		if [ -n "$(find ~/Library/Logs/DiagnosticReports -newer "$CRASH_REPORTS_MARKER" -type f 2>/dev/null)" ]; then
			break
		fi
		sleep 1
	done

	find ~/Library/Logs/DiagnosticReports -newer "$CRASH_REPORTS_MARKER" -type f -exec cp {} "$BUILD_SOURCESDIRECTORY/build/uitests-failure-results/" \; 2>/dev/null || true
	exit $APP_EXIT_CODE
fi

## Export the failed tests list for reuse in a pipeline retry
pushd $BUILD_SOURCESDIRECTORY/src/Uno.NUnitTransformTool

echo "Running NUnitTransformTool"

## Fail the build when no test results could be read
dotnet run fail-empty $TEST_RESULTS_FILE

if [ $? -eq 0 ]; then
	dotnet run list-failed $TEST_RESULTS_FILE $UNO_TESTS_FAILED_LIST
fi

popd
