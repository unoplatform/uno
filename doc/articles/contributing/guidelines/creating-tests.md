# Guidelines for creating tests

Good test coverage is essential to maintaining Uno stable and free of regressions. Appropriate tests are generally a requirement for bugfix and new feature PRs.

Testing UI is notoriously tricky, and there is no 'one size fits all' strategy. Accordingly Uno has several different types of tests, which can be confusing to new contributors. These different test strategies complement each other and work together to provide the widest possible coverage.

This guide offers an overview of the various types of tests used within Uno, and explains how to decide which one is appropriate for testing a given behavior.

## Types of tests

These are the main types of tests in Uno:

 Test type                   | Location
 --------------------------- | ---------------------------------------------------------------------------------
 UI tests                    | https://github.com/unoplatform/uno/tree/master/src/SamplesApp/SamplesApp.UITests
 .NET Framework unit tests   | https://github.com/unoplatform/uno/tree/master/src/Uno.UI.Tests
 Platform runtime unit tests | https://github.com/unoplatform/uno/tree/master/src/Uno.UI.RuntimeTests
 Xaml code generation tests  | https://github.com/unoplatform/uno/tree/master/src/SourceGenerators/XamlGenerationTests

 All these tests are run on each CI build, and all tests must pass before a PR can be merged.

 ### UI tests

 Uno's UI tests use the [Uno.UITest](https://github.com/unoplatform/Uno.UITest) testing harness, which mimics the [`Xamarin.UITest` API](https://docs.microsoft.com/en-us/appcenter/test-cloud/uitest/) and extends it to WebAssembly. These tests run out-of-process and interact with a running app (the [SamplesApp](https://github.com/unoplatform/uno/tree/master/src/SamplesApp/UITests.Shared)) to confirm that it behaves correctly on each supported platform.

UI tests can mimic the actions of a user:

 - tapping buttons and other UI elements
 - entering keyboard input
 - scrolling, swiping, and other gestures

 They can verify assertions about the state of the app:

  - text labels
  - any DependencyProperty value
  - onscreen bounds of a view element
  - comparing screenshots at different stages of the app, and asserting equality or inequality

A complete set of instructions for authoring UI tests is available [here](../../uno-development/working-with-the-samples-apps.md).

Although only a subset of the samples in the SamplesApp are covered by automated UI tests, _all_ samples are screenshotted on every build, and a reporting tool (currently WASM-only) reports any screenshots that differ from the previous build. Currently the build isn't gated on these checks, but this may be adjusted in the future.

### .NET Framework unit tests (`Uno.UI.Tests`)

These are 'classic' unit tests which are built against a mocked version of the `Uno.UI` assembly targetting .NET Framework.

These tests are ideal for testing platform-agnostic parts of the code, such as the dependency property system, or panel measurement logic. They [have access to](https://github.com/unoplatform/uno/blob/af5365331d87a80ed4dd83b9e4839cc0d4a0ee5b/src/Uno.UI/AssemblyInfo.cs#L3) internal Uno.UI members, and the mocking layer allows values to be read or written which can't be accessed via the 'real' API.

### Platform runtime unit tests (`Uno.UI.RuntimeTests`)

Again these are 'classic' unit tests, but they are run 'in-process' on the actual target platform, using the 'real' Uno.UI assemblies. They can be run locally through the [Unit Tests Runner](https://github.com/unoplatform/uno/blob/master/src/SamplesApp/SamplesApp.Shared/Samples/UnitTests/UnitTestsPage.xaml) sample in the SamplesApp.

These tests are useful for testing behavior which can run synchronously, or on the UI Thread and where correctness can be asserted programmatically. Relative to the .NET Framework tests, they have the advantage that platform-dependent behavior can be tested, and also that the same test can easily be run on UWP by compiling and running the Windows head of the SamplesApp, giving confidence that the test is verifying the correct behaviour.

The platform runtime tests also have access to internal Uno.UI members if need be, but when possible they should be restricted to the public API, since this allows them to be run on UWP as just mentioned.

### Xaml code generation tests (`XamlGenerationTests`)

These specifically target [the parser](https://github.com/unoplatform/uno/tree/master/src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator) which generates C# code from Xaml files. They are 'tests' in a simple sense that if the parser throws an error, or if it generates invalid C# code, they will fail the CI build.

If you want to actually test that generated Xaml produces correct behavior, which will be the case most of the time, you should use one of the other test types.

## Which type of test should I create?

The UI tests are in some sense the most powerful means of testing behavior, since they can simulate real user input and verify the state of the running app. However they're also typically more time-consuming to author, and take several orders of magnitude longer to run , which affects both your own personal development loop and also very importantly the running time of the CI build. Sometimes a UI test is a sledgehammer where you only need... some... smaller hammer.

As a rule of thumb, if the behavior you're testing can be verified by a unit test (either in `Uno.UI.Tests` or `Uno.UI.RuntimeTests`), you should write a unit test. If you're fixing a bug that involves user interaction, multiple asynchronous UI operations, or can only be verified by examining the onscreen visual state, then create a UI test.

## What can't be tested?

At the moment, Uno's testing process lacks a good means to verify visual output at a per-pixel level against an existing 'source of truth.' This makes it difficult to make automated tests for particular aspects of the framework's behavior: complex paths, image alignment, gradient brushes, and clipping/masking issues are some examples.

If you're working on something that falls under this description, for now it's sufficient to add a [visual sample](https://github.com/unoplatform/uno/tree/master/src/SamplesApp/UITests.Shared) that covers the bug or feature you're working on, and to manually verify that the existing samples in the same feature category aren't negatively affected by your changes.
