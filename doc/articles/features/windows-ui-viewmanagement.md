---
uid: Uno.Features.WinUIViewManagement
---

# View Management

> [!TIP]
> This article covers Uno-specific information for `Windows.UI.ViewManagement` namespace. For a full description of the feature and instructions on using it, consult the UWP documentation: https://learn.microsoft.com/en-us/uwp/api/windows.ui.viewmanagement

* The `Windows.UI.ViewManagement.ApplicationViewTitleBar` class allows working with the title bar of the application window.
* The `Windows.UI.ViewManagement.UISettings` class allows retrieving the current system visual settings.

## `ApplicationViewTitleBar`

The `BackgroundColor` property is implemented on WASM and uses the `theme-color` `<meta>` tag.

Is you set `theme_color` in the PWA application manifest, setting this property will override this configuration.

This functionality is currently supported in Chrome, Edge (Chromium) and Opera when app is installed as PWA and in Chrome mobile (even without installing).

![Blue title bar](../Assets/features/applicationviewtitlebar/blue.png)
![Red title bar](../Assets/features/applicationviewtitlebar/red.png)

## `UISettings`

Using the `GetColorValue` method, you can retrieve the system `Background` and `Foreground` color, which is useful to check if the system is currently using dark or light theme. To get notified about the color scheme changes, subscribe to the `ColorValuesChanged` event. Similarly to UWP, make sure to keep a reference to the `UISettings` instance, otherwise the instance will be collected and the event will not be raised.

On Android, the `AnimationsEnabled` property is implemented and allows you to check whether animations were disabled on the system level (for accessibility or battery saving). You can then use this information to disable custom animations within your app.
