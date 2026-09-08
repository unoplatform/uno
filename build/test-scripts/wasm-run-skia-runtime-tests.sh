#!/bin/bash
set -x #echo on
set -euo pipefail
IFS=$'\n\t'

# https://github.com/sfinktah/bash/blob/master/rawurlencode.inc.sh
rawurlencode() {
    local string="${1}"
    local strlen=${#string}
    local encoded=""
    local pos c o

    for (( pos=0 ; pos<strlen ; pos++ )); do
        c=${string:$pos:1}
        case "$c" in
            [-_.~a-zA-Z0-9] ) o="${c}" ;;
            * )               printf -v o '%%%02x' "'$c"
        esac
        encoded+="${o}"
    done
    ENCODED_RESULT="${encoded}"
}

# For Skia-WASM, SamplesApp is set up so that when saving files, it
# sends a POST request at HOSTNAME:PORT+1 where HOSTNAME and PORT are
# the hostname and port of the server that serves the SamplesApp
python -m http.server 8000 -d "$SAMPLESAPPARTIFACTPATH" &
python $BUILD_SOURCESDIRECTORY/build/test-scripts/skia-browserwasm-file-creation-server.py 8001 &
sleep 10

export RESULTS_FILE="$BUILD_SOURCESDIRECTORY/build/skia-browserwasm-runtime-tests-results.xml"
export RESULTS_CANARY_FILE="$RESULTS_FILE.canary"
export UITEST_RUNTIME_TEST_GROUP=${UITEST_RUNTIME_TEST_GROUP:-}
export UNO_TESTS_FAILED_LIST=$BUILD_SOURCESDIRECTORY/build/uitests-failure-results/failed-tests-skia-wasm-runtimetests-$UITEST_RUNTIME_TEST_GROUP-chromium.txt

## The pipeline arms this as "false" before the dependency install; flipping it here tells the
## publish tasks the test step actually started, so they only report a missing results file
## when there is a real harness failure rather than a killed job.
echo "##vso[task.setvariable variable=UNO_TESTS_STEP_RAN]true"

## Create the failed-tests directory up front: every abort path below (a crashed harness,
## a killed app, a non-zero transform tool) otherwise skips the mkdir and leaves
## `PublishBuildArtifacts@1` retrying a missing PathtoPublish for minutes.
mkdir -p $(dirname ${UNO_TESTS_FAILED_LIST})

if [ -f "$UNO_TESTS_FAILED_LIST" ]; then
	export UITEST_RUNTIME_TESTS_FILTER=`cat $UNO_TESTS_FAILED_LIST | base64 -w 0`

    # Replace the `=` with `!` to avoid url encoding issues
    UITEST_RUNTIME_TESTS_FILTER=${UITEST_RUNTIME_TESTS_FILTER//=/!}

	# echo the failed filter list, if not empty
	if [ -n "$UNO_TESTS_FAILED_LIST" ]; then
		echo "Tests to run: $UNO_TESTS_FAILED_LIST"
	fi
else
    export UITEST_RUNTIME_TESTS_FILTER=""
fi

rawurlencode "$RESULTS_FILE"
RESULTS_FILE_ENCODED=$ENCODED_RESULT

rawurlencode "$UITEST_RUNTIME_TESTS_FILTER"
UITEST_RUNTIME_TESTS_FILTER_ENCODED=$ENCODED_RESULT

RUNTIME_TESTS_URL="http://localhost:8000/?--runtime-tests=${RESULTS_FILE_ENCODED}&--runtime-tests-group=${UITEST_RUNTIME_TEST_GROUP}&--runtime-tests-group-count=${UITEST_RUNTIME_TEST_GROUP_COUNT}&--runtime-test-filter=${UITEST_RUNTIME_TESTS_FILTER_ENCODED}"

TRY_COUNT=0

while [ $TRY_COUNT -lt 5 ]; do
    # we use xvfb instead of headless chrome because using --enable-logging with --headless doesn't
    # print the logs as expected
    # for some reason, you have to run the next line twice or else it doesn't work
    killall -9 chrome || true
    killall -9 xvfb-run || true
    killall -9 Xvfb || true
    killall -9 chrome_crashpad_handler || true
    # We now launch a fluxbox window manager alongside chrome (see below); kill any stray instance
    # from a previous attempt so retries do not accumulate fluxbox processes.
    killall -9 fluxbox || true
    rm -fr /tmp/.X99-lock || true
    # Under xvfb the browser needs a real screen (size + 24-bit depth) AND a window manager, otherwise
    # the window is treated as background/zero-size and Chromium throttles requestAnimationFrame/timers
    # to ~1Hz, stalling render-loop-driven scroll/BringIntoView/virtualization animations -> the runtime
    # tests that wait on them time out (flaky WASM-Skia). Mirror linux-skia-runtime-tests.sh: define a
    # screen, run a window manager (fluxbox), keep a visible window size, and disable bg throttling.
    # (fluxbox degrades gracefully: if it is unavailable the backgrounded launch is a no-op and chrome
    # still runs.) --autoplay-policy lifts the gesture requirement on HTMLMediaElement.play(), which
    # otherwise rejects with NotAllowedError and stalls every media playback test. The URL is passed
    # as a positional arg to avoid re-quoting its '&'/'='/'?' chars.
    # --no-first-run/--no-default-browser-check/--disable-search-engine-choice-screen stop the first-run
    # experience from swallowing the command-line URL on the agent's brand-new profile: without them
    # chrome starts but never navigates, so the canary never appears.
    xvfb-run --auto-servernum --server-args='-screen 0 1920x1080x24' sh -c '{ fluxbox >/dev/null 2>&1 & } ; google-chrome --enable-logging=stderr --no-sandbox --no-first-run --no-default-browser-check --disable-search-engine-choice-screen --disable-background-timer-throttling --disable-renderer-backgrounding --disable-backgrounding-occluded-windows --autoplay-policy=no-user-gesture-required --window-size=1920,1080 "$1"' _ "${RUNTIME_TESTS_URL}" &

    # wait one minute for the canary file to be created, otherwise fail the script.
    # This may happen if xvfb-run of chrome fails to start
    for i in {1..6}; do
        if test -f "$RESULTS_CANARY_FILE"; then
            break
        fi
        sleep 10
    done

    # if the canary file exists, continue
    if test -f "$RESULTS_CANARY_FILE"; then
        break
    fi

    TRY_COUNT=$((TRY_COUNT+1))
    echo "Canary file not found. retrying... (Tried $TRY_COUNT times)"
done

# if the canary file does not exist show a message and exit
if ! test -f "$RESULTS_CANARY_FILE"; then
    echo "Canary file not found. The app may not have started? Exiting."
    exit 1
fi

# Bound the wait: if the browser started (the canary exists) but the run never produces a
# results file, this loop otherwise spins until the 60-minute job timeout kills the job, which
# reports as an opaque agent timeout rather than as a stalled test run.
RESULTS_WAIT_SECONDS=2100
WAITED=0
while ! test -f "$RESULTS_FILE"; do
    if [ $WAITED -ge $RESULTS_WAIT_SECONDS ]; then
        echo "##vso[task.logissue type=error]UNOBLD005: The runtime tests did not produce $RESULTS_FILE within $((RESULTS_WAIT_SECONDS / 60)) minutes. The app started (the canary file exists) but the run never completed."
        exit 1
    fi
    sleep 10
    WAITED=$((WAITED + 10))
done

## Export the failed tests list for reuse in a pipeline retry
pushd $BUILD_SOURCESDIRECTORY/src/Uno.NUnitTransformTool

echo "Running NUnitTransformTool"

## Fail the build when no test results could be read
dotnet run fail-empty $RESULTS_FILE

if [ $? -eq 0 ]; then
	dotnet run list-failed $RESULTS_FILE $UNO_TESTS_FAILED_LIST
fi

popd
