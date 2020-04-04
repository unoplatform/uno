# Working with themes

Uno Platform supports `Light`, `Dark` and `HighContrast` themes similarly to UWP.

As in UWP, the default theme is `Dark`, but in the case of Uno we use `Light` for all supported systems, as that is the default on all except Windows. However, if you don't specify a theme in the constructor of `App` and don't specify it in `App.xaml` either, it will be determined by the current system theme on iOS, Android and to current system/browser theme (depending on browser implementation) on WASM.

To set `HighContrast` theme or a custom theme, you can use the `Uno.UI.ApplicationHelper` class:

```
Uno.UI.ApplicationHelper.RequestedCustomTheme = "HighContrast";
```

This must be called during app startup in the `App` class constructor. Uno does not yet support `FrameworkElement.RequestedTheme` so the theme cannot be changed dynamically at runtime.

Setting `Application.Current.RequestedTheme` outside of `App` constructor is not allowed, which is in line with UWP.

## Reacting to OS theme

If you don't specify the theme in `App` constructor and `App.xaml`, your app will automatically adapt to OS theme changes at runtime. However, on Android, you need to make sure to add `ConfigChanges.UiMode` to the `MainActivity` `[Application]` attribute, for example:

``` c#
[Activity(
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode,
        WindowSoftInputMode = SoftInput.AdjustPan | SoftInput.StateHidden
    )]
```
