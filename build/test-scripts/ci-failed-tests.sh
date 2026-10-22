#!/usr/bin/env bash
# Sourced by the device test scripts. The publish step is what normally fails a job on failed
# tests, but on iOS it has been finishing without publishing anything, so the run step has to
# fail the job itself.

# Fails when a `list-failed` output of the NUnitTransformTool names any test. The list is a
# single `A | B | invalid-test-for-retry` line whose trailing sentinel is always present.
uno_fail_on_failed_tests() {
	local list="$1"

	if [ ! -f "$list" ]; then
		return 0
	fi

	local failed
	failed=$(tr '|' '\n' < "$list" \
		| sed -e 's/^[[:space:]]*//' -e 's/[[:space:]]*$//' \
		| { grep -v -e '^$' -e '^invalid-test-for-retry$' || true; } \
		| wc -l | tr -d ' ')

	if [ "$failed" -gt 0 ]; then
		echo "##vso[task.logissue type=error]$failed test(s) failed; see the failed tests listed above."
		return 1
	fi
}
