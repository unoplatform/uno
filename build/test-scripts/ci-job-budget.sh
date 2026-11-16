#!/usr/bin/env bash
# Sourced by the device test scripts. UNO_JOB_DEADLINE_EPOCH comes from the job's first step
# (build/ci/templates/job-deadline.yml): the job start plus its timeoutInMinutes.

# Prints how many seconds a wait may last: the requested amount, capped so that `reserve` seconds
# are still left before the job timeout. A job that hits its timeout is cancelled without running
# its always() steps, so a hung run would otherwise publish neither results nor device logs.
uno_job_wait_budget() {
	local requested="$1"
	local reserve="$2"

	if [ -z "${UNO_JOB_DEADLINE_EPOCH:-}" ]; then
		echo "$requested"
		return
	fi

	local left=$(( UNO_JOB_DEADLINE_EPOCH - $(date +%s) - reserve ))
	if [ "$left" -lt 0 ]; then
		left=0
	fi

	if [ "$left" -lt "$requested" ]; then
		echo "Capping the ${requested}s wait to ${left}s, keeping ${reserve}s for publishing before the job timeout" >&2
		echo "$left"
	else
		echo "$requested"
	fi
}
