#!/bin/bash
set -x #echo on
set -euo pipefail
IFS=$'\n\t'

export UITEST_RUNTIME_TEST_GROUP=${UITEST_RUNTIME_TEST_GROUP:-}
export UNO_TEST_RESULT_LABEL=${UNO_TEST_RESULT_LABEL:-}

export UNO_TESTS_FAILED_LIST=$BUILD_SOURCESDIRECTORY/build/uitests-failure-results/failed-tests-skia-linux${UNO_TEST_RESULT_LABEL}-runtimetests-$UITEST_RUNTIME_TEST_GROUP.txt
export TEST_RESULTS_FILE=$BUILD_SOURCESDIRECTORY/build/skia-linux${UNO_TEST_RESULT_LABEL}-runtime-tests-results.xml

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

NUNIT_TOOL_DIR="$BUILD_SOURCESDIRECTORY/src/Uno.NUnitTransformTool"
run_nunit_tool() { (cd "$NUNIT_TOOL_DIR" && dotnet run "$@"); }

# Maximum failures to trigger an in-job retry; overridable via env var.
FLAKE_RETRY_MAX_FAILURES=${FLAKE_RETRY_MAX_FAILURES:-20}

# Maximum seconds between heartbeat emissions before the watchdog kills the app.
HEARTBEAT_TIMEOUT_S=${HEARTBEAT_TIMEOUT_S:-300}

run_skia_tests() {
	# $RUNTIME_TESTS_OUTPUT is exported so it is visible in the sh -c subshell below.
	export RUNTIME_TESTS_OUTPUT="$1"
	local HEARTBEAT_LOG
	HEARTBEAT_LOG=$(mktemp /tmp/uno-rt-heartbeat.XXXXXX)
	: > "$HEARTBEAT_LOG"

	cd $SamplesAppArtifactPath
	if [ "$USE_XVFB" = "true" ]; then
		# tee forwards all output to the CI log while grep filters heartbeat markers to HEARTBEAT_LOG.
		xvfb-run --auto-servernum --server-args='-screen 0 1280x1024x24' sh -c '{ fluxbox & } ; dotnet SamplesApp.Skia.Generic.dll --runtime-tests=$RUNTIME_TESTS_OUTPUT' \
			2>&1 | tee >(grep --line-buffered '##UNO-RT-HEARTBEAT##' >> "$HEARTBEAT_LOG") &
	else
		dotnet SamplesApp.Skia.Generic.dll --runtime-tests="$RUNTIME_TESTS_OUTPUT" \
			2>&1 | tee >(grep --line-buffered '##UNO-RT-HEARTBEAT##' >> "$HEARTBEAT_LOG") &
	fi
	local PIPE_PID=$!

	# Watchdog: once the first heartbeat is received, kill if no new heartbeat within HEARTBEAT_TIMEOUT_S.
	while kill -0 $PIPE_PID 2>/dev/null; do
		sleep 10
		if [ -s "$HEARTBEAT_LOG" ]; then
			local HB_AGE=$(( $(date +%s) - $(stat -c %Y "$HEARTBEAT_LOG") ))
			if [ $HB_AGE -gt $HEARTBEAT_TIMEOUT_S ]; then
				echo "##[error]No heartbeat for ${HB_AGE}s — test appears hung. Last heartbeats:"
				tail -3 "$HEARTBEAT_LOG"
				kill -9 $PIPE_PID 2>/dev/null || true
				rm -f "$HEARTBEAT_LOG"
				exit 1
			fi
		fi
	done
	wait $PIPE_PID || true
	rm -f "$HEARTBEAT_LOG"
}

run_skia_tests "$TEST_RESULTS_FILE"

## Export the failed tests list for reuse in a pipeline retry
mkdir -p $(dirname ${UNO_TESTS_FAILED_LIST})

## Fail the build when no test results could be read
run_nunit_tool fail-empty $TEST_RESULTS_FILE

FAILED_COUNT=$(run_nunit_tool count-failed $TEST_RESULTS_FILE | tail -1)
run_nunit_tool list-failed $TEST_RESULTS_FILE $UNO_TESTS_FAILED_LIST

# In-job retry: if a small number of tests failed, rerun only those to catch flakes
if [ "$FAILED_COUNT" -gt 0 ] && [ "$FAILED_COUNT" -le "$FLAKE_RETRY_MAX_FAILURES" ]; then
	echo "##[warning]$FAILED_COUNT test(s) failed — retrying in-job to filter flakes..."
	export UITEST_RUNTIME_TESTS_FILTER=$(cat $UNO_TESTS_FAILED_LIST | base64 -w 0)

	TEST_RESULTS_RERUN_FILE="${TEST_RESULTS_FILE%.xml}-rerun.xml"
	run_skia_tests "$TEST_RESULTS_RERUN_FILE"

	if [ -f "$TEST_RESULTS_RERUN_FILE" ]; then
		run_nunit_tool merge-results $TEST_RESULTS_FILE $TEST_RESULTS_RERUN_FILE $TEST_RESULTS_FILE
		run_nunit_tool list-failed $TEST_RESULTS_FILE $UNO_TESTS_FAILED_LIST
	else
		echo "##[warning]Rerun did not produce a results file; original results kept."
	fi
fi
