# Native Frame Navigation

## Introduction
Since Uno.UI is written under the UWP api contract, most of the how-tos are sharable. In this article, we will discuss the particularities of the native `Frame` navigation in detail.

It is recommended to be familiar with the concept of navigation in UWP, here are some good starting points:
- [Navigation design basics for Windows apps](https://docs.microsoft.com/en-us/windows/uwp/design/basics/navigation-basics)
- [`Windows.UI.Xaml.Controls.Frame`](https://docs.microsoft.com/en-us/uwp/api/Windows.UI.Xaml.Controls.Frame)

## Native Frame Navigation
On Android and iOS, there is the option to enable the native frame navigation which replaces below controls' UWP style & template by their native counterpart:
- `Windows.UI.Xaml.Controls.Frame`<superscript>*</superscript>
- `Windows.UI.Xaml.Controls.CommandBar`
- `Windows.UI.Xaml.Controls.AppBarButton`

This feature can be enabled by calling the method below typically in the constructor of `App` class:
```cs
#if __IOS__ || __ANDROID__
    Uno.UI.FeatureConfiguration.Style.ConfigureNativeFrameNavigation();
#endif
```
> note: It is recommended to enable this feature in order to provide a more native user experience.

### CommandBar and AppBarButton
In essence, by enabling this, a custom renderer will be used as the control template. This renderer injects the native control in lieu and acts as a mediator, that maps dependency properties (colors, content/icon, ...) and connects events (clicked), between the UWP controls and the native ones:

UWP Control|Native Android Control|Native iOS Control
-|-|-
`CommandBar`|`android.widget.Toolbar`|`UIKit.UINavigationBar`
`AppBarButton`|`android.view.MenuItem`|`UIKit.UIBarButtonItem`

As such, you can achieve the feel and look of native control, including feature like native swipe to go back on iOS, while remaining in the familiar territory of UWP. However, you are no longer able to customize the control template.

### Frame
The `Frame` also uses a custom presenter for its control template.
On iOS, this presenter creates an `UINavigationController`, and keeps its states in sync with its `Frame`. It also creates a `UIViewController` for each `Page` that gets navigated.
On iOS and on Android, it also manages the state of back button on the `CommandBar`, if it is presents on a `Page`.

## Platform Specifics
- android: Tapping the CommandBar's back button, system back button, or performing the sweep gesture will trigger `SystemNavigationService.BackRequested`. It's the responsibility of the application's navigation controller to eventually call `Frame.GoBack()`.
- ios: Tapping the back button automatically triggers a back navigation on the native `UINavigationController` (which is part of the native `Frame` implementation). The `Frame` will raise the `Navigated` event, and its state will automatically be updated to reflect the navigation. The navigation can't be intercepted or canceled.
- any uno platforms: `Uno.UI.FeatureConfiguration.Page.IsPoolingEnabled` can be set to enable reuse of `Page` instances. Enabling this allows the pages in the forward stack to be recycled when stack is modified, which can improve performance of `Frame` navigation.
- android: On mobile, the pages in the back stack are still preserved in the visual tree for performance reason. This feature can be disabled on Android through `Uno.UI.FeatureConfiguration.NativeFramePresenter.AndroidUnloadInactivePages`.
- wasm: `SystemNavigationManager.AppViewBackButtonVisibility` needs to be set to `Visible` to intercept the browser back navigation. The intercepted event is forwarded to `SystemNavigationService.BackRequested`. It's the responsibility of the application's navigation controller to eventually call `Frame.GoBack()`.

## Additional Resources
- [Uno-specific documentation](../controls/CommandBar) on `CommandBar` and `AppBarButton`
- [How-to guide](../guides/native-frame-nav-tutorial) & [sample](https://github.com/unoplatform/Uno.Samples/tree/master/UI/NativeFrameNav) on native frame navigation
