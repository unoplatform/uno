#!/bin/bash
set -x #echo on
set -euo pipefail
IFS=$'\n\t'

cd $BUILD_SOURCESDIRECTORY/build/wasm-uitest-binaries

npm i chromedriver@127.0.0
npm i puppeteer@22.14.0

# Download chromium explicitly
pushd ./node_modules/puppeteer
npm install
popd

# install dotnet serve / Remove as needed
dotnet tool uninstall dotnet-serve -g || true
dotnet tool uninstall dotnet-serve --tool-path $BUILD_SOURCESDIRECTORY/build/tools || true
dotnet tool install dotnet-serve --version 1.10.140 --tool-path $BUILD_SOURCESDIRECTORY/build/tools || true
export PATH="$PATH:$BUILD_SOURCESDIRECTORY/build/tools"

export UNO_UITEST_TARGETURI=http://localhost:8000
export UNO_UITEST_DRIVERPATH_CHROME=$BUILD_SOURCESDIRECTORY/build/wasm-uitest-binaries/node_modules/chromedriver/lib/chromedriver
export UNO_UITEST_CHROME_BINARY_PATH=~/.cache/puppeteer/chrome/linux-127.0.6533.72/chrome-linux64/chrome
export UITEST_RUNTIME_TEST_GROUP=${UITEST_RUNTIME_TEST_GROUP=automated}
export UNO_UITEST_SCREENSHOT_PATH=$BUILD_ARTIFACTSTAGINGDIRECTORY/screenshots/wasm-automated-$SITE_SUFFIX-$UITEST_AUTOMATED_GROUP-$UITEST_RUNTIME_TEST_GROUP
export UNO_UITEST_PLATFORM=Browser
export UNO_UITEST_BENCHMARKS_PATH=$BUILD_ARTIFACTSTAGINGDIRECTORY/benchmarks/wasm-automated
export UNO_UITEST_RUNTIMETESTS_RESULTS_FILE_PATH=$BUILD_SOURCESDIRECTORY/build/RuntimeTestResults-wasm-automated-$SITE_SUFFIX.xml
export UNO_TESTS_LOCAL_TESTS_FILE=$BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.UITests
export UNO_ORIGINAL_TEST_RESULTS_DIRECTORY=$BUILD_SOURCESDIRECTORY/build
export UNO_ORIGINAL_TEST_RESULTS=$UNO_ORIGINAL_TEST_RESULTS_DIRECTORY/TestResult-original.xml
export UNO_TESTS_FAILED_LIST=$BUILD_SOURCESDIRECTORY/build/uitests-failure-results/failed-tests-wasm-automated-$SITE_SUFFIX-$UITEST_AUTOMATED_GROUP-$UITEST_RUNTIME_TEST_GROUP-chromium.txt
export UNO_RUNTIME_TESTS_FAILED_LIST=$BUILD_SOURCESDIRECTORY/build/uitests-failure-results/failed-tests-wasm-runtimetest-$SITE_SUFFIX-$UITEST_AUTOMATED_GROUP-$UITEST_RUNTIME_TEST_GROUP-chromium.txt
export UNO_TESTS_RESPONSE_FILE=$BUILD_SOURCESDIRECTORY/build/nunit.response

if [ "$UITEST_AUTOMATED_GROUP" == 'Default' ];
then
	export TEST_FILTERS=" \
		FullyQualifiedName !~ SamplesApp.UITests.Snap \
		& FullyQualifiedName !~ SamplesApp.UITests.Runtime.RuntimeTests \
		& FullyQualifiedName !~ SamplesApp.UITests.Runtime.BenchmarkDotNetTests \
"

elif [ "$UITEST_AUTOMATED_GROUP" == 'RuntimeTests' ];
then
		export TEST_FILTERS="FullyQualifiedName ~ SamplesApp.UITests.Runtime.RuntimeTests"

elif [ "$UITEST_AUTOMATED_GROUP" == 'Benchmarks' ];
then
		export TEST_FILTERS="FullyQualifiedName ~ SamplesApp.UITests.Runtime.BenchmarkDotNetTests"
fi

if [ -f "$UNO_TESTS_FAILED_LIST" ] && [ `cat "$UNO_TESTS_FAILED_LIST"` = "invalid-test-for-retry" ]; then
	# The test results file only contains the re-run marker and no
	# other test to rerun. We can skip this run.
	echo "The file $UNO_TESTS_FAILED_LIST does not contain tests to re-run, skipping."
	exit 0
fi


if [ -f "$UNO_RUNTIME_TESTS_FAILED_LIST" ]; then
	export UITEST_RUNTIME_TESTS_FILTER=`cat $UNO_RUNTIME_TESTS_FAILED_LIST | base64`

	# echo the failed filter list, if not empty
	if [ -n "$UITEST_RUNTIME_TESTS_FILTER" ]; then
		echo "Tests to run: $UITEST_RUNTIME_TESTS_FILTER"
	fi
fi

mkdir -p $UNO_UITEST_SCREENSHOT_PATH

## The python server serves the current working directory, and may be changed by the nunit runner
dotnet-serve -p 8000 -d "$BUILD_SOURCESDIRECTORY/build/wasm-uitest-binaries/site-$SITE_SUFFIX" &

if [ -f "$UNO_TESTS_FAILED_LIST" ]; then
    UNO_TESTS_FILTER=`cat $UNO_TESTS_FAILED_LIST`
else
    UNO_TESTS_FILTER=$TEST_FILTERS
fi

echo "Test Parameters:"
echo "  Timeout=$UITEST_TEST_TIMEOUT"
echo "  Test filters: $UNO_TESTS_FILTER"

cd $UNO_TESTS_LOCAL_TESTS_FILE

## Run the tests
dotnet run -c Release -- --results-directory $UNO_ORIGINAL_TEST_RESULTS_DIRECTORY --settings .runsettings --filter "$UNO_TESTS_FILTER" || true

echo "Killing dotnet serve"

jobs

## terminate dotnet serve
kill %%

echo "Killed!"

## Copy the results file to the results folder
cp --backup=t $UNO_ORIGINAL_TEST_RESULTS $UNO_UITEST_SCREENSHOT_PATH

## Find the string net::ERR_INSUFFICIENT_RESOURCES in any file inside the path $UNO_UITEST_SCREENSHOT_PATH
## If found, show an error message then exit with code 1
grep -r "net::ERR_INSUFFICIENT_RESOURCES" $UNO_UITEST_SCREENSHOT_PATH && (echo "Found net::ERR_INSUFFICIENT_RESOURCES in the test results, failing the build" && exit 1) || true

# Copy dump files, if any
mkdir $UNO_UITEST_SCREENSHOT_PATH/dumps || true
cp $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.UITests/TestResults/*/*.dmp $UNO_UITEST_SCREENSHOT_PATH/dumps || true
cp $BUILD_SOURCESDIRECTORY/src/SamplesApp/SamplesApp.UITests/TestResults/*/*.xml $UNO_UITEST_SCREENSHOT_PATH/dumps || true

## Export the failed tests list for reuse in a pipeline retry
pushd $BUILD_SOURCESDIRECTORY/src/Uno.NUnitTransformTool
mkdir -p $(dirname ${UNO_TESTS_FAILED_LIST})

echo "Running NUnitTransformTool"

## Fail the build when no test results could be read
dotnet run fail-empty $UNO_ORIGINAL_TEST_RESULTS

if [ $? -eq 0 ]; then
	dotnet run list-failed $UNO_ORIGINAL_TEST_RESULTS $UNO_TESTS_FAILED_LIST
fi

if [ "$UITEST_AUTOMATED_GROUP" == 'RuntimeTests' ];
then
	## Fail the build when no runtime test results could be read
	dotnet run fail-empty $UNO_UITEST_RUNTIMETESTS_RESULTS_FILE_PATH

	if [ $? -eq 0 ]; then
		dotnet run list-failed $UNO_UITEST_RUNTIMETESTS_RESULTS_FILE_PATH $UNO_RUNTIME_TESTS_FAILED_LIST
	fi
fi

popd
