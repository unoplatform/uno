---
uid: Uno.Contributing.Tests.CreateMockedTests
---

# Creating unit tests in Uno.UI.Tests

Unit tests in `Uno.UI.Tests` run against a .NET Framework build of `Uno.UI`, which uses the 'real' Uno code for platform-independent components (eg the dependency-property system) and mocks platform-dependent aspects (eg actual rendering).

Adding tests here is closest to the 'traditional' unit test experience: you can run tests from the Visual Studio test window pane, easily debug the code you're modifying, etc. This is the ideal place to test platform-independent parts of the API, like dependency property behaviors and XAML-generated code.

## Running tests in Uno.UI.Tests

1. Open and build the Uno.UI solution [for the unit tests target](xref:Uno.Contributing.BuildingUno).
2. Open Test Explorer from the TEST menu.
3. Tests are listed under `Uno.UI.Tests`. You can run all tests or a subsection, with or without debugging. Tests run in a vanilla .NET Framework environment. (Note: You usually don't need to run `Uno.Xaml.Tests` tests locally, unless you're making changes to low-level XAML parsing in `Uno.Xaml`. )

## Adding a new test

1. Locate the test class corresponding to the control or class you want to create a test for. If you need to add a new test class, create the file as `Namespace_In_Snake_Case/ControlNameTests/Given_ControlName.cs`. be marked with the `[TestClass]` attribute.
2. Add tests for your cases, naming it as `When_Your_Scenario` and marking it with the `[TestMethod]` attribute. (For more information about the 'Given-When-Then' naming style, read <https://martinfowler.com/bliki/GivenWhenThen.html> )

The mocking layer of Uno.UI for unit tests has been added as needed, and depending on your case, you may encounter areas of functionality that aren't supported. Your options if that happens are either to add the missing mocking, or to [add the test in Uno.UI.RuntimeTests](xref:Uno.Contributing.Tests.RuntimeTests) instead.
