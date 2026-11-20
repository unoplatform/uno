#!/bin/bash
set -euo pipefail
IFS=$'\n\t'

readonly HTTP_PORT=8100
readonly WEBDRIVER_PORT=9515
readonly RESULTS_DIR="$BUILD_SOURCESDIRECTORY/build/appium-wasm-test-results"
readonly FAILURE_DIR="$BUILD_SOURCESDIRECTORY/build/uitests-failure-results/appium-wasm"
readonly TOOLS_DIR="$BUILD_SOURCESDIRECTORY/build/tools"
readonly CHROME_BINARY="$(command -v google-chrome)"
readonly CHROME_VERSION="$(google-chrome --product-version)"
readonly CHROMEDRIVER_DIR="$TOOLS_DIR/chromedriver-$CHROME_VERSION"
readonly CHROMEDRIVER_ZIP="$TOOLS_DIR/chromedriver-$CHROME_VERSION.zip"
readonly CHROMEDRIVER_URL="https://storage.googleapis.com/chrome-for-testing-public/$CHROME_VERSION/linux64/chromedriver-linux64.zip"

mkdir -p "$RESULTS_DIR" "$FAILURE_DIR" "$TOOLS_DIR"

if [ ! -x "$CHROMEDRIVER_DIR/chromedriver-linux64/chromedriver" ]; then
	wget --tries=3 --waitretry=2 --output-document="$CHROMEDRIVER_ZIP" "$CHROMEDRIVER_URL"
	rm -rf "$CHROMEDRIVER_DIR"
	mkdir -p "$CHROMEDRIVER_DIR"
	unzip -q "$CHROMEDRIVER_ZIP" -d "$CHROMEDRIVER_DIR"
	chmod +x "$CHROMEDRIVER_DIR/chromedriver-linux64/chromedriver"
fi

python -m http.server "$HTTP_PORT" -d "$SAMPLESAPPARTIFACTPATH" >"$FAILURE_DIR/http-server.log" 2>&1 &
HTTP_PID=$!
setsid "$CHROMEDRIVER_DIR/chromedriver-linux64/chromedriver" \
	--port="$WEBDRIVER_PORT" \
	--allowed-origins='*' \
	>"$FAILURE_DIR/chromedriver.log" 2>&1 &
WEBDRIVER_PID=$!

cleanup() {
	kill -TERM -- "-$WEBDRIVER_PID" 2>/dev/null || true
	kill -TERM "$HTTP_PID" 2>/dev/null || true
	wait "$WEBDRIVER_PID" "$HTTP_PID" 2>/dev/null || true
}
trap cleanup EXIT

for _ in {1..30}; do
	if curl --fail --silent "http://127.0.0.1:$WEBDRIVER_PORT/status" >/dev/null &&
		curl --fail --silent "http://127.0.0.1:$HTTP_PORT/" >/dev/null; then
		break
	fi
	sleep 1
done

curl --fail --silent "http://127.0.0.1:$WEBDRIVER_PORT/status" >/dev/null
curl --fail --silent "http://127.0.0.1:$HTTP_PORT/" >/dev/null

export UNO_APPIUM_PLATFORM=wasm
export UNO_APPIUM_SAMPLESAPP="http://127.0.0.1:$HTTP_PORT/"
export UNO_APPIUM_SERVER="http://127.0.0.1:$WEBDRIVER_PORT/"
export UNO_APPIUM_CHROME_BINARY="$CHROME_BINARY"
export UNO_APPIUM_CHROME_ARGUMENTS='--headless=new|--no-sandbox|--disable-gpu|--disable-dev-shm-usage'
export UNO_APPIUM_TIMEOUT_SECONDS=30
export UNO_APPIUM_ARTIFACTS_DIR="$FAILURE_DIR"
# Pin the committed baselines to the checkout instead of the compile-time [CallerFilePath],
# so the run keeps working if build and test ever happen on different agents.
export UNO_APPIUM_SNAPSHOTS_DIR="$BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.AppiumTests/Snapshots"

dotnet test \
	--project "$BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.AppiumTests/SamplesApp.AppiumTests.csproj" \
	-c Release \
	-p:UNO_DISABLE_ANALYZERS_IN_SAMPLES=true \
	--filter 'TestCategory=HostRequired|TestCategory=WasmHostRequired' \
	--no-progress \
	--report-trx \
	--results-directory "$RESULTS_DIR"
