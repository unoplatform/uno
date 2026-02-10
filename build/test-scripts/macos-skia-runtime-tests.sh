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

export UITEST_RUNTIME_TEST_GROUP=${UITEST_RUNTIME_TEST_GROUP:-}

export UNO_TESTS_FAILED_LIST=$BUILD_SOURCESDIRECTORY/build/uitests-failure-results/failed-tests-skia-macos-runtimetests-$UITEST_RUNTIME_TEST_GROUP.txt
export TEST_RESULTS_FILE=$BUILD_SOURCESDIRECTORY/build/skia-macos-runtime-tests-results.xml
export UITEST_RUNTIME_CURRENT_TEST_FILE=$BUILD_SOURCESDIRECTORY/build/runtime-current-test-macos-$UITEST_RUNTIME_TEST_GROUP.txt

# CI uses this file to report the last started test when a shard hangs.
export UITEST_RUNTIME_CURRENT_TEST_FILE

if [ -f "$UNO_TESTS_FAILED_LIST" ]; then
	export UITEST_RUNTIME_TESTS_FILTER=`cat $UNO_TESTS_FAILED_LIST | base64 -b 0`

	# echo the failed filter list, if not empty
	if [ -n "$UITEST_RUNTIME_TESTS_FILTER" ]; then
		echo "Tests to run: $UITEST_RUNTIME_TESTS_FILTER"
	fi
fi

cd $SamplesAppArtifactPath
dotnet SamplesApp.Skia.Generic.dll --runtime-tests=$TEST_RESULTS_FILE

HEARTBEAT_MESSAGE="No runtime test heartbeat file found."
if [ -f "$UITEST_RUNTIME_CURRENT_TEST_FILE" ]; then
	if command -v iconv >/dev/null 2>&1; then
		HEARTBEAT_MESSAGE="$(iconv -f utf-16 -t utf-8 "$UITEST_RUNTIME_CURRENT_TEST_FILE")"
	else
		HEARTBEAT_MESSAGE="$(cat "$UITEST_RUNTIME_CURRENT_TEST_FILE")"
	fi
	echo "Last runtime test heartbeat: $HEARTBEAT_MESSAGE"
else
	echo "No runtime test heartbeat file found."
fi

HEARTBEAT_XML_PATH="$BUILD_SOURCESDIRECTORY/build/runtime-heartbeat-macos-$UITEST_RUNTIME_TEST_GROUP.xml"
write_heartbeat_xml "$HEARTBEAT_XML_PATH" "$HEARTBEAT_MESSAGE"

## Export the failed tests list for reuse in a pipeline retry
pushd $BUILD_SOURCESDIRECTORY/src/Uno.NUnitTransformTool
mkdir -p $(dirname ${UNO_TESTS_FAILED_LIST})

echo "Running NUnitTransformTool"

## Fail the build when no test results could be read
dotnet run fail-empty $TEST_RESULTS_FILE

if [ $? -eq 0 ]; then
	dotnet run list-failed $TEST_RESULTS_FILE $UNO_TESTS_FAILED_LIST
fi

popd
