# Uno Support for Windows.UI.ViewManagement

## `ApplicationViewTitleBar`

The `BackgroundColor` property is implemented on WASM and uses the `theme-color` `<meta>` tag.

Is you set `theme_color` in the PWA application manifest, setting this property will override this configuration.

This functionality is currently supported in Chrome, Edge (Chromium) and Opera when app is installed as PWA and in Chrome mobile (even without installing).

![Blue title bar](../Assets/features/applicationviewtitlebar/blue.png)
![Red title bar](../Assets/features/applicationviewtitlebar/red.png)

## `UISettings`

The `UISettings` class can be used to retrieve the current system visual settings.

Using the `GetColorValue` method, you can retrieve the system `Background` and `Foreground` color, which is useful to check if the system is currently using dark or light theme. To get notified about the color scheme changes, subscribe to the `ColorValuesChanged` event. Similarly to UWP, make sure to keep a reference to the `UISettings` instance, otherwise the instance will be collected and the event will not be raised.

On Android, the `AnimationsEnabled` property is implemented and allows you to check whether animations were disabled on the system level (for accessibility or battery saving). You can then use this information to disable custom animations within your app.