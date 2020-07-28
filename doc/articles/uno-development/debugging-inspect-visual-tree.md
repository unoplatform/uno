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
In principle it's possible to use XCode's 'Debug View Hierarchy' feature on any iOS app, including Uno apps. The steps are the following:

1. Launch XCode
2. Create a dummy iOS app (or open an existing one) - you won't actually run this app.
3. Run the app whose layout you wish to inspect.
4. Set the device or simulator you're using as the active device in the upper toolbar.
5. Select Debug -> Attach to Process -> [name of the app]
6. Once the debugger has successfully attached, select Debug -> View Debugging -> Capture View Hierarchy.

In practice, XCode is somewhat temperamental, and this approach may fail for some apps. It's recommended to fall back on the breakpoint-based inspection method described below. 

## Web 
For an Uno.WASM app you can simply use the layout inspection tools built into whatever browser you're using. For example, for Chrome, open the 'Developer tools' panel (`F12`) and select the 'Elements' tab, or just right-click any element in the visual tree and choose 'Inspect.'

![DOM tree in Chrome](assets/debugging-inspect-visual-tree/WASM-DOM-Elements.jpg)

You can configure Uno to annotate the DOM with the values of common Xaml properties. Just add the following somewhere in your app's entry point (eg the constructor of `App.xaml.cs`):

```csharp
#if DEBUG && __WASM__
        // Annotate generated DOM elements with x:Name
        Uno.UI.FeatureConfiguration.UIElement.AssignDOMXamlName = true;
        // Annotate generated DOM elements with commonly-used Xaml properties (height/width, alignment etc)
        Uno.UI.FeatureConfiguration.UIElement.AssignDOMXamlProperties = true;
#endif
```

**Note:** for performance reasons, if a _release build_ of Uno.UI is used, `AssignDOMXamlProperties` will only display the values of properties as they were when the element was loaded - that is, they may be stale in some cases. If a _debug build_ of Uno.UI is used, this limitation is lifted and the DOM annotation will reflect the most up-to-date values.

## Retrieving the visual tree through code or at a breakpoint (Android/iOS/WebAssembly/macOS) 
It's common enough when debugging Uno to be at a breakpoint and want to quickly know exactly where the view is in the visual tree, that we added a helper method.  

If you're using a debug build of Uno, this is directly available on UIElement as the `public string ShowLocalVisualTree(int fromHeight)` method (for ease of use in the watch window). If you're using the release version of Uno, the same method is available as an extension in UIKit.UIViewExtensions for iOS or Uno.UI.ViewExtensions for Android.  

The method returns the visual tree from a certain 'height' above the target element as an indented string. So if you call ShowLocalVisualTree(2), you'll get the visual subtree from the target element's grandparent down. If you call ShowLocalVisualTree(100), you'll almost certainly get the entire visual tree starting from the root element. The original target is picked out with an asterisk (*) so you can find it.  

![ShowLocalVisualTree() on iOS](assets/debugging-inspect-visual-tree/iOS-ShowLocalVisualTree.jpg)

## Tips for interpreting runtime view information 

 * Look for view types defined in the app's namespace to 'orient yourself' in the visual tree.
 * Expect to see more views in the materialized tree than just those defined by XAML. Some are added by default control templates, some are platform-specific, some may be created by code-behind...
