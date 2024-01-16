---
uid: Uno.Features.NativeFrameNav
---

# Native Frame Navigation

## Introduction

This article discusses the particularities of the native `Frame` navigation in detail.

It is recommended to be familiar with the concept of navigation in UWP, here are some good starting points:

- [Navigation design basics for Windows apps](https://learn.microsoft.com/windows/uwp/design/basics/navigation-basics)
- [`Windows.UI.Xaml.Controls.Frame`](https://learn.microsoft.com/uwp/api/Windows.UI.Xaml.Controls.Frame)

## Native Frame Navigation

On Android and iOS, there is the option to enable native frame navigation. Enabling this feature replaces the default styles and templates of the controls below:

- `Windows.UI.Xaml.Controls.Frame`<superscript>*</superscript>
- `Windows.UI.Xaml.Controls.CommandBar`
- `Windows.UI.Xaml.Controls.AppBarButton`

This feature can be enabled by calling the method below, typically in the constructor of your `App` class in `App.cs` or `App.xaml.cs`:

```csharp
#if __IOS__ || __ANDROID__
    Uno.UI.FeatureConfiguration.Style.ConfigureNativeFrameNavigation();
#endif
```

> [!NOTE]
> It is recommended to enable this feature in order to provide a more native user experience.

### CommandBar and AppBarButton

In essence, by enabling this, a custom renderer will be used as the control template. This renderer injects the native control into the app, and maps dependency properties (colors, content/icon, ...) and events (clicked), between the UWP controls and the native ones:

UWP Control|Native Android Control|Native iOS Control
-|-|-
`CommandBar`|`android.widget.Toolbar`|`UIKit.UINavigationBar`
`AppBarButton`|`android.view.MenuItem`|`UIKit.UIBarButtonItem`

As such, you can have the look and feel of the native control, including features like native swipe to go back on iOS, while programming against the UWP API. However, you are no longer able to customize the control template.

### Frame

The `Frame` also uses a custom presenter for its control template.
On iOS, this presenter creates an `UINavigationController`, and keeps its states in sync with its `Frame`. It also creates a `UIViewController` for each `Page` that gets navigated.
On iOS and on Android, it also manages the state of back button on the `CommandBar`, if it is presents on a `Page`.

### Helpers

There is a `FrameNavigationHelper` that is in place to expose useful properties and methods related to Frame-based navigation logic. It contains the following helper methods:

Method|Return Type|Description
-|-|-
`GetCurrentEntry(Frame)`|`PageStackEntry`|Returns the PageStackEntry for the currently displayed Page within the given `frame`
`GetInstance(PageStackEntry)`|`Page`|Returns the actual Page instance of the given `entry`
`EnsurePageInitialized(Frame, PageStackEntry)`|`Page`|Retrieves the current `Page` instance of the given `PageStackEntry`. If no instance exists, the `Page` will be created and properly initialized to the provided `Frame`
`CreateNavigationEventArgs(...)`|`NavigationEventArgs`|Creates a new instance of `NavigationEventArgs`/>

## Platform Specifics

- Android: Tapping the CommandBar's back button, system back button, or performing the sweep gesture will trigger `SystemNavigationService.BackRequested`. It's the responsibility of the application's navigation controller to eventually call `Frame.GoBack()`.
- iOS: Tapping the back button automatically triggers a back navigation on the native `UINavigationController` (which is part of the native `Frame` implementation). The `Frame` will raise the `Navigated` event, and its state will automatically be updated to reflect the navigation. The navigation can't be intercepted or canceled.
- all non-Windows platforms: `Uno.UI.FeatureConfiguration.Page.IsPoolingEnabled` can be set to enable reuse of `Page` instances. Enabling this allows the pages in the forward stack to be recycled when stack is modified, which can improve performance of `Frame` navigation.
- Android: On mobile, the pages in the back stack are still preserved in the visual tree for performance reasons. This feature can be disabled on Android by setting `Uno.UI.FeatureConfiguration.NativeFramePresenter.AndroidUnloadInactivePages = true`.
- WASM: `SystemNavigationManager.AppViewBackButtonVisibility` needs to be set to `Visible` to intercept the browser back navigation. The intercepted event is forwarded to `SystemNavigationService.BackRequested`. It's the responsibility of the application's navigation controller to eventually call `Frame.GoBack()`.

## Additional Resources

- [Uno-specific documentation](../controls/CommandBar.md) on `CommandBar` and `AppBarButton`
- [How-to guide](../guides/native-frame-nav-tutorial.md) & [sample](https://github.com/unoplatform/Uno.Samples/tree/master/UI/NativeFrameNav) on native frame navigation
