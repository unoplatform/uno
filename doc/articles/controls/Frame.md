---
uid: Uno.Controls.Frame
---

# Frame

<!-- Leave the infotip below in place, and add a link to the UWP documentation for the feature or control you're documenting. If the feature has no UWP equivalent, you should be using the Uno-only feature template: .feature-template-uno-only.md -->

> [!TIP]
> This article covers Uno-specific information for Frame. For a full description of the feature and instructions on using it, see [Link text](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.frame?view=windows-app-sdk-1.5)

* Displays Page instances, supports navigation to new pages, and maintains a navigation history to support forward and backward navigation.

## Using Frame with Uno

To improve performance during navigation, `Frame` on Android, iOS, and WebAssembly targets operates in different way than in WinUI. Whereas WinUI follows `NavigationCacheMode` property on individual `Page` instances, on iOS and Android we keep the individual page instances in the backstack in memory by default. This way the can be quickly surfaced back to the user during back navigation. This behavior can be controlled using the `FeatureConfiguration.Frame.UseWinUIBehavior` property. This defaults to `true` on Skia targets and to `false` on Android, iOS and WebAssembly.
