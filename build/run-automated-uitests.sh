#!/bin/bash
set -euo pipefail
IFS=$'\n\t'

cd $BUILD_SOURCESDIRECTORY/build/wasm-uitest-binaries

npm i chromedriver@86.0.0
npm i puppeteer@5.3.1

# install dotnet serve
dotnet tool install dotnet-serve --version 1.8.15 --tool-path $BUILD_SOURCESDIRECTORY/build/tools
export PATH="$PATH:$BUILD_SOURCESDIRECTORY/build/tools"

export UNO_UITEST_TARGETURI=http://localhost:8000
export UNO_UITEST_DRIVERPATH_CHROME=$BUILD_SOURCESDIRECTORY/build/wasm-uitest-binaries/node_modules/chromedriver/lib/chromedriver
export UNO_UITEST_CHROME_BINARY_PATH=$BUILD_SOURCESDIRECTORY/build/wasm-uitest-binaries/node_modules/puppeteer/.local-chromium/linux-800071/chrome-linux/chrome
export UNO_UITEST_SCREENSHOT_PATH=$BUILD_ARTIFACTSTAGINGDIRECTORY/screenshots/wasm-automated-$SITE_SUFFIX
export UNO_UITEST_PLATFORM=Browser
export NUNIT_VERSION=3.11.1
export TEST_FILTERS="namespace != 'SamplesApp.UITests.Snap'"
export UNO_ORIGINAL_TEST_RESULTS=$BUILD_SOURCESDIRECTORY/build/TestResult-original.xml
export UNO_TESTS_FAILED_LIST=$BUILD_SOURCESDIRECTORY/build/uitests-failure-results/failed-tests-wasm-automated-$SITE_SUFFIX-chromium.txt
export UNO_TESTS_RESPONSE_FILE=$BUILD_SOURCESDIRECTORY/build/nunit.response

mkdir -p $UNO_UITEST_SCREENSHOT_PATH

## The python server serves the current working directory, and may be changed by the nunit runner
dotnet serve -p 8000 -d "$BUILD_SOURCESDIRECTORY/build/wasm-uitest-binaries/site-$SITE_SUFFIX" &

## Build the NUnit configuration file
echo "--trace=Verbose" > $UNO_TESTS_RESPONSE_FILE
echo "--result=$UNO_ORIGINAL_TEST_RESULTS" >> $UNO_TESTS_RESPONSE_FILE

if [ -f "$UNO_TESTS_FAILED_LIST" ]; then
    echo "--testlist \"$UNO_TESTS_FAILED_LIST\"" >> $UNO_TESTS_RESPONSE_FILE
else
    echo "--where \"$TEST_FILTERS\"" >> $UNO_TESTS_RESPONSE_FILE
fi

echo "$BUILD_SOURCESDIRECTORY/build/samplesapp-uitest-binaries/SamplesApp.UITests.dll" >> $UNO_TESTS_RESPONSE_FILE

## Install NUnit
mono $BUILD_SOURCESDIRECTORY/build/nuget/NuGet.exe install NUnit.ConsoleRunner -Version $NUNIT_VERSION

## Run the tests
mono $BUILD_SOURCESDIRECTORY/build/wasm-uitest-binaries/NUnit.ConsoleRunner.$NUNIT_VERSION/tools/nunit3-console.exe \
    @$UNO_TESTS_RESPONSE_FILE || true

## Export the failed tests list for reuse in a pipeline retry
pushd $BUILD_SOURCESDIRECTORY/src/Uno.NUnitTransformTool
mkdir -p $(dirname ${UNO_TESTS_FAILED_LIST})
dotnet run list-failed $UNO_ORIGINAL_TEST_RESULTS $UNO_TESTS_FAILED_LIST
popd
