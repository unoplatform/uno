#!/bin/bash
set -x #echo on
set -euo pipefail
IFS=$'\n\t'

escape_for_xml() {
	printf '%s' "$1" | tr '\r\n' ' ' | \
		sed -e 's/&/\&amp;/g' -e 's/</\&lt;/g' -e 's/>/\&gt;/g' -e 's/"/\&quot;/g' -e "s/'/\&apos;/g"
}

write_heartbeat_xml() {
	local output_path="$1"
	local message="$2"
	local escaped_message
	escaped_message=$(escape_for_xml "$message")
	cat > "$output_path" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<test-run id="0" name="RuntimeHeartbeat" testcasecount="1" result="Skipped" total="1" passed="0" failed="0" skipped="1" inconclusive="0" duration="0">
    <test-suite type="Assembly" name="RuntimeHeartbeat" result="Skipped" total="1" passed="0" failed="0" skipped="1" inconclusive="0" duration="0">
        <test-case id="0-0" name="LastHeartbeat" result="Skipped" duration="0">
            <reason>
                <message>${escaped_message}</message>
            </reason>
        </test-case>
    </test-suite>
</test-run>
EOF
}

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
export RUNTIME_CURRENT_TEST_FILE="$BUILD_SOURCESDIRECTORY/build/runtime-current-test-wasm-$UITEST_RUNTIME_TEST_GROUP.txt"
export UITEST_RUNTIME_TEST_GROUP=${UITEST_RUNTIME_TEST_GROUP:-}
export UNO_TESTS_FAILED_LIST=$BUILD_SOURCESDIRECTORY/build/uitests-failure-results/failed-tests-skia-wasm-runtimetests-$UITEST_RUNTIME_TEST_GROUP-chromium.txt

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

rawurlencode "$RUNTIME_CURRENT_TEST_FILE"
RUNTIME_CURRENT_TEST_FILE_ENCODED=$ENCODED_RESULT

rawurlencode "$UITEST_RUNTIME_TESTS_FILTER"
UITEST_RUNTIME_TESTS_FILTER_ENCODED=$ENCODED_RESULT

RUNTIME_TESTS_URL="http://localhost:8000/?--runtime-tests=${RESULTS_FILE_ENCODED}&--runtime-tests-group=${UITEST_RUNTIME_TEST_GROUP}&--runtime-tests-group-count=${UITEST_RUNTIME_TEST_GROUP_COUNT}&--runtime-test-filter=${UITEST_RUNTIME_TESTS_FILTER_ENCODED}"
RUNTIME_TESTS_URL+="&--runtime-current-test-file=${RUNTIME_CURRENT_TEST_FILE_ENCODED}"

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
    xvfb-run --auto-servernum google-chrome --enable-logging=stderr --no-sandbox "${RUNTIME_TESTS_URL}" &

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

while ! test -f "$RESULTS_FILE"; do
    sleep 10
done

HEARTBEAT_MESSAGE="No runtime test heartbeat file found."
if test -f "$RUNTIME_CURRENT_TEST_FILE"; then
    HEARTBEAT_MESSAGE="$(cat "$RUNTIME_CURRENT_TEST_FILE")"
    echo "Last runtime test heartbeat: $HEARTBEAT_MESSAGE"
else
    echo "No runtime test heartbeat file found."
fi

HEARTBEAT_XML_PATH="$BUILD_SOURCESDIRECTORY/build/runtime-heartbeat-wasm-$UITEST_RUNTIME_TEST_GROUP.xml"
write_heartbeat_xml "$HEARTBEAT_XML_PATH" "$HEARTBEAT_MESSAGE"

## Export the failed tests list for reuse in a pipeline retry
pushd $BUILD_SOURCESDIRECTORY/src/Uno.NUnitTransformTool
mkdir -p $(dirname ${UNO_TESTS_FAILED_LIST})

echo "Running NUnitTransformTool"

## Fail the build when no test results could be read
dotnet run fail-empty $RESULTS_FILE

if [ $? -eq 0 ]; then
	dotnet run list-failed $RESULTS_FILE $UNO_TESTS_FAILED_LIST
fi

popd
