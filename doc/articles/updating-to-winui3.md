---
uid: Uno.Development.UpdatingToWinUI3
---

# Updating an Uno application to WinUI 3.0

Uno Platform supports authoring apps using [WinUI 3's API](uwp-vs-winui3.md). This article details the changes required when migrating an application from the UWP API set to the WinUI 3 API set.

[Read more about WinUI 3 and Uno Platform.](uwp-vs-winui3.md)

## Migrating an app to WinUI 3.0

- **NuGet updates:**
  - `Uno.UI` becomes `Uno.WinUI`
  - `Uno.UI.DevServer` becomes `Uno.WinUI.DevServer`
  - `Uno.UI.Lottie` becomes `Uno.WinUI.Lottie`
  - `Uno.UI.Foldable` becomes `Uno.WinUI.Foldable`
- **String replacements:**
  - `Windows.UI.Xaml` becomes `Microsoft.UI.Xaml`
  - `Windows.UI.Composition` becomes `Microsoft.UI.Composition`
- **Update `App.xaml.cs`:**
  - If your solution was created with an older version of the Uno app template, you'll need to update `App.xaml.cs` for compatibility with WinUI 3/Project Reunion.

        Fixes to apply:
    - Ensure `Window` doesn't fall out of scope ([diff](https://github.com/unoplatform/uno/commit/0d5418dada17561f857cf13750762468b77dfbf0))
    - Fix invalid defines ([diff](https://github.com/unoplatform/uno/commit/a4c3d3f5ec65071041a7b93f64d7175fbde189ac))

## API Changes

### Changed or removed controls

- `MapControl` is missing
- `MediaPlayerElement` is missing
- `WebView` is now `WebView2`

For a full list of unavailable controls and visual features, as well as timelines for restoration in some cases, consult the [WinUI 3 Feature Roadmap](https://github.com/microsoft/microsoft-ui-xaml/blob/master/docs/roadmap.md#winui-30-feature-roadmap).

### WinRT API changes for Desktop apps

Some WinRT APIs are unsupported in WinUI 3 Desktop apps, notably `CoreDispatcher`, `CoreWindow`, and `ApplicationView`. Consult [this guide](https://github.com/microsoft/microsoft-ui-xaml/blob/master/docs/winrt-apis-for-desktop.md) for supported alternatives to those APIs. `DispatcherQueue`, the replacement for `CoreDispatcher` in WinUI 3, is supported by Uno Platform.

## Creating an application from the templates

Instructions are available in [Getting started with WinUI 3](get-started-winui3.md) for creating a new Uno Platform application targeting WinUI 3.
