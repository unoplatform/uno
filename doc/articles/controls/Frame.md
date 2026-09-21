---
uid: Uno.Controls.Frame
---

# Frame

> [!TIP]
> This article covers Uno-specific information for the `Frame` class. For a full description of the feature and instructions on using it, see [Frame class](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.frame)

* Displays Page instances, supports navigation to new pages, and maintains a navigation history to support forward and backward navigation.

## Using Frame with Uno

`Frame` follows the WinUI navigation model on every platform. Page instances are created and cached according to each page's `NavigationCacheMode` and the frame's `CacheSize`, so a page with the default `NavigationCacheMode.Disabled` is recreated when you navigate back to it. To keep a page and its state alive across back navigation, set `NavigationCacheMode` to `Enabled` or `Required` on the page:

```xml
<Page NavigationCacheMode="Required">
```

> [!NOTE]
> Before Uno Platform 7.0, `Frame` on Android, iOS and WebAssembly kept every back-stack page in memory unless `FeatureConfiguration.Frame.UseWinUIBehavior` was set to `true`. That mode and the flag have been removed; see [Migrating to Uno Platform 7.0](xref:Uno.Development.MigratingToUno7).
