---
uid: Uno.Contributing.CreateMockedTests
---

# Creating unit tests in Uno.UI.Tests

Unit tests in `Uno.UI.Tests` (the test runner project at `src/Uno.UI.Tests/Uno.UI.Unit.Tests.csproj`) run against the **Skia** build of Uno.UI. They exercise the real cross-platform code path -- the same dependency-property system, layouter, and dispatcher used by Skia at runtime -- not a mock. Any platform-specific behavior (rendering surfaces, native windowing, OS APIs) is provided by the Skia runtime stack itself.

Adding tests here is closest to the 'traditional' unit test experience: you can run tests from the Visual Studio test window pane, easily debug the code you're modifying, etc. This is the ideal place to test platform-independent parts of the API, like dependency property behaviors and XAML-generated code.

If a test requires a hosted Window, real input dispatching, or rendering output (for example, screenshot comparisons or visibility-driven assertions), prefer [`Uno.UI.RuntimeTests`](creating-runtime-tests.md) instead -- those tests run inside a hosted SamplesApp on each target platform.

## Running tests in Uno.UI.Tests

1. Open and build the Uno.UI solution [for the unit tests target](building-uno-ui.md).
2. Open Test Explorer from the TEST menu.
3. Tests are listed under `Uno.UI.Tests`. You can run all tests or a subsection, with or without debugging. (Note: You usually don't need to run `Uno.Xaml.Tests` tests locally, unless you're making changes to low-level XAML parsing in `Uno.Xaml`.)

## Adding a new test

1. Locate the test class corresponding to the control or class you want to create a test for. If you need to add a new test class, create the file as `Namespace_In_Snake_Case/ControlNameTests/Given_ControlName.cs` and mark it with the `[TestClass]` attribute.
2. Add tests for your cases, naming them as `When_Your_Scenario` and marking each with the `[TestMethod]` attribute. (For more information about the 'Given-When-Then' naming style, read <https://martinfowler.com/bliki/GivenWhenThen.html>.)
