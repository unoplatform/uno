---
uid: Uno.Contributing.Tests.CreateRuntimeTests
---

# Creating unit tests in Uno.UI.RuntimeTests

Platform-runtime unit tests are relatively cheap (in terms of developer productivity and CI time) whilst still allowing functionality to be verified across all supported platforms and in a realistic environment. Accordingly they're appropriate for many bugfixes and new features.

Tests in Uno.UI.RuntimeTests are easy to write. There are a few special attributes and environmental helpers to know about, which this guide will cover.

For other types of automated tests used internally by Uno.UI, [see here](xref:Uno.Contributing.Tests.CreatingTests).

## Running tests locally

Since the Uno.UI.RuntimeTests tests run in the platform environment using the real Uno.UI binaries, they must be run from within SamplesApp.

1. Build and launch the SamplesApp, following [the instructions here](xref:Uno.Contributing.SamplesApp). Note: if you're testing a mobile platform, it's recommended to run on a tablet in landscape mode. On a phone-sized layout, a few tests will fail because they don't have enough space to measure properly.
2. From the sample menu, navigate to 'Unit Tests' > 'Unit Tests Runner' or click on the top-left-most button.
3. (Optional) Add a filter string in the text input control at the top of the page (for example the name or part of the name of your test method); only tests matching the filter will be run. Otherwise, all tests will run.
    > On mobile devices with limit screen width, you can drag the grid-slitter (the horizontal and vertical blue bars) to make more space for the filter textbox or test area.
4. Press the 'Run' button. Tests will run in sequence, and the results will be shown.

## Authoring tests

To add a new runtime test:

1. Locate the test class corresponding to the control or class you want to create a test for. Tests are located in `Uno.UI.RuntimeTests/Tests`. If you need to add a new test class, create the file as `Tests/Namespace_In_Snake_Case/Given_ControlName.cs`. The class should be marked with the `[TestClass]` attribute.
2. Create your test as a method, naming it as `When_Your_Scenario` and marking it with the `[TestMethod]` attribute. (For more information about the 'Given-When-Then' naming style, read <https://martinfowler.com/bliki/GivenWhenThen.html> )
3. The runtime tests run from a background thread by default, but when testing UI-related scenarios this isn't desirable. You can use the `[RunsOnUIThread]` attribute, either on individual test methods or the whole test class, to configure the test (or suite of tests) to be run on the UI thread.
4. Use the helpers described below to set up the state required to repro your bug, and use standard `Assert` calls and/or [FluentAssertions](https://fluentassertions.com/introduction) to verify expected state.
5. Verify your test locally by running according to the instructions above. Typically it's fine to test locally on just one target platform; the CI will take care of the others.

### Helpers

A number of helper methods are available to set up a state for testing common UI scenarios.

#### WindowHelper

The `Private.Infrastructure.TestServices.WindowHelper` class exposes several static methods and properties to easily insert a control into the running visual tree, and to wait for modifications to the UI to have been fully processed, since updates to the UI typically take effect asynchronously.

- `WindowHelper.WindowContent`: assigning a `FrameworkElement` to this static property will cause that element to be loaded into the running visual tree. Subsequently setting it to `null` (or another element) will cause the old element to be unloaded from the visual tree.
- `Task WindowHelper.WaitForLoaded(FrameworkElement element)`: returns a task that will complete once `element` is fully loaded into the visual tree. You'd typically await it after assigning `WindowContent = element`, to ensure that `element` has been loaded, measured, and arranged, before further manipulations or assertions.
- `Task WindowHelper.WaitForIdle()`: returns a task that will complete when the idle dispatcher is raised, roughly indicating that the UI thread 'isn't doing anything'. Await this to wait for the UI thread to 'settle' without a specific condition.
- `Task WindowHelper.WaitFor(Func<bool> condition)`: returns a task that will complete once `condition` is met, or throw an exception if it times out. Await this to wait for the UI to reach a specific expected state.

#### StyleHelper

- `IDisposable UseNativeStyle<T>() where T : Control`: This allows you to override the style settings to use a native default style for the duration of a test. Eg, `using (UseNativeStyle<Slider>()) { }` will cause all `Slider` elements to use the native style by default.
- `IDisposable UseNativeFrameNavigation()`: This is a helper which sets native styles as default for the control types implicated in frame navigation (`Frame`, `CommandBar` and `AppBarButton`). This is useful for testing native frame navigation.

#### Useful methods coming from `Uno.UI` itself

 The `FindFirstChild<T>` and `FindFirstParent<T>` extension methods are helpful in the common case that you want to retrieve a descendant or ancestor element by traversing the visual tree. They optionally take a condition to be met.

 Note that for Android/iOS/macOS, the versions of the methods that allow native views to be traversed and retrieved are located in different namespaces. The complete set of usings to conditionally include is:

 ```csharp
 #if NETFX_CORE
using Uno.UI.Extensions;
#elif __APPLE_UIKIT__
using UIKit;
#else
using Uno.UI;
#endif
 ```

### Other tips

- If you open a popup in some way (including indirectly via a `ComboBox`, `Flyout` etc), ensure to close it at the end of the test, ideally in a `try/finally` block. Otherwise it may interfere with the correct execution of subsequent tests.

### Example

Let's look at a complete test, from the `Given_ListViewBase` test class. The code is below.

The test is ignored on iOS and Android since it verifies a feature that's not yet supported on those platforms. Note that since the `Uno.UI.RuntimeTests` assembly is compiled separately for each platform, we use compiler conditionals to ignore a test per-platform.

The test is an `async` method that returns `Task` because we want to perform asynchronous operations on the UI thread (add the view and wait for it to be measured and arranged). Since the `Given_ListViewBase` class is marked with the `[RunsOnUIThread]` attribute, we don't need to add it again to the method.

We create a new items source, then create a `ListView` and assign its `ItemsSource` property. Then we put the `ListView` inside of a `Border` (because this is the specific measurement scenario we wish to test), and add the `Border` to the active visual tree by assigning it to the `WindowHelper.WindowContent` property.

We call `await WindowHelper.WaitForIdle()` to wait for the newly-added visual trees to be loaded, measured and arranged. (We could also have used the `WindowHelper.WaitForLoaded()` method here.)

In this test we want to check that the item containers inside the list have been properly measured and arranged. We use the `ContainerFromItem()` method to get each container; we wrap it inside a `WaitFor()` check because, on some platforms, it takes a few UI loops for the list to materialize its items. Another way to get the containers would have been to use `FindFirstChild<ListViewItem>()` and an appropriate predicate.

Finally we assert that the `ActualWidth` of each container is what we expect, and the `ActualWidth` of the list itself for good measure.

```csharp
[TestMethod]
#if __IOS__ || __TVOS__ || __ANDROID__
[Ignore("ListView only supports HorizontalAlignment.Stretch - https://github.com/unoplatform/uno/issues/1133")]
#endif
public async Task When_ListView_Parent_Unstretched()
{
 var source = Enumerable.Range(0, 5).ToArray();
 var SUT = new ListView
 {
  HorizontalAlignment = HorizontalAlignment.Stretch,
  ItemsSource = source
 };

 const int minWidth = 193;
 var border = new Border
 {
  HorizontalAlignment = HorizontalAlignment.Left,
  MinWidth = minWidth,
  Child = SUT
 };

 WindowHelper.WindowContent = border;

 await WindowHelper.WaitForIdle();

 ListViewItem lvi = null;
 foreach (var item in source)
 {
  await WindowHelper.WaitFor(() => (lvi = SUT.ContainerFromItem(item) as ListViewItem) != null);
  Assert.AreEqual(minWidth, lvi.ActualWidth);
 }

 Assert.AreEqual(minWidth, SUT.ActualWidth);
}
```
