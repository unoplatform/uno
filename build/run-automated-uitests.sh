#!/bin/bash
set -euo pipefail
IFS=$'\n\t'

cd $BUILD_SOURCESDIRECTORY/build/wasm-uitest-binaries

npm i chromedriver@74.0.0
npm i puppeteer@1.13.0
mono $BUILD_SOURCESDIRECTORY/build/nuget/NuGet.exe install NUnit.ConsoleRunner -Version 3.10.0

export UNO_UITEST_TARGETURI=http://localhost:8000
export UNO_UITEST_DRIVERPATH_CHROME=$BUILD_SOURCESDIRECTORY/build/wasm-uitest-binaries/node_modules/chromedriver/lib/chromedriver
export UNO_UITEST_CHROME_BINARY_PATH=$BUILD_SOURCESDIRECTORY/build/wasm-uitest-binaries/node_modules/puppeteer/.local-chromium/linux-637110/chrome-linux/chrome
export UNO_UITEST_SCREENSHOT_PATH=$BUILD_ARTIFACTSTAGINGDIRECTORY/screenshots/wasm-automated
export UNO_UITEST_PLATFORM=Browser

mkdir -p $UNO_UITEST_SCREENSHOT_PATH

## The python server serves the current working directory, and may be changed by the nunit runner
bash -c "cd $BUILD_SOURCESDIRECTORY/build/wasm-uitest-binaries/site; python server.py &"

export TEST_FILTERS="namespace != 'SamplesApp.UITests.Snap'"

mono $BUILD_SOURCESDIRECTORY/build/wasm-uitest-binaries/NUnit.ConsoleRunner.3.10.0/tools/nunit3-console.exe \
    --where "$TEST_FILTERS" \
    $BUILD_SOURCESDIRECTORY/build/wasm-uitest-binaries/test-bin/SamplesApp.UITests.dll
