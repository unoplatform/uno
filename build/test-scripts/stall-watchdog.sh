#!/bin/bash

# Background watchdog for the desktop Skia runtime tests.
#
# Watches the runtime test log for progress. When no new test starts for longer than the stall
# threshold, it captures native and managed stacks of the test process so a frozen run leaves
# evidence instead of being killed silently by the job timeout.
#
# Progress is the count of test-start lines, not the log's mtime: the in-process stall monitor
# writes a heartbeat to this same log every 30s, so by mtime the file is never quiet and no
# stall could ever be captured.
#
# Usage: stall-watchdog.sh <log-file> <output-dir> <pgrep-pattern>
#
# Environment:
#   UNO_WATCHDOG_STALL_SECONDS         no-progress period before a capture (default 180)
#   UNO_WATCHDOG_POLL_SECONDS          poll interval                       (default 15)
#   UNO_WATCHDOG_MAX_CAPTURES          cap on captures per run             (default 6)
#   UNO_WATCHDOG_HARD_TIMEOUT_SECONDS  terminate the app after this        (default 0 = never)
#   UNO_WATCHDOG_PROGRESS_PATTERN      line that marks progress            (default 'Running test ')
#
# Deliberately never exits non-zero: this observes a run, it must not be able to fail one.

set -uo pipefail

LOG_FILE="${1:?log file required}"
OUT_DIR="${2:?output dir required}"
PROC_PATTERN="${3:?process pattern required}"

STALL_SECONDS=${UNO_WATCHDOG_STALL_SECONDS:-180}
POLL_SECONDS=${UNO_WATCHDOG_POLL_SECONDS:-15}
MAX_CAPTURES=${UNO_WATCHDOG_MAX_CAPTURES:-6}
HARD_TIMEOUT_SECONDS=${UNO_WATCHDOG_HARD_TIMEOUT_SECONDS:-0}
PROGRESS_PATTERN=${UNO_WATCHDOG_PROGRESS_PATTERN:-Running test }

mkdir -p "$OUT_DIR"
export PATH="$PATH:$HOME/.dotnet/tools"

log() { echo "[watchdog] $*"; }

# Number of tests started so far. Re-scanning the whole log each poll costs little next to a
# 15s interval, and needs no state to survive the app being restarted mid-run.
progress_count() {
	local f=$1
	[ -f "$f" ] || { echo 0; return; }
	grep -c "$PROGRESS_PATTERN" "$f" 2>/dev/null || echo 0
}

install_tools() {
	# Best effort: the watchdog still reports ps/sample output without these.
	if ! command -v dotnet-stack >/dev/null 2>&1; then
		log "installing dotnet-stack..."
		dotnet tool install -g dotnet-stack >/dev/null 2>&1 || log "dotnet-stack install failed (continuing)"
	fi
}

capture() {
	local reason=$1 index=$2 pid=$3
	local dir="$OUT_DIR/stall-$(printf '%02d' "$index")-$(date +%Y%m%d-%H%M%S)"
	mkdir -p "$dir"

	log "capturing diagnostics ($reason) for pid $pid -> $dir"

	{
		echo "reason:        $reason"
		echo "captured at:   $(date -u +%Y-%m-%dT%H:%M:%SZ)"
		echo "pid:           $pid"
		echo "tests started so far: $(progress_count "$LOG_FILE")"
		echo "uname:         $(uname -a)"
	} > "$dir/context.txt" 2>&1

	tail -n 60 "$LOG_FILE" > "$dir/log-tail.txt" 2>&1 || true
	ps -o pid,ppid,stat,%cpu,%mem,etime,command -p "$pid" > "$dir/ps.txt" 2>&1 || true

	# Managed stacks — the primary artefact for a managed deadlock or a hung await.
	if command -v dotnet-stack >/dev/null 2>&1; then
		timeout 120 dotnet-stack report --process-id "$pid" > "$dir/dotnet-stack.txt" 2>&1 \
			|| log "dotnet-stack failed (see $dir/dotnet-stack.txt)"
	fi

	case "$(uname -s)" in
		Darwin)
			# Native stacks for every thread, including AppKit/CoreAnimation frames that
			# managed stacks cannot show. This is the artefact for a host-level livelock.
			timeout 120 sample "$pid" 10 -f "$dir/sample.txt" >/dev/null 2>&1 \
				|| log "sample failed"
			timeout 60 vmmap --summary "$pid" > "$dir/vmmap.txt" 2>&1 || true
			;;
		Linux)
			if command -v eu-stack >/dev/null 2>&1; then
				timeout 120 eu-stack -p "$pid" > "$dir/eu-stack.txt" 2>&1 || true
			fi
			cat "/proc/$pid/status" > "$dir/proc-status.txt" 2>&1 || true
			cat "/proc/$pid/wchan" > "$dir/proc-wchan.txt" 2>&1 || true
			for t in /proc/"$pid"/task/*; do
				[ -d "$t" ] || continue
				{ echo "== $t"; cat "$t/stat" 2>/dev/null; cat "$t/wchan" 2>/dev/null; echo; }
			done > "$dir/proc-threads.txt" 2>&1 || true
			;;
	esac

	log "capture complete: $dir"
}

find_pid() {
	pgrep -f "$PROC_PATTERN" 2>/dev/null | head -n 1
}

install_tools
log "watching '$LOG_FILE' (stall=${STALL_SECONDS}s poll=${POLL_SECONDS}s maxCaptures=${MAX_CAPTURES} hardTimeout=${HARD_TIMEOUT_SECONDS}s)"

STARTED_AT=$(date +%s)
CAPTURES=0
CAPTURED_THIS_STALL=0
LAST_PROGRESS=-1
LAST_PROGRESS_AT=$STARTED_AT

while true; do
	sleep "$POLL_SECONDS"

	PID=$(find_pid)
	if [ -z "$PID" ]; then
		# App not started yet, or already gone. Either way there is nothing to sample.
		continue
	fi

	NOW=$(date +%s)
	PROGRESS=$(progress_count "$LOG_FILE")
	if [ "$PROGRESS" != "$LAST_PROGRESS" ]; then
		LAST_PROGRESS=$PROGRESS
		LAST_PROGRESS_AT=$NOW
		# The run moved on — re-arm so the next stall is captured too.
		CAPTURED_THIS_STALL=0
	fi

	AGE=$(( NOW - LAST_PROGRESS_AT ))
	ELAPSED=$(( NOW - STARTED_AT ))

	if [ "$AGE" -ge "$STALL_SECONDS" ]; then
		if [ "$CAPTURED_THIS_STALL" -eq 0 ] && [ "$CAPTURES" -lt "$MAX_CAPTURES" ]; then
			CAPTURES=$(( CAPTURES + 1 ))
			CAPTURED_THIS_STALL=1
			capture "no test started for ${AGE}s" "$CAPTURES" "$PID"
		fi
	fi

	if [ "$HARD_TIMEOUT_SECONDS" -gt 0 ] && [ "$ELAPSED" -ge "$HARD_TIMEOUT_SECONDS" ]; then
		log "hard timeout reached after ${ELAPSED}s — capturing then terminating pid $PID"
		CAPTURES=$(( CAPTURES + 1 ))
		capture "hard timeout after ${ELAPSED}s" "$CAPTURES" "$PID"

		# SIGTERM first so the app can still write its results XML, then escalate.
		kill -TERM "$PID" 2>/dev/null || true
		for _ in $(seq 1 30); do
			kill -0 "$PID" 2>/dev/null || break
			sleep 1
		done
		kill -KILL "$PID" 2>/dev/null || true
		log "terminated pid $PID"
		exit 0
	fi
done
