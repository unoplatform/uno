#!/bin/bash
set -x #echo on
set -euo pipefail
IFS=$'\n\t'

export UITEST_RUNTIME_TEST_GROUP=${UITEST_RUNTIME_TEST_GROUP:-}
export UNO_TEST_RESULT_LABEL=${UNO_TEST_RESULT_LABEL:-}

export UNO_TESTS_FAILED_LIST=$BUILD_SOURCESDIRECTORY/build/uitests-failure-results/failed-tests-skia-linux${UNO_TEST_RESULT_LABEL}-runtimetests-$UITEST_RUNTIME_TEST_GROUP.txt
export TEST_RESULTS_FILE=$BUILD_SOURCESDIRECTORY/build/skia-linux${UNO_TEST_RESULT_LABEL}-runtime-tests-results.xml

## The pipeline arms this as "false" before the dependency install; flipping it here tells the
## publish tasks the test step actually started, so they only report a missing results file
## when there is a real harness failure rather than a killed job.
echo "##vso[task.setvariable variable=UNO_TESTS_STEP_RAN]true"

## Create the failed-tests directory up front: every abort path below (a crashed harness,
## a killed app, a non-zero transform tool) otherwise skips the mkdir and leaves
## `PublishBuildArtifacts@1` retrying a missing PathtoPublish for minutes.
mkdir -p "$(dirname "$UNO_TESTS_FAILED_LIST")"

if [ -f "$UNO_TESTS_FAILED_LIST" ]; then
	export UITEST_RUNTIME_TESTS_FILTER=`cat $UNO_TESTS_FAILED_LIST | base64 -w 0`

	# echo the failed filter list, if not empty
	if [ -n "$UITEST_RUNTIME_TESTS_FILTER" ]; then
		echo "Tests to run: $UITEST_RUNTIME_TESTS_FILTER"
	fi
fi

USE_XVFB=${1:-true}
USE_WESTON=${2:-false}

# Grant access to DRM and framebuffer devices if they exist, so the app
# can use hardware rendering instead of falling back to headless mode.
sudo chmod a+rw /dev/dri/card* 2>/dev/null || true
sudo chmod a+rw /dev/fb* 2>/dev/null || true

cd "$SamplesAppArtifactPath"
if [ "$USE_WESTON" = "true" ]; then
	# Keep the compositor and all clients isolated from the agent's desktop session.
	XDG_RUNTIME_DIR="$(mktemp -d)"
	export XDG_RUNTIME_DIR
	WESTON_PID=
	cleanup_wayland()
	{
		if [ -n "$WESTON_PID" ]; then
			kill "$WESTON_PID" 2>/dev/null || true
			wait "$WESTON_PID" 2>/dev/null || true
		fi
		rm -rf "$XDG_RUNTIME_DIR"
	}
	trap cleanup_wayland EXIT
	trap 'exit 130' INT
	trap 'exit 143' TERM
	chmod 700 "$XDG_RUNTIME_DIR"
	unset DISPLAY
	export WAYLAND_DISPLAY=uno-runtime-tests
	export UNO_EXPECT_HOST=Uno.UI.Runtime.Wayland
	WESTON_LOG="$BUILD_SOURCESDIRECTORY/build/uitests-failure-results/weston${UNO_TEST_RESULT_LABEL}.log"
	weston --backend=headless --socket="$WAYLAND_DISPLAY" --width=1280 --height=1024 --scale=1 --idle-time=0 --log="$WESTON_LOG" &
	WESTON_PID=$!
	WESTON_READY=false
	for i in $(seq 1 50); do
		if ! kill -0 "$WESTON_PID" 2>/dev/null; then
			break
		fi
		if [ -S "$XDG_RUNTIME_DIR/$WAYLAND_DISPLAY" ]; then
			WESTON_READY=true
			break
		fi
		sleep 0.2
	done
	if [ "$WESTON_READY" != true ]; then
		echo "Headless Weston failed to become ready" >&2
		cat "$WESTON_LOG" >&2 || true
		exit 1
	fi

	# Runtime results can survive a shutdown crash. The separate smoke must exit cleanly.
	LIFECYCLE_LOG="$BUILD_SOURCESDIRECTORY/build/uitests-failure-results/wayland-lifecycle${UNO_TEST_RESULT_LABEL}.log"
	UNO_WAYLAND_LIFECYCLE_SMOKE=1 timeout 60s dotnet SamplesApp.dll sample=Windowing/Window_DesktopScenarios > "$LIFECYCLE_LOG" 2>&1
	cat "$LIFECYCLE_LOG"
	grep -q '^SamplesApp host: Uno.UI.Runtime.Wayland$' "$LIFECYCLE_LOG"
	grep -q '^Wayland lifecycle: windows closed$' "$LIFECYCLE_LOG"
	grep -q '^Wayland lifecycle: host stopped$' "$LIFECYCLE_LOG"
	RUNTIME_LOG="$BUILD_SOURCESDIRECTORY/build/uitests-failure-results/wayland-runtime${UNO_TEST_RESULT_LABEL}.log"
	dotnet SamplesApp.dll --runtime-tests="$TEST_RESULTS_FILE" > "$RUNTIME_LOG" 2>&1 || true
	cat "$RUNTIME_LOG"
	grep -q '^SamplesApp host: Uno.UI.Runtime.Wayland$' "$RUNTIME_LOG"
elif [ "$USE_XVFB" = "true" ]; then
	xvfb-run --auto-servernum --server-args='-screen 0 1280x1024x24' sh -c '{ fluxbox & } ; dotnet SamplesApp.dll --runtime-tests=$TEST_RESULTS_FILE' || true # sometimes we crash during app shutdown, so we're forcing a 0 exit code
else
	dotnet SamplesApp.dll --runtime-tests=$TEST_RESULTS_FILE || true
fi

## Export the failed tests list for reuse in a pipeline retry
pushd $BUILD_SOURCESDIRECTORY/src/Uno.NUnitTransformTool

echo "Running NUnitTransformTool"

## Fail the build when no test results could be read
dotnet run fail-empty $TEST_RESULTS_FILE

if [ $? -eq 0 ]; then
	dotnet run list-failed $TEST_RESULTS_FILE $UNO_TESTS_FAILED_LIST
fi

popd
