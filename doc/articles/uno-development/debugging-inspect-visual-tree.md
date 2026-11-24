---
uid: Uno.Contributing.InspectVisualTree
---

# Inspecting the runtime visual tree of an Uno app

Often the first step in debugging a UI bug is to scrutinize the application's visual tree. The visual tree is derived from the app's layout defined in XAML, but there's not a straightforward 1-to-1 mapping from XAML to runtime visual tree, due to templating, view manipulation in code, etc. Also, by definition if you're getting a UI bug then there's a discrepancy between what you expect based on the XAML and code and the behavior you're actually observing. Don't live in suspense – check the visual tree!

Tools for inspecting the visual tree differ by platform.

## UWP

UWP has by far the easiest and most convenient experience for debugging the visual tree. The small black toolbar at the top center of your app during debugging enable buttons to go to the Live Visual Tree view, directly select a visual element for inspection, and show layouting decorations. The complement to the Live Visual Tree is the Live Property Explorer, which allows you to inspect current values for any property of a view, and even change some of them on the fly.  

![UWP Live Visual Tree](assets/debugging-inspect-visual-tree/UWP-Live-Visual-Tree.jpg)

## Android

There are a couple of options for viewing the visual tree of an Uno app running on Android.

One approach is to use [Android Studio](https://developer.android.com/studio). You can then attach the debugger to your running process and take a snapshot with the [Layout Inspector](https://developer.android.com/studio/debug/layout-inspector), which allows you to select different elements visually, see their properties, see the whole visual tree, etc.

![Android Studio Layout Inspector](assets/debugging-inspect-visual-tree/Android-Layout-Inspector.jpg)

The other approach is to use the [Stetho package](https://www.nuget.org/packages/nventive.Stetho.Xamarin). It integrates into your app with a few lines of code, and then allows you to inspect the visual tree in Chrome. One nice feature is it allows you to press any element on the device's screen to locate it in the visual tree.

Unfortunately neither of these approaches give you an easy way to inspect properties defined on UIElement, FrameworkElement, and other managed types. You can however look at native properties to obtain information like layout size, opacity, etc.

## iOS

In principle it's possible to use Xcode's 'Debug View Hierarchy' feature on any iOS app, including Uno apps. The steps are the following:

1. Launch Xcode
2. Create a dummy iOS app (or open an existing one) - you won't actually run this app.
3. Run the app whose layout you wish to inspect.
4. Set the device or simulator you're using as the active device in the upper toolbar.
5. Select Debug -> Attach to Process -> [name of the app]
6. Once the debugger has successfully attached, select Debug -> View Debugging -> Capture View Hierarchy.

In practice, Xcode is somewhat temperamental, and this approach may fail for some apps. It's recommended to fall back on the breakpoint-based inspection method described below.

## Web

For an Uno.WASM app you can simply use the layout inspection tools built into whatever browser you're using. For example, for Chrome, open the 'Developer tools' panel (`F12`) and select the 'Elements' tab, or just right-click any element in the visual tree and choose 'Inspect.'

![DOM tree in Chrome](assets/debugging-inspect-visual-tree/WASM-DOM-Elements.jpg)

## Retrieving the visual tree through code or at a breakpoint

It's common enough when debugging to be at a breakpoint and want to quickly know exactly where the view is in the visual tree, that Uno provides a helper method: `TreeGraph`. It's an extension method of the `Uno.UI.Extensions.VisualTreeExtensions` class, which ships in `Uno.UI.Extras` and is public on every target, WinAppSDK included.

`TreeGraph` returns the visual tree below an element as an indented string, with details suited for troubleshooting visual and layout issues:

```csharp
using Uno.UI.Extensions;
...

private void DebugVT(object sender, RoutedEventArgs e)
{
    var tree = this.TreeGraph();
} // <-- add a breakpoint here
```

![TreeGraph](assets/debugging-inspect-visual-tree/tree-graph.png)

To get the whole tree, call it on the root element, for example `XamlRoot.Content.TreeGraph()`. In the watch window, call it as a static method: `Uno.UI.Extensions.VisualTreeExtensions.TreeGraph(element)`.

You can customize the amount of details, as `TreeGraph` has an overload that takes in a `Func<object, string>` to describe the visual tree node:

> [!NOTE]
> For more examples of control details, check out these source files:
>
> - [`DebugVTNode\GetDetails()` method in Toolkit](https://github.com/unoplatform/uno.toolkit.ui/blob/main/src/Uno.Toolkit.UI/Helpers/VisualTreeHelperEx.cs)
> - [`DescribeVTNode\GetDetails()` method in Uno](https://github.com/unoplatform/uno/blob/master/src/Uno.UI.Extras/Extensions/VisualTreeExtensions.cs)

```csharp
var tree = this.TreeGraph(Describe);
...


private static string Describe(object x)
{
    if (x is null) return "<null>";

    return new StringBuilder()
        .Append(x.GetType().Name)
        .Append((x as FrameworkElement)?.Name is string { Length: > 0 } xname ? $"#{xname}" : string.Empty)
        .Append(GetPropertiesDescriptionSafe())
        .ToString();

    string? GetPropertiesDescriptionSafe()
    {
        try
        {
            return string.Join(", ", DescribeDetails(x)) is { Length: > 0 } propertiesDescription
                ? $" // {propertiesDescription}"
                : null;
        }
        catch (Exception e)
        {
            return $"// threw {e.GetType().Name}: {e.Message}";
        }
    }
}

private static IEnumerable<string> DescribeDetails(object x)
{
    // add details pertinent to your debug session:
    if (x is FrameworkElement fe)
    {
        yield return $"Actual={fe.ActualWidth:0.#}x{fe.ActualHeight:0.#}";
        yield return $"Constraints=[{fe.MinWidth:0.#},{fe.Width:0.#},{fe.MaxWidth:0.#}]x[{fe.MinHeight:0.#},{fe.Height:0.#},{fe.MaxHeight:0.#}]";
        yield return $"HV={fe.HorizontalAlignment}/{fe.VerticalAlignment}";
    }
    if (x is ScrollViewer sv)
    {
        yield return $"Offset={sv.HorizontalOffset:0.#},{sv.VerticalOffset:0.#}";
        yield return $"Viewport={sv.ViewportWidth:0.#}x{sv.ViewportHeight:0.#}";
        yield return $"Extent={sv.ExtentWidth:0.#}x{sv.ExtentHeight:0.#}";
    }
    if (x is ListViewItem lvi)
    {
        yield return $"Index={ItemsControl.ItemsControlFromItemContainer(lvi)?.IndexFromContainer(lvi) ?? -1}";
    }
}
```

### Retrieving the visual tree of a flyout/popup

You can add a slight delay before dumping the visual tree, giving you time to open the flyout:

```csharp
private async void DebugVT(object sender, RoutedEventArgs e)
{
    // ample time to click on the flyout
    await Task.Delay(5000);

    // grab the popup's root for tree-graphing
    var root = VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).FirstOrDefault()?.Child;
    var tree2 = root?.TreeGraph();
} // <-- add a breakpoint here
```

This same technique is also useful for when you need the visual tree at certain specific moments, like during mouse-hover, pressed states of some control.

## Tips for interpreting runtime view information

- Look for view types defined in the app's namespace to 'orient yourself' in the visual tree.
- Expect to see more views in the materialized tree than just those defined by XAML. Some are added by default control templates, some are platform-specific, some may be created by code-behind...
