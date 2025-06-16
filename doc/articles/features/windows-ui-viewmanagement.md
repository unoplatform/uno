---
uid: Uno.Features.WinUIViewManagement
---

# View Management

> [!TIP]
> This article covers Uno-specific information for the `Windows.UI.ViewManagement` namespace. For a full description of the feature and instructions on using it, see [Windows.UI.ViewManagement Namespace](https://learn.microsoft.com/uwp/api/windows.ui.viewmanagement).

* The `Windows.UI.ViewManagement.ApplicationViewTitleBar` class allows working with the title bar of the application window.
* The `Windows.UI.ViewManagement.StatusBar` class allows working with the status bar on mobile.
* The `Windows.UI.ViewManagement.UISettings` class allows retrieving the current system visual settings.

## `ApplicationViewTitleBar` class

The `BackgroundColor` property is implemented on WASM and uses the `theme-color` `<meta>` tag.

If you set `theme_color` in the PWA application manifest, setting this property will override this configuration.

This functionality is currently supported in Chrome, Edge (Chromium), and Opera when the app is installed as PWA and in Chrome mobile (even without installing).

![Blue title bar](../Assets/features/applicationviewtitlebar/blue.png)
![Red title bar](../Assets/features/applicationviewtitlebar/red.png)

## `StatusBar` class

The `StatusBar` is located at the top of the screen on mobile devices. The properties below are implemented for both iOS and Android.

You may style the status bar using the `BackgroundColor` and `ForegroundColor` properties.

> [!NOTE]
> While you can assign any color to `ForegroundColor`, the status bar will render it as either light or dark. It will automatically choose the closest match to the specified color.

If you don’t explicitly set the `ForegroundColor`, the app will automatically choose between a light or dark foreground to try to maximize contrast with the `BackgroundColor` you provide.

> [!IMPORTANT]
> In order for the status bar colors to be customizable on iOS, the [`UIViewControllerBasedStatusBarAppearance` property](https://developer.apple.com/documentation/bundleresources/information-property-list/uiviewcontrollerbasedstatusbarappearance) needs to be set to false in the platform's Info.plist file.

![Blue Android Status Bar](../Assets/features/statusbar/blue.png)
![Dark blue iOS Status Bar](../Assets/features/statusbar/darkblue.png)

## `UISettings`

Using the `GetColorValue` method, you can retrieve the system `Background` and `Foreground` color, which is useful to check if the system is currently using dark or light theme. To get notified about the color scheme changes, subscribe to the `ColorValuesChanged` event. Similarly to UWP, make sure to keep a reference to the `UISettings` instance, otherwise the instance will be collected and the event will not be raised.

On Android, the `AnimationsEnabled` property is implemented and allows you to check whether animations were disabled on the system level (for accessibility or battery saving). You can then use this information to disable custom animations within your app.
