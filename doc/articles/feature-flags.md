---
uid: Uno.Development.FeatureFlags
---

# Configuring Uno's behavior globally

Uno provides a set of feature flags that can be set early in an app's startup to control its behavior. Some of these flags are for backward compatibility, some of them provide fine-grained customizability of a particular feature, and some of them allow to toggle between more 'WinUI-like' and more 'native-like' behavior in a particular context.

## Legacy clipping (Android)

Historically, Uno has been relying on the default platform's behavior for clipping, which is quite different from WinUI compositing behavior.

This behavior can be controlled by `Uno.UI.FeatureConfiguration.UIElement.UseLegacyClipping`, which now defaults to false.

If legacy clipping is enabled, clipping is applied on the assigned children bounds instead of the parent bounds.

## ListView scrolling (Android and iOS)

On iOS and Android platforms specifically, `ListView.ScrollIntoView` performs an animated scrolling instead of an instant scrolling than other platforms.

This feature can be toggled with `Uno.UI.FeatureConfiguration.ListViewBase.AnimateScrollIntoView`.
Alternatively, `Uno.UI.Helpers.ListViewHelper` offers two extension methods, `InstantScrollToIndex` and `SmoothScrollToIndex`, to perform a specific type of scrolling irrespective of the flag set.

## WinUI styles default

By default, Uno favors the default WinUI XAML styles over the native styles for Button, Slider, ComboBox, etc...

This can be changed using `Uno.UI.FeatureConfiguration.Style.UseUWPDefaultStyles`.

## Disabling accessibility text scaling (Android and iOS)

By default, Uno automatically enables accessibility text scaling on iOS and Android devices. However, to have more control, the feature flag `Uno.UI.FeatureConfiguration.Font.IgnoreTextScaleFactor` was added to control.

## Giving a maximum text scaling value (Android and iOS)

By default, Android has a limit of 200% for the text scaling which is not the case for iOS.

Use `Uno.UI.FeatureConfiguration.Font.MaximumTextScaleFactor` to control this.

## ComboBox

### Default preferred placement

By default, `ComboBox` popup is placed in such a way that the currently selected item is centered above the `ComboBox`. If you want to adjust this behavior on non-Windows targets, set the `Uno.UI.FeatureConfiguration.ComboBox.DefaultDropDownPreferredPlacement` property.

### Allow popup under translucent status bar

By default, the `ComboBox` popup will not extend under the status bar even if it is set as translucent. If you want to change this behavior, set the `Uno.UI.FeatureConfiguration.ComboBox.AllowPopupUnderTranslucentStatusBar` property to `true`. This property is Android-specific.

## Popups

### Constraining by visible bounds

By default we don't constrain popups by the visible bounds of the application view on Skia renderer targets. This is different from native renderer where popups are automatically padded by the visible bounds, which ensures the popup does not flow below the system UI (e.g. mobile device status bar or navigation bar). The `ConstrainByVisibleBounds` property allows you to control this behavior. Please note that in case the property is set to `false` (which is the default for Skia renderer), you are responsible for ensuring the content of your popups has appropriate padding around its content. This can be achieved using the [`SafeArea` control in Uno Toolkit](xref:Toolkit.Controls.SafeArea).

### Native popups (Android)

On Android, it is possible to use a native popup implementation, which is integrated into the system for `Popup`- and `Flyout`-derived UI. Prior to Uno Platform 3.5, native popups were used by default. On Uno Platform 3.5 or later, we made the managed implementation the default.

If you require native popups for your use case, set the `Uno.UI.FeatureConfiguration.Popup.UseNativePopup` to `true`.

### Light Dismiss Default

In older versions of Uno Platforms, the `Popup.IsLightDismissEnabled` dependency property defaulted to `true`. In WinUI and Uno 4.1 and newer, it correctly defaults to `false`. If your code depended on the old behavior, you can set the `Uno.UI.FeatureConfiguration.Popup.EnableLightDismissByDefault` property to `true` to override this.

### Prevent light dismiss on window deactivation

By default all light-dismissible elements are dismissed when window deactivates. This happens in various situations, including hitting a breakpoint while debugging. Setting the `Popup.PreventLightDismissOnWindowDeactivated` flag to `true` prevents this behavior. We strongly recommend setting this only when debugging.

## MessageDialog

By default, `MessageDialog` in Uno Platform targets displays using `ContentDialog` on WebAssembly and Skia, whereas it uses native dialog UI on Android, iOS, and macOS. The native dialogs are familiar to the users of the target platform, whereas the `ContentDialog` version offers the same UI on all targets. The `WinRTFeatureConfiguration.MessageDialog.UseNativeDialog` flag allows you to either disable or enable the use of native dialog UI. The default value of the flag depends on the target platform and changing the value of the flag on Skia has no effect (only `ContentDialog` version is available there):

| Feature        | Android | iOS | macOS | WASM | Skia |
|----------------|---------|-----|-------|------| --- |
| Default value of `UseNativeDialog`     | `true` | `true` |  `true`   | `false` | `false` |
| Native version available     | ✅ | ✅ |  ✅   | ✅(*) | ❌ |
| `ContentDialog` version available     | ✅ | ✅ |  ✅   | ✅ | ✅ |

(*) Native WebAssembly implementation uses `alert()` and is very limited.

When `ContentDialog` version is used, it uses the default `ContentDialog` style. If you want to customize the UI, you can declare your own `Style` on the application resource level and then set the `StyleOverride` flag to the resource key:

```xml
<Application.Resources>
    <Style x:Key="CustomMessageDialogStyle" TargetType="ContentDialog">
    ...
    </Style>
</Application.Resources>
```

```c#
WinRTFeatureConfiguration.MessageDialog.StyleOverride = "CustomMessageDialogStyle";
```

## ToolTips

By default, `ToolTips` are disabled on all platforms except for WebAssembly and Skia (see [#10791](https://github.com/unoplatform/uno/issues/10791)). To enable them on a specific platform, set the `UseToolTips` configuration flag to `true`. You can add the following in the end of the `App` constructor:

```csharp
Uno.UI.FeatureConfiguration.ToolTip.UseToolTips = true;
```

It is also possible to adjust the delay in milliseconds (`Uno.UI.FeatureConfiguration.ToolTip.ShowDelay` - defaults to `1000`) and show duration in milliseconds (`Uno.UI.FeatureConfiguration.ToolTip.ShowDuration` - defaults to `5000`). This configuration only applies to Uno Platform targets. Windows App SDK (WinUI) will not adhere to this configuration.

## WebView2

### Inspectable (iOS)

To enable inspecting applications using `WebView2` controls from macOS with the Safari Developer Tools, set the `IsInspectable` configuration flag to `true` in your `App.Xaml.cs`:

```csharp
public App()
{
    this.InitializeComponent();
#if __IOS__
    Uno.UI.FeatureConfiguration.WebView2.IsInspectable = true;
#endif
}
```

> [!IMPORTANT]
>
> This feature will only work for security reasons when the application runs in Debug mode.

## `ApplicationData`

On Skia Desktop targets, it is possible to override the default `ApplicationData` folder locations using `WinRTFeatureConfiguration.ApplicationData` properties. For more information, see [related docs here](/articles/features/applicationdata.md#data-location-on-skia-desktop)

## Deprecated NSObjectExtensions.ValidateDispose for iOS

The method `NSObjectExtensions.ValidateDispose` is deprecated in Uno 5.x and will be removed in the next major release.

In order for calls to fail on uses of this method, set the `Uno.UI.FeatureConfiguration.UIElement.FailOnNSObjectExtensionsValidateDispose` flag to `true`.

## Android Settings

### `IsEdgeToEdgeEnabled`

This flag controls the [edge-to-edge UI behavior](https://developer.android.com/develop/ui/views/layout/edge-to-edge) on Android. When set to `true` it makes the system UI (status bar and navigation bar) transparent, and lets the application expand below these overlays. To ensure all UI is still accessible for the user, proper safe area padding/margin needs to be applied. To achieve this, use the [`SafeArea` control in Uno Toolkit](xref:Toolkit.Controls.SafeArea). The default value is `true` for apps targeting .NET 9 or targeting Android SDK 35 and newer, defaults to `false` otherwise. For apps targeting SDK 35+ and running on Android 15 and newer, edge-to-edge is always enforced by the OS, so it cannot be disabled.

```csharp
#if __ANDROID__
var isEdgeToEdge = FeatureConfiguration.AndroidSettings.IsEdgeToEdgeEnabled;
#endif
```
