# Working with themes

Uno Platform supports `Light`, `Dark` and `HighContrast` themes similarly to UWP.

As in UWP, the default theme is `Dark`. If you don't specify theme in the constructor of `App` and don't specify it in `App.xaml`, it will be determined by the current system theme on iOS and Android. WASM doesn't yet support proper system theme detection, so we recommend to make sure to explicitly set theme there unless you want to stay with the default `Dark` theme.

To set `HighContrast` theme or a custom theme, you can use the `Uno.UI.ApplicationHelper` class:

```
Uno.UI.ApplicationHelper.RequestedCustomTheme = "HighContrast";
```

This must be called during app startup in the `App` class constructor. Uno does not yet support `FrameworkElement.RequestedTheme` so theme cannot be changed dynamically at runtime.