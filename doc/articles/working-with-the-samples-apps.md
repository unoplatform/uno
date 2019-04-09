# Working with the Samples Applications

The Uno solution provides a set of sample applications that provide a way to test features, as
well as provide a way to write UI Tests.

Those applications are structured in a way that samples can created out of normal `UserControl` instances, marked with the `SampleControlInfoAttribute` so the sample application can discover them.

Those applications are located in the `SamplesApp` folder of the solution, and a live devevelopment out of the master branch version for the WebAssembly application can be found here: https://unoui-sampleapp-unoui-sampleapp-staging.azurewebsites.net

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

# Requirements for UI tests

- Each sample should demonstrate one and only one feature of a control so
that it can be run and have a stable screenshot taken. This screenshot is then
using for bitmap comparison during regression testing, or used by the an automated UI Testing tool.
- Avoid the sample for having to scroll to view content, unless you are creating a UI Tests script as well.
- If the sample has multiple states, a Xamarin UI Test script must also be added.

# Creating Non-UI Tests

In the context of non-UI tests, a special sample control is available in the samples app (Unit Tests Runner). This control looks for tests in the `Uno.UI.RuntimeTests` project.

Those tests use the MSTests format, and can be run as part of the running application to test for features that depend on the current platform.

To create a Non-UI Test:
- Create or reuse a folder named from the namespace of the class your want to test, replacing "`.`" by "`_`"
- Name your class `Given_Your_Class_Name` 
- Create your test methods using `When_Your_Scenario`
- An optional ViewModel type may be provided as an attribute so the browser automatically sets an instance as the DataContext of the sample

> More information about the GivenWhenThen pattern: <https://martinfowler.com/bliki/GivenWhenThen.html>

# Creating performance benchmarks

Performance is measured using BenchmarkDotNet, in the suite located in the `SamplesApp.Benchmarks` shared project.

A few points to consider when adding tests:
- Make a folder using the namespace separated by `_`
- Don't make classes that contain a very important number of benchmarks. Those tests are run synchronously under
WebAssembly, and this will allow for progress reporting to be visible.