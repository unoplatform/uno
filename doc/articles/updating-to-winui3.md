# Updating an Uno application to WinUI 3.0

The support for WinUI 3.0 is currently in preview for the Uno Platform. It is currently generated from the WinUI 2.0 / UWP codebase, by applying a set of transformations to align with WinUI 3.0 APIs using the `Uno.WinUIRevert` tool.

## Migrating an app to WinUI 3.0

- Nuget updates
    - `Uno.UI` becomes `Uno.WinUI`
    - `Uno.UI.RemoteControl` becomes `Uno.WinUI.RemoteControl`
    - `Uno.UI.Lottie` becomes `Uno.WinUI.Lottie`
    - `Uno.UI.DualScreen` becomes `Uno.WinUI.DualScreen`
- String replacements:
    - `Windows.UI.Xaml` becomes `Microsoft.UI.Xaml`
    - `Windows.UI.Composition` becomes `Microsoft.UI.Composition`
- Loggers updates
```
{ "Windows", LogLevel.Warning },
{ "Microsoft", LogLevel.Warning },
```

## API Changes as of Alpha builds

- `Thickness` is missing a constructor
- `Duration` is missing a constructor
- `GridLength` is missing a constructor
- `MapControl` is missing
- `WebView` is now `WebView2`

## Creating an application from the templates

A blank application can be created through:

```bash
dotnet new unoapp-winui
```
