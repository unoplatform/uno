---
uid: Uno.Controls.Frame
---

# Frame

> [!TIP]
> This article covers Uno-specific information for the `Frame` class. For a full description of the feature and instructions on using it, see [Frame class](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.frame)

* Displays Page instances, supports navigation to new pages, and maintains a navigation history to support forward and backward navigation.

## Using Frame with Uno

To improve performance during navigation, `Frame` on Android, iOS, and WebAssembly targets operates in different way than in WinUI. Whereas WinUI follows `NavigationCacheMode` property on individual `Page` instances, on iOS and Android we keep the individual page instances in the back stack in memory by default. This way they can be quickly surfaced back to the user during back navigation. This behavior can be controlled using the `FeatureConfiguration.Frame.UseWinUIBehavior` property. This defaults to `true` on Skia targets and to `false` on Android, iOS and WebAssembly.

If you set `UseWinUIBehavior` to `true` on Android and iOS, you also need to override the default style for the control. You can do this by explicitly setting the `Style` to `XamlDefaultFrame`:

```xml
<Frame Style="{StaticResource XamlDefaultFrame}" />
```

Or by creating an implicit style based on `XamlDefaultFrame`:

```xml
<Style TargetType="Frame" BasedOn="XamlDefaultFrame" />
```
