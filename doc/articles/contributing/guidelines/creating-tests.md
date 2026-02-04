---
uid: Uno.Contributing.CreatingTests
---

# Guidelines for creating tests

Good test coverage is essential to maintaining Uno stable and free of regressions. Appropriate tests are generally a requirement for bugfix and new feature PRs.

There's no 'one size fits all' strategy to testing UI. Simple in-process unit tests have the traditional advantages of speed and simplicity, but more complex out-of-process testing harnesses are necessary for simulating user interaction and verifying actual screen input. Consequently Uno internally incorporates several different types of tests. These different test strategies complement each other and work together to provide the widest possible coverage.

This guide offers an overview of the various types of tests used within Uno, and shows you how to decide which one is appropriate for testing a given behavior.

The 'TLDR' rule of thumb for adding tests is:

- if you're testing platform-independent functionality, like the dependency property system, [use Uno.UI.Tests](../../uno-development/creating-mocked-tests.md);
- if you're testing platform-dependent functionality that can be verified programmatically in-process, like checking that a control is measured and arranged properly, [use Uno.UI.RuntimeTests](../../uno-development/creating-runtime-tests.md);
- if your test needs to simulate user interaction or check that the final screen output is correct, [use SamplesApp.UITests](../../uno-development/creating-ui-tests.md).

## Types of tests

These are the main types of tests in Uno:

 Test type                   | Location
 --------------------------- | ---------------------------------------------------------------------------------
 UI tests                    | <https://github.com/unoplatform/uno/tree/master/src/SamplesApp/SamplesApp.UITests>
 Unit tests                  | <https://github.com/unoplatform/uno/tree/master/src/Uno.UI.Tests>
 Platform runtime unit tests | <https://github.com/unoplatform/uno/tree/master/src/Uno.UI.RuntimeTests>
 XAML code generation tests  | <https://github.com/unoplatform/uno/tree/master/src/SourceGenerators/XamlGenerationTests>
 UI snapshot tests           | <https://github.com/unoplatform/uno/tree/master/src/SamplesApp/UITests.Shared>

 All these tests are run on each CI build, and all tests must pass before a PR can be merged.

### UI tests

Uno's UI tests use the [Uno.UITest](https://github.com/unoplatform/Uno.UITest) testing harness, which mimics the [`Xamarin.UITest` API](https://learn.microsoft.com/appcenter/test-cloud/uitest/) and extends it to WebAssembly. These tests run out-of-process and interact with a running app (the [SamplesApp](https://github.com/unoplatform/uno/tree/master/src/SamplesApp/UITests.Shared)) to confirm that it behaves correctly on each supported platform.

UI tests can mimic the actions of a user:

- tapping buttons and other UI elements
- entering keyboard input
- scrolling, swiping, and other gestures

They can verify assertions about the state of the app:

- text labels
- any DependencyProperty value
- onscreen bounds of a view element
- comparing screenshots at different stages of the app, and asserting equality or inequality

A complete set of instructions for authoring UI tests is available in [Using the SamplesApp documentation](../../uno-development/working-with-the-samples-apps.md).

Although only a subset of the samples in the SamplesApp are covered by automated UI tests, _all_ samples are screen-shotted on every build, and a reporting tool (runs on Skia, Wasm, Android, and iOS) reports any screenshots that differ from the previous build. Currently, the build isn't gated on these checks, but this may be adjusted in the future.

> [!NOTE]
> Platform runtime tests are generally preferred to UI tests as their execution performance is generally faster than UI Tests.

### Unit tests (`Uno.UI.Tests`)

These are 'classic' unit tests which are built against a mocked version of the `Uno.UI` assembly.

These tests are ideal for testing platform-agnostic parts of the code, such as the dependency property system. They [have access to](https://github.com/unoplatform/uno/blob/b1a6eddcad3bcca6d9756b0a57ff6cf458321048/src/Uno.UI/AssemblyInfo.cs#L7) internal Uno.UI members, and the mocking layer allows values to be read or written which can't be accessed via the 'real' API.

### Platform runtime tests (`Uno.UI.RuntimeTests`)

Again these are 'classic' unit tests, but they are run 'in-process' on the actual target platform, using the 'real' Uno.UI assemblies. They can be run locally through the [Unit Tests Runner](https://github.com/unoplatform/uno/blob/master/src/SamplesApp/SamplesApp.Shared/Samples/UnitTests/UnitTestsPage.xaml) sample in the SamplesApp.

These tests are useful for testing behavior which can run synchronously, or on the UI Thread and where correctness can be asserted programmatically. Relative to the unit tests, they have the advantage that platform-dependent behavior can be tested, and also that the same test can easily be run on WinUI by compiling and running the Windows head of the SamplesApp, giving confidence that the test is verifying the correct behavior.

The platform runtime tests also have access to internal Uno.UI members if need be, but when possible they should be restricted to the public API, since this allows them to be run on WinUI as just mentioned.

### XAML code generation tests (`XamlGenerationTests`)

These specifically target [the parser](https://github.com/unoplatform/uno/tree/master/src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator) which generates C# code from XAML files. They are 'tests' in a simple sense that if the parser throws an error, or if it generates invalid C# code, they will fail the CI build.

If you want to actually test that generated XAML produces correct behavior, which will be the case most of the time, you should use one of the other test types.

### Source generator tests

These can be used to assert that a given input to a given source generator produces specific expected diagnostics. The infrastructure for the tests easily allows to test the generator output exactly, but you should avoid that kind of assertion if you can. These tests exist in [`Uno.UI.SourceGenerators.Tests`](https://github.com/unoplatform/uno/tree/master/src/SourceGenerators/Uno.UI.SourceGenerators.Tests).

### UI snapshot tests

These are 'semi-automated' tests that takes a screenshot of each sample in the [SamplesApp](https://github.com/unoplatform/uno/tree/master/src/SamplesApp/UITests.Shared).

On Android, iOS, and Wasm, a minimal UI test is [generated](https://github.com/unoplatform/uno/blob/master/src/SamplesApp/SamplesApp.UITests.Generator/SnapShotTestGenerator.cs) which simply runs the sample (which automatically takes a screenshot of the loaded sample).

On Skia and macOS, we [loop over the samples and load them](https://github.com/unoplatform/uno/blob/b1a6eddcad3bcca6d9756b0a57ff6cf458321048/src/SamplesApp/SamplesApp.UnitTests.Shared/Controls/UITests/Presentation/SampleChooserViewModel.cs#L364-L427), then [save a screenshot](https://github.com/unoplatform/uno/blob/b1a6eddcad3bcca6d9756b0a57ff6cf458321048/src/SamplesApp/SamplesApp.UnitTests.Shared/Controls/UITests/Presentation/SampleChooserViewModel.cs#L1221-L1244) of each sample using `RenderTargetBitmap` and `BitmapEncoder`. This is generally faster than relying on UI tests.

The screenshots from each sample, as well as all screenshots generated by the main UI tests (using the `TakeScreenshot()` method), are [compared](https://github.com/unoplatform/uno/tree/master/src/Uno.UI.TestComparer) with the same sample from the most recent merged CI build.

**Failed** comparisons will not fail the build (for one thing, it's possible the differences are expected or even desired). Instead, a report is generated, with a summary added to the PR as a comment listing all samples that changed. The onus is on the PR author to manually review these changes to ensure that no regressions have been introduced.

Screenshots can be viewed by going to the CI build (from the main PR summary, press the 'Details' link for the build), opening the **Tests** tab, and opening **Attachments** for individual tests. For failed comparisons, one attachment will be a per-pixel XOR with the screenshot from the previous build, which will give some insight into why the comparison failed. Attached screenshots also contain multiple versions of the same screenshot from previous successful builds of the target branch (e.g. master or main). The file name for those screenshots contains a leading number for which the highest is from your current build.

Note: there may be 'false positives' in the comparison results due to inherently 'noisy' samples: ones with animated content, ones that show a timestamp, etc. It's possible to exclude screenshots for specific samples and/or UI tests.

### Excluding snapshots from comparison

Skip screenshot for static sample using `IgnoreInSnapshotTests`:

```csharp
namespace Uno.UI.Samples.Content.UITests.ButtonTestsControl;

[Sample("Button", IgnoreInSnapshotTests = true)]
public sealed partial class CheckBox_Button_With_CanExecute_Changing : UserControl
{
  ...
}
```

Skip screenshot comparison for UI tests:

```csharp
[Test]
[AutoRetry]
public void TimePicker_Flyout()
{
  Run("UITests.Shared.Windows_UI_Xaml_Controls.TimePicker.TimePicker_Flyout_Automated", skipInitialScreenshot: true);

  ...

  TakeScreenshot("TimePicker - Flyout", ignoreInSnapshotCompare: true);
}
```

## Which type of test should I create?

The UI tests are in some sense the most powerful means of testing behavior, since they can simulate real user input and verify the state of the running app. However they're also typically more time-consuming to author, and take several orders of magnitude longer to run , which affects both your own personal development loop and also very importantly the running time of the CI build. Sometimes, a UI test is a sledgehammer where you only need... some... smaller hammer.

As a rule of thumb, if the behavior you're testing can be verified by a unit test (in `Uno.UI.Tests`) or a runtime test (in `Uno.UI.RuntimeTests`), you should write it as such.

If you're fixing a bug that can be verified by a static sample, create a SamplesApp sample that can be monitored by the snapshot comparer.

If you're fixing a bug that involves user interaction or multiple asynchronous UI operations, or you want a hard verification of the onscreen visual state, then create a UI test.

## What can't be tested?

Some UI behaviors are difficult to test in an automated fashion, such as transient animations.

Some non-UI APIs may not be testable in the emulated environment on the CI build.

If you're working on something that falls under one of these descriptions, you should add a [sample](https://github.com/unoplatform/uno/tree/master/src/SamplesApp/UITests.Shared) that covers the bug or feature you're working on, and verify that the existing samples in the same feature category aren't negatively affected by your changes. Also, you should mark such sample as a manual test. For more information about manual tests, see [Adding a manual test sample section in Using the SamplesApp documentation](../../uno-development/working-with-the-samples-apps.md#adding-a-manual-test-sample).
