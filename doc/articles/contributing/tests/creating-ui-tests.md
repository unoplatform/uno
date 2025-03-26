---
uid: Uno.Contributing.CreateUITests
---

# Creating automated UI tests

Internally Uno.UI uses automated UI tests using the [Uno.UITest framework](https://github.com/unoplatform/Uno.UITest). These tests run out-of-process relative to the application itself. They can simulate user interaction, and can also record and verify on-screen pixels.

UI tests contribute significantly to the CI build time, and for many purposes a test in [Uno.UI.RuntimeTests](creating-runtime-tests.md) could be sufficient. You should write a `Uno.UITest`-based UI test if:

- you need user interaction to put the app in a state that reproduces the bug, and/or
- you need to verify the final visual output onscreen.

 Conversely, you can use an in-process unit test in Uno.UI.RuntimeTests if:

- you can put the app in the required state programmatically, and
- you can verify the correct behavior programmatically (eg by checking DesiredSize, ActualWidth/ActualHeight etc).

  For more on general testing strategy in Uno.UI, see [Guidelines for creating tests](xref:Uno.Contributing.Tests.CreatingTests).

> [!NOTE]
> [Platform runtime unit tests](xref:Uno.Contributing.Tests.CreatingTests) are generally preferred to UI tests as their execution performance is generally faster than UI Tests.

## Running UI tests locally

1. Ensure [your environment is configured](xref:Uno.GetStarted.vs2022) for the platform you want to run on.
1. Ensure `UnoTargetFrameworkOverride` is set to `net7.0-android` for testing on Android, `net7.0-ios` for testing on iOS, and `net7.0` for testing on Wasm.
1. Open Uno.UI with the [correct target override and solution filter](xref:Uno.Contributing.BuildingUno) for the platform you want to run on.
1. [Build and run the SamplesApp](xref:Uno.Contributing.SamplesApp) at least once.
1. Only Android and WASM are supported from Visual Studio for Windows. (Running tests on iOS using a Mac is possible, see additional instructions below.)
1. If testing on WebAssembly, ensure that [`WebAssemblyDefaultUri`](../../../../src/SamplesApp/SamplesApp.UITests/Constants.cs) matches Url used when the sample app was launched in the step above. Visual Studio may change the Url on demand to avoid conflicts with already running sites on the same machine.
1. Open the [Test Explorer](https://learn.microsoft.com/visualstudio/test/run-unit-tests-with-test-explorer) in Visual Studio.
1. UI tests are grouped under 'SamplesApp.UITests'. From the Test Explorer you can run all tests, debug a single test, etc.

> [!IMPORTANT]
> Running the UI tests won't automatically rebuild the SamplesApp. If you add or modify samples, make sure to re-deploy the SamplesApp before you try to run UI tests against your modifications.

### Troubleshooting

- For Android, ensure that your system-level environment variables `JAVA_HOME` and `ANDROID_HOME` are set to the same values as the ones set in Visual Studio's Xamarin Android options panel. Note that you may need to restart Visual Studio once the variables have been set. You may need to set these values if you get this message when running tests:

   ```console
   Failed to execute: C:\Program Files\Android\Jdk\microsoft_dist_openjdk_1.8.0.25\bin\keytool.exe -J-Duser.language=en -list -v -alias androiddebugkey -keystore
   ```

- For Android, ensure that you are running hardware accelerated emulators. See [this documentation for details](https://learn.microsoft.com/xamarin/android/get-started/installation/android-emulator/hardware-acceleration?pivots=windows).

## Adding a new test

1. Typically the first step is to [add a sample to the SamplesApp](xref:Uno.Contributing.SamplesApp) that reproduces the bug you're fixing or demonstrates the functionality you're adding, unless you can do so with an existing sample.
2. The UI test fixtures themselves are located in [SamplesApp.UITests](../../../../src/SamplesApp/SamplesApp.UITests). Locate the test class corresponding to the control or class you want to create a test for. If you need to add a new test class, create the file as `Namespace_In_Snake_Case/ControlNameTests/ControlName_Tests.cs`. The class should inherit from `SampleControlUITestBase` and be marked with the `[TestFixture]` attribute.
3. Add your test, making sure to include the `[Test]` and `[AutoRetry]` attributes. (The `[AutoRetry]` attributes indicates that the test should be retried if it fails. Currently it's required for all tests.)

## Selectively ignore tests per platform

It may be that some UI Tests are platform specific, or that some tests may not work on a particular platform.

The `ActivePlatformsAttribute` allows to specify which platform are active for a given test.

This attribute is used as follows:

```csharp
[ActivePlatforms(Platform.iOS, Platform.Browser)] // Run on iOS and Browser.
```

This attribute can be placed at the test or class level.

## Test format

The basic structure of a UI test is to run one of the samples from the SamplesApp, issue instructions that mimic user interaction, and then verify the correct state of the program, either via programmatic properties or by inspecting the visible display.

A simple complete test is presented below.

The `[ActivePlatforms]` attribute restricts the test to only run on the listed platforms. In this case it's there because the bug in question still needs to be fixed on WebAssembly.

The `Run()` method launches the sample used for the test. The `_app` field, defined on `SampleControlUITestBase`, provides hooks to interact with the running application. Using `_app.Marked("ControlName")` we can retrieve a [query](https://github.com/unoplatform/Uno.UITest/blob/master/src/Uno.UITest.Helpers/Helpers/UITests.Queries/Query.cs) for the visual element designated `x:Name="ControlName"`. Queries can be used to interact with and retrieve information from visual elements; in many cases, there are overrides that also allow passing the name string directly.

The `_app.WaitForElement(element)` method will wait until the designated element is loaded and available. It's always important to remember when writing UI tests that the interactions you program will not execute synchronously. You must explicitly wait for a condition that confirms that the expected change has occurred. `WaitForElement()` and `WaitForText()` are two common ways to do this.

The `TakeScreenshot()` method, as the name suggests, takes a screenshot of the application in its current state. This can be visually analyzed in the running test, and it will also be available as an attachment in the tests browser on the CI.

The `_app.FastTap()` method simulates the user tapping on an element in the app, in this case a button. (It's called `FastTap()` because it's more performant than the `_app.Tap()` method.)

The `_app.WaitForText("ElementName", "ExpectedText")` method waits until the `TextBlock` named "ElementName" is displaying "ExpectedText", as a confirmation that the button was tapped and the visual tree has had time to update. The `WaitForText()` method is a convenience wrapper for the generalized `WaitForDependencyPropertyValue()` method, which allows any public 'DependencyProperty` value to be waited upon.

Finally, we take another screenshot, and then use the `ImageAssert` class to verify that the onscreen display has changed as expected.

```csharp
[TestFixture]
public class LinearGradientBrush_Tests : SampleControlUITestBase
{
 [Test]
 [AutoRetry]
 [ActivePlatforms(Platform.Android, Platform.iOS)] // This should be enabled for WASM once it no longer uses the LEGACY_SHAPE_MEASURE code path - https://github.com/unoplatform/uno/issues/2983
 public void When_GradientStops_Changed()
 {
  Run("UITests.Windows_UI_Xaml_Media.GradientBrushTests.LinearGradientBrush_Change_Stops");

  var rectangle = _app.Marked("GradientBrushRectangle");

  _app.WaitForElement(rectangle);

  var screenRect = _app.GetRect(rectangle);

  var before = TakeScreenshot("Before");

  _app.FastTap("ChangeBrushButton");

  _app.WaitForText("StatusTextBlock", "Changed");

  var after = TakeScreenshot("After");

  ImageAssert.AreNotEqual(before, after, screenRect);
 }
}
```

## Running iOS UI Tests in a Simulator on macOS

Running UI Tests in iOS Simulators on macOS requires, as of VS4Mac 8.4, to build and run the tests from the command line. Editing the Uno.UI solution is not a particularly stable experience yet.

In a terminal, run the following:

```bash
cd build
./test-scripts/local-ios-uitest-run.sh
```

The Uno.UI solution will build, and the UI tests will run. You may need to adjust some of the parameters in the script, such as:

- `UITEST_SNAPSHOTS_ONLY` which runs automated or snapshots tests
- `UITEST_SNAPSHOTS_GROUP` which controls which group of tests will be run. Note that this feature is mainly used for build performance, where tests from different groups can be run in parallel during the CI.

## Uno.UITest

[`Uno.UITest`](https://github.com/unoplatform/Uno.UITest) is a standalone UI testing framework with an API very similar to the [Xamarin.UITest framework](https://learn.microsoft.com/appcenter/test-cloud/frameworks/uitest/), allowing tests written for 'Xamarin.UITest' to be imported with little or no modifications and additionally run on Uno WebAssembly apps.

On Android and iOS, `Uno.UITest` is actually a thin wrapper over `Xamarin.UITest`, this means for example that you can [use the REPL](https://learn.microsoft.com/appcenter/test-cloud/frameworks/uitest/#using-the-repl) while authoring a test.
