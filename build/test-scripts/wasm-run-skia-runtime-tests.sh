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

RESULTS_FILE="$BUILD_SOURCESDIRECTORY/build/skia-browserwasm-runtime-tests-results.xml"
RESULTS_CANARY_FILE="$RESULTS_FILE.canary"
UITEST_RUNTIME_TEST_GROUP=${UITEST_RUNTIME_TEST_GROUP:-}

rawurlencode "$RESULTS_FILE"

RUNTIME_TESTS_URL="http://localhost:8000/?--runtime-tests=${ENCODED_RESULT}&--runtime-tests-group=${UITEST_RUNTIME_TEST_GROUP}&--runtime-tests-group-count=${UITEST_RUNTIME_TEST_GROUP_COUNT}"

TRY_COUNT=0

while [ $TRY_COUNT -lt 5 ]; do
    # we use xvfb instead of headless chrome because using --enable-logging with --headless doesn't
    # print the logs as expected
    # for some reason, you have to run the next line twice or else it doesn't work
    killall -9 chrome || true
    killall -9 xvfb-run || true
    killall -9 Xvfb || true
    killall -9 chrome_crashpad_handler || true
    rm -fr /tmp/.X99-lock || true
    xvfb-run google-chrome --enable-logging=stderr -e /dev/stdout --auto-servernum --no-sandbox "${RUNTIME_TESTS_URL}" &

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

while ! test -f "$BUILD_SOURCESDIRECTORY/build/skia-browserwasm-runtime-tests-results.xml"; do
    sleep 10
done
