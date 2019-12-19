# Working with the Samples Applications

The Uno solution provides a set of sample applications that provide a way to test features, as
well as provide a way to write UI Tests.

Those applications are structured in a way that samples can created out of normal `UserControl` instances, marked with the `SampleControlInfoAttribute` so the sample application can discover them.

Those applications are located in the `SamplesApp` folder of the solution, and a live development out of the master branch version for the WebAssembly application can be found here: https://unoui-sampleapp-unoui-sampleapp-staging.azurewebsites.net

This article contains instructions and guidelines for authoring UI tests for Uno. For guidance on other test strategies used in the Uno codebase, see [this guide](../contributing/guidelines/creating-tests.md).

## Creating UI Tests

The goal of these UI Tests is to :

* Demonstrate the use of controls
* Compare the behavior of controls between platforms
* Provide regression testing through the sample browser

To create a UI Test for the sample applications:
- Create or reuse a folder named from the namespace of the control or class your want to test, replacing "`.`" by "`_`"
- Create a new `UserControl` from the Visual Studio templates in the `UITests.Shared` project
- Add `[Uno.UI.Samples.Controls.SampleControlInfo("Replace_with_control_or_class_name", "MyTestName", description: "MyDescription")]` on the code-behind class.
- Run the samples application, and the sample should appear in the samples browser

The Uno.UI process validates does two types of validations:
- Screenshot based validation (with results comparison, see below)
- Automated UI Testing for WebAssembly and Android using the `SamplesApp.UITests` and the [`Uno.UITest`](https://www.nuget.org/packages?q=uno.uitest) package.

At this time, only WebAssembly and Android are used to run UI Tests, iOS is coming soon.

## Selectively ignore tests per platform

It may be that some UI Tests are platform specific, or that some tests may not work on a particular platform.

In order to do so, the `ActivePlatformsAttribute` allows to specify which platform are active for a given test.

This attribute is used as follows:
```
[ActivePlatforms(Platform.iOS, Platform.Browser)]	// Run on iOS and Browser.
```

This attribute can be placed at the test or class level.

## Setup for Automated UI Tests on WebAssembly

- Navigate to the `SamplesApp.Wasm.UITests` folder and run `npm i`. This will download Puppeteer and the Chrome driver.
- Deploy and run the `SamplesApp.Wasm` application once.

## Setup for Automated UI Tests on Android

- Setup an android simulator or device, start it
- Deploy and run the `SamplesApp.Droid` application on that device
- After you have added a new test page, you must launch the samples application once before running the test, otherwise the code for that page is not generated and the test will fail.

## Running UI Tests

- Open the [`Constants.cs`](src/SamplesApp/SamplesApp.UITests/Constants.cs) file and change the `CurrentPlatform` field to the platform you want to test.
- Select a test in the `SamplesApp.UITests` project and run a specific test.

## Troubleshooting tests running during the CI

The build artifacts contain the tests output, as well as the device logs (in the case of Android).

# Requirements for UI tests

- Each sample should demonstrate one and only one feature of a control so
that it can be run and have a stable screenshot taken. This screenshot is then
using for bitmap comparison during regression testing, or used by the an automated UI Testing tool.
- Avoid the sample for having to scroll to view content, unless you are creating a UI Tests script as well.
- If the sample has multiple states, a Xamarin UI Test script must also be added.

# Creating Non-UI Tests

In the context of non-UI tests, a [special sample control](https://github.com/unoplatform/uno/blob/master/src/SamplesApp/SamplesApp.UnitTests.Shared/Controls/UnitTest/UnitTestsControl.cs) is available in the samples app (Unit Tests Runner). This control [looks for tests in the `Uno.UI.RuntimeTests` project](https://github.com/unoplatform/uno/blob/master/src/SamplesApp/SamplesApp.Shared/Samples/UnitTests/UnitTestsPage.xaml.cs).

Those tests use the MSTests format, and can be run as part of the running application to test for features that depend on the current platform.

To create a Non-UI Test:
- Create or reuse a folder named from the namespace of the class your want to test, replacing "`.`" by "`_`"
- Name your class `Given_Your_Class_Name`
- Create your test methods using `When_Your_Scenario`
- An optional ViewModel type may be provided as an attribute so the browser automatically sets an instance as the DataContext of the sample

> More information about the GivenWhenThen pattern: <https://martinfowler.com/bliki/GivenWhenThen.html>

# Running the WebAssembly UI Tests Snapshots
The WebAssembly head has the ability to be run through puppeteer, and displays all tests in sequence. Puppeteer runs a headless version of Chromium, suited for running tests in a CI environment.

To run the tests:
- Build the `SamplesApp.Wasm.UITests.njsproj` project
- Press `F5`, node will start and run the tests sequentially
- The screen shots are placed in a folder named `out`

Note that the same operation is run during the CI, in a specific job running under Linux. The screen shots are located in the Unit Tests section under `Screenshots Compare Test Run` as well as in the build artifact.

## Running iOS UI Tests in a Simulator on macOS 

Running UI Tests in iOS Simulators on macOS requires, as of VS4Mac 8.4, to build and run the tests from the command line. Editing the Uno.UI solution is not a particularly stable experience yet.

In a terminal, run the following:
``` bash
cd build
./local-ios-uitest-run.sh
```

The Uno.UI solution will build, and the UI tests will run. You may need to adjust some of the parameters in the script, such as:
- `UITEST_SNAPSHOTS_ONLY` which runs automated or snapshots tests
- `UITEST_SNAPSHOTS_GROUP` which controls which group of tests will be run. Note that this feature is mainly used for build performance, where tests from different groups can be run in parallel during the CI.

## Validating the WebAssembly UI Tests results

In the CI build, an artifact named `wasm-uitests` is generated and contains an HTML file that shows all the differences
for screenshots taken for the past builds. Download this artifact and open the html file to determine if any screenshots
have changed.

## Troubleshooting the tests
It is possible to enable the chromium head using the configuration parameters in the [app.ts](src/SamplesApp/SamplesApp.Wasm.UITests/app.ts) file.

# Creating performance benchmarks

Performance is measured using BenchmarkDotNet, in the suite located in the `SamplesApp.Benchmarks` shared project.

A few points to consider when adding tests:
- Make a folder using the namespace separated by `_`
- Don't make classes that contain a very important number of benchmarks. Those tests are run synchronously under
WebAssembly, and this will allow for progress reporting to be visible.
