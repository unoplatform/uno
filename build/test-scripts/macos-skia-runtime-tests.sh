#!/bin/bash
set -x #echo on
set -euo pipefail
IFS=$'\n\t'

export UITEST_RUNTIME_TEST_GROUP=${UITEST_RUNTIME_TEST_GROUP:-}

export UNO_TESTS_FAILED_LIST=$BUILD_SOURCESDIRECTORY/build/uitests-failure-results/failed-tests-skia-macos-runtimetests-$UITEST_RUNTIME_TEST_GROUP.txt
export TEST_RESULTS_FILE=$BUILD_SOURCESDIRECTORY/build/skia-macos-runtime-tests-results.xml

if [ -f "$UNO_TESTS_FAILED_LIST" ]; then
	export UITEST_RUNTIME_TESTS_FILTER=`cat $UNO_TESTS_FAILED_LIST | base64 -b 0`

	# echo the failed filter list, if not empty
	if [ -n "$UITEST_RUNTIME_TESTS_FILTER" ]; then
		echo "Tests to run: $UITEST_RUNTIME_TESTS_FILTER"
	fi
fi

NUNIT_TOOL_DIR="$BUILD_SOURCESDIRECTORY/src/Uno.NUnitTransformTool"
run_nunit_tool() { (cd "$NUNIT_TOOL_DIR" && dotnet run "$@"); }

cd $SamplesAppArtifactPath
dotnet SamplesApp.Skia.Generic.dll --runtime-tests=$TEST_RESULTS_FILE

## Export the failed tests list for reuse in a pipeline retry
mkdir -p $(dirname ${UNO_TESTS_FAILED_LIST})

echo "Running NUnitTransformTool"

## Fail the build when no test results could be read
run_nunit_tool fail-empty $TEST_RESULTS_FILE

FAILED_COUNT=$(run_nunit_tool count-failed $TEST_RESULTS_FILE)
run_nunit_tool list-failed $TEST_RESULTS_FILE $UNO_TESTS_FAILED_LIST

# In-job retry: if a small number of tests failed, rerun only those to catch flakes
if [ "$FAILED_COUNT" -gt 0 ] && [ "$FAILED_COUNT" -le 20 ]; then
	echo "##[warning]$FAILED_COUNT test(s) failed — retrying in-job to filter flakes..."
	export UITEST_RUNTIME_TESTS_FILTER=$(cat $UNO_TESTS_FAILED_LIST | base64 -b 0)

	TEST_RESULTS_RERUN_FILE="${TEST_RESULTS_FILE%.xml}-rerun.xml"

	cd $SamplesAppArtifactPath
	dotnet SamplesApp.Skia.Generic.dll --runtime-tests="$TEST_RESULTS_RERUN_FILE" || true

	if [ -f "$TEST_RESULTS_RERUN_FILE" ]; then
		run_nunit_tool merge-results $TEST_RESULTS_FILE $TEST_RESULTS_RERUN_FILE $TEST_RESULTS_FILE
		run_nunit_tool list-failed $TEST_RESULTS_FILE $UNO_TESTS_FAILED_LIST
	else
		echo "##[warning]Rerun did not produce a results file; original results kept."
	fi
fi
