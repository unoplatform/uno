#!/usr/bin/env bash
# Sourced by the runtime test scripts. Re-runs the failed tests once, in a fresh app process, within
# the same job: the failures are mostly state leaking from earlier tests of the shard, and a stage
# retry costs a new agent, the setup and the downstream stages all over again.
#
# The script sets UNO_RERUN_FIRST_PASS=false when it runs from a failed-tests list already (a stage
# retry), then calls:
#   uno_rerun_prepare <results.xml> <reserve-seconds>   # succeeds when a re-run is worth it
#   ...relaunch the app with $UNO_RERUN_FILTER, writing <rerun.xml>, bounded by $UNO_RERUN_TIMEOUT
#      (uno_rerun_run does that for a process that exits once the tests are done)...
#   uno_rerun_merge <results.xml> <rerun.xml>
# and lists the failed tests from <results.xml> as before.

source "$BUILD_SOURCESDIRECTORY/build/test-scripts/ci-job-budget.sh"

UNO_RERUN_TOOL="$BUILD_SOURCESDIRECTORY/src/Uno.NUnitTransformTool"

# More failures than this is a real break, not flakiness: re-running them only burns the job budget.
UNO_RERUN_MAX_FAILURES=${UNO_RERUN_MAX_FAILURES:-30}
UNO_RERUN_MAX_SECONDS=${UNO_RERUN_MAX_SECONDS:-600}

# Run from the tool's directory, like the rest of the scripts do, so the repository's global.json applies.
uno_rerun_tool() {
	(cd "$UNO_RERUN_TOOL" && dotnet run -- "$@")
}

# Sets UNO_RERUN_FILTER (base64, for UITEST_RUNTIME_TESTS_FILTER) and UNO_RERUN_TIMEOUT (seconds).
uno_rerun_prepare() {
	local results="$1"
	local reserve="$2"

	UNO_RERUN_FILTER=""
	UNO_RERUN_TIMEOUT=0

	if [ "${UNO_RERUN_FIRST_PASS:-true}" != "true" ] || [ ! -s "$results" ]; then
		return 1
	fi

	local filter_file
	filter_file=$(mktemp)
	if ! uno_rerun_tool rerun-filter "$results" "$UNO_RERUN_MAX_FAILURES" "$filter_file" || [ ! -s "$filter_file" ]; then
		rm -f "$filter_file"
		return 1
	fi
	UNO_RERUN_FILTER=$(cat "$filter_file")
	rm -f "$filter_file"

	UNO_RERUN_TIMEOUT=$(uno_job_wait_budget "$UNO_RERUN_MAX_SECONDS" "$reserve")
	if [ "$UNO_RERUN_TIMEOUT" -lt 120 ]; then
		echo "##vso[task.logissue type=warning]Not re-running the failed tests: only ${UNO_RERUN_TIMEOUT}s are left before the job timeout."
		return 1
	fi

	echo "Re-running the failed tests in a fresh app process, for at most ${UNO_RERUN_TIMEOUT}s"
}

# Runs the given command, terminating it after $UNO_RERUN_TIMEOUT seconds. macOS ships no timeout(1).
# Always succeeds: an app crashing on shutdown is common, whether the re-run counts is up to its results.
uno_rerun_run() {
	"$@" &
	local pid=$!
	local end_time=$((SECONDS + UNO_RERUN_TIMEOUT))

	while kill -0 "$pid" 2>/dev/null; do
		if [ "$SECONDS" -ge "$end_time" ]; then
			echo "##vso[task.logissue type=warning]The failed tests re-run did not finish within ${UNO_RERUN_TIMEOUT}s."
			kill -TERM "$pid" 2>/dev/null || true
			break
		fi
		sleep 5
	done

	wait "$pid" || true
}

# Folds the re-run results into <results.xml>. A re-run that produced nothing leaves the first pass as is.
uno_rerun_merge() {
	local results="$1"
	local rerun_results="$2"

	if [ ! -s "$rerun_results" ]; then
		echo "##vso[task.logissue type=warning]The failed tests re-run produced no results; keeping the first run's failures."
		return 0
	fi

	if ! uno_rerun_tool merge-rerun "$results" "$rerun_results" "$results"; then
		echo "##vso[task.logissue type=warning]Could not merge the failed tests re-run results; keeping the first run's failures."
	fi
}
