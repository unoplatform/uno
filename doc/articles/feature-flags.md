---
uid: Uno.Development.FeatureFlags
---

# Configuring Uno's behavior globally

Uno provides a set of feature flags that can be set early in an app's startup to control its behavior. Some of these flags are for backward compatibility, some of them provide fine-grained customizability of a particular feature, and some of them allow to toggle between more 'WinUI-like' and more 'native-like' behavior in a particular context.

## Optimized default control styles

A performance-optimized variant of the built-in Fluent control styles is available, ported from the WinUI "perf2026" default style variants. Those styles preserve the control contracts while being tuned for startup and first-frame performance, mainly by:

- replacing the visual state storyboards that only carry zero-duration `DiscreteObjectKeyFrame`s by `VisualState.Setters`, which apply at the same precedence but avoid creating and starting a `Storyboard`;
- reducing the number of resource lookups performed while materializing a template, for example by sharing a single `Style` between the `ScrollBar` repeat buttons instead of assigning an inline `ControlTemplate` to each of them.

The optimized styles are opt-in, as the visual tree of a template may differ slightly from the non-optimized variant:

```csharp
Uno.UI.FeatureConfiguration.Style.UseDefaultStyleOptimizations = true;
```

This must be set before the first control of a given type is created (typically before `Application.Start`), as default styles are cached on first use. Controls that do not have an optimized variant keep using their default style.
It is also enabled by `Uno.UI.FeatureConfiguration.Perf2026.EnableAll` unless configured explicitly.

The style source baseline is Microsoft UI XAML commit
[`3c9c168844f06c6ac000a97977f0bb3f4c90fd75`](https://github.com/microsoft/microsoft-ui-xaml/tree/3c9c168844f06c6ac000a97977f0bb3f4c90fd75/controls/dev).
Each dictionary identifies its source file. The overlay retains existing Uno template-host adaptations
(such as gradient-border presenters, root-bounded popups, and the CommandBar style-key alias) and
shares the base theme's resource keys. It imports the selected optimized styles, not the entire
resource set or TabView control. The optimized ComboBox also enables the ported
`ComboBoxHelper.KeepInteriorCornersSquare` behavior for editable drop-downs; the legacy style is unchanged.

## Disabling accessibility text scaling (Android and iOS)

By default, Uno automatically enables accessibility text scaling on iOS and Android devices. However, to have more control, the feature flag `Uno.UI.FeatureConfiguration.Font.IgnoreTextScaleFactor` was added to control.

## Giving a maximum text scaling value (Android and iOS)

By default, Android has a limit of 200% for the text scaling which is not the case for iOS.

Use `Uno.UI.FeatureConfiguration.Font.MaximumTextScaleFactor` to control this.

## Rendering backend (Skia)

The rendering backend for the Skia renderer can be configured per platform. On desktop, the preferred approach is the [host builder API](xref:Uno.Skia.Vulkan). The feature flags below are kept for backwards compatibility and for platforms (like Android) that don't use a host builder.

- `Uno.UI.FeatureConfiguration.Rendering.UseVulkanOnSkiaAndroid` — Enables Vulkan rendering on Android (Skia). Default: `false`.
- `Uno.UI.FeatureConfiguration.Rendering.UseVulkanOnX11` — Enables Vulkan rendering on Linux/X11. Default: `false`.
- `Uno.UI.FeatureConfiguration.Rendering.UseVulkanOnWin32` — Enables Vulkan rendering on Windows/Win32. Default: `false`.
- `Uno.UI.FeatureConfiguration.Rendering.UseOpenGLOnSkiaAndroid` — Enables OpenGL ES rendering on Android (Skia). Default: `true`.
- `Uno.UI.FeatureConfiguration.Rendering.UseOpenGLOnX11` — Enables OpenGL rendering on Linux/X11. Default: `null` (auto-detect).
- `Uno.UI.FeatureConfiguration.Rendering.UseOpenGLOnWin32` — Enables OpenGL rendering on Windows/Win32. Default: `null` (auto-detect).

For details, see [Vulkan Rendering Backend](xref:Uno.Skia.Vulkan).

## Skipping visual tree painting (Skia)

Set `Uno.UI.FeatureConfiguration.Rendering.SkipVisualTreePainting` to `true` to skip painting of the visual tree entirely, producing frames with no visual output. Frame scheduling, rendering events, composition animations and `RenderTargetBitmap` keep working as usual. This is intended for scenarios where the visual output is of no interest (e.g. automated tests) to save CPU/GPU. Default: `false`.

## ComboBox

### Default preferred placement

By default, `ComboBox` popup is placed in such a way that the currently selected item is centered above the `ComboBox`. If you want to adjust this behavior on non-Windows targets, set the `Uno.UI.FeatureConfiguration.ComboBox.DefaultDropDownPreferredPlacement` property.

## Popups

### Constraining by visible bounds

By default we don't constrain popups by the visible bounds of the application view on Skia renderer targets. This is different from native renderer where popups are automatically padded by the visible bounds, which ensures the popup does not flow below the system UI (e.g. mobile device status bar or navigation bar). The `ConstrainByVisibleBounds` property allows you to control this behavior. Please note that in case the property is set to `false` (which is the default for Skia renderer), you are responsible for ensuring the content of your popups has appropriate padding around its content. This can be achieved using the [`SafeArea` control in Uno Toolkit](xref:Toolkit.Controls.SafeArea).

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

## TextBox

### Disable the iPadOS 26 floating number pad popover (iOS)

On iPadOS 26, `UITextField` displays the numeric keyboard as a floating popover instead of a docked keyboard. Setting `Uno.UI.FeatureConfiguration.TextBox.DisableNumberPadPopover` to `true` opts out of this behavior and restores the docked keyboard:

```csharp
Uno.UI.FeatureConfiguration.TextBox.DisableNumberPadPopover = true;
```

The flag only takes effect on iOS Skia, on iOS 26 or later, and only affects `TextBox` instances configured for a numeric keyboard on iPad. It has no effect on other platforms, on iOS versions prior to 26, or on non-numeric keyboards. Defaults to `false`.

## ToolTips

By default, `ToolTips` are disabled on all platforms except for WebAssembly and Skia (see [#10791](https://github.com/unoplatform/uno/issues/10791)). To enable them on a specific platform, set the `UseToolTips` configuration flag to `true`. You can add the following in the end of the `App` constructor:

```csharp
Uno.UI.FeatureConfiguration.ToolTip.UseToolTips = true;
```

It is also possible to adjust the delay in milliseconds (`Uno.UI.FeatureConfiguration.ToolTip.ShowDelay` - defaults to `1000`) and show duration in milliseconds (`Uno.UI.FeatureConfiguration.ToolTip.ShowDuration` - defaults to `5000`). This configuration only applies to Uno Platform targets. Windows App SDK (WinUI) will not adhere to this configuration.

## WebView2

### EnableDevTools

Toggles the platform-native developer tools for the underlying web engine of `WebView2` (Chromium DevTools on Windows / Linux Skia, Chrome DevTools remote debugging on Android, Safari Web Inspector on iOS / macOS). Defaults to `true` in `DEBUG` builds and `false` in `RELEASE` builds. Set it during application startup before any `WebView2` is materialized:

```csharp
public App()
{
    Uno.UI.FeatureConfiguration.WebView2.EnableDevTools = true;
    this.InitializeComponent();
}
```

See [WebView2 → Enabling native developer tools](xref:Uno.Controls.WebView2#enabling-native-developer-tools) for the per-platform inspection workflow.

> [!IMPORTANT]
> On Apple platforms the OS only honors this flag for development-signed apps (DEBUG builds).

### AllowSingleSignOnUsingOSPrimaryAccount

Enables single sign-on using the OS primary account (for example, the Microsoft Entra ID / Azure AD account the user is signed into Windows with) when `WebView2` authenticates against supporting resources. This is **Windows (Skia Desktop) only**; it is a no-op on other targets and on the Windows App SDK target (configure SSO through `CoreWebView2EnvironmentOptions` there). Defaults to `false`. Set it during application startup, before any `WebView2` is materialized:

```csharp
public App()
{
    Uno.UI.FeatureConfiguration.WebView2.AllowSingleSignOnUsingOSPrimaryAccount = true;
    this.InitializeComponent();
}
```

> [!NOTE]
> The CoreWebView2 environment is shared process-wide per user-data folder, so this must be set before the first `WebView2` is created. In a managed tenant the flag may be necessary but not sufficient: device-registration state and administrator policy can still gate Entra ID SSO.

### AdditionalBrowserArguments

Additional command-line switches passed to the browser process backing `WebView2` (for example proxy configuration or Chromium feature flags), useful in locked-down or managed environments. **Windows (Skia Desktop) only**, applied when the environment is first created; a no-op elsewhere. Set it during application startup, before any `WebView2` is materialized:

```csharp
public App()
{
    Uno.UI.FeatureConfiguration.WebView2.AdditionalBrowserArguments = "--proxy-server=http://proxy.example:8080";
    this.InitializeComponent();
}
```

## `ApplicationData`

On Skia Desktop targets, it is possible to override the default `ApplicationData` folder locations using `WinRTFeatureConfiguration.ApplicationData` properties. For more information, see [related docs here](/articles/features/applicationdata.md#data-location-on-skia-desktop)

## Android Settings

### Edge-to-edge UI

On Android, the [edge-to-edge UI behavior](https://developer.android.com/develop/ui/views/layout/edge-to-edge) is always enabled: the system UI (status bar and navigation bar) is transparent and the application expands below these overlays. To ensure all UI remains accessible to the user, apply proper safe area padding/margin using the [`SafeArea` control in Uno Toolkit](xref:Toolkit.Controls.SafeArea).

## `Perf2026` optimizations

`Uno.UI.FeatureConfiguration.Perf2026` groups opt-in performance optimizations ported from WinUI that change internal
allocation patterns or visual tree shapes. They are disabled by default; set `Uno.UI.FeatureConfiguration.Perf2026.EnableAll`
to `true` to enable every optimization that has not been configured individually, or set a single flag to opt in
selectively. Because these flags are captured while elements are being created, set them during application startup.
Set `EnableAll` before assigning individual switches; an individual assignment remains an explicit override.

`EnableAll` currently enables optimized default control styles, deferred evaluation of overridden style setters,
and the Grid-less `FontIcon`/`BitmapIcon` visual tree.

### Deferred overridden style setters

Set `Uno.UI.FeatureConfiguration.Style.DeferOverriddenSetterValues` to enable or disable lazy evaluation separately.
When enabled, a style setter that cannot win because of a local value or higher-precedence style does not create its
value until the overriding value is cleared.

### `IconElementNoGridContainer`

When enabled, `FontIcon` and `BitmapIcon` host their inner `TextBlock`/`Image` directly instead of nesting it inside a
`Grid` filled with a transparent brush, saving two objects and one layout/render level per icon. Measure/arrange results,
foreground and theme propagation, hit testing, automation and rendering are unchanged, but code that reaches into an icon
by index (for example `VisualTreeHelper.GetChild(icon, 0)`) observes the inner element instead of the `Grid`. Other icon
types (`SymbolIcon`, `PathIcon`, `ImageIcon`, `IconSourceElement`) keep the `Grid`. Pointer-event `OriginalSource` can
also identify the `IconElement` rather than the removed wrapper.

```csharp
public App()
{
    Uno.UI.FeatureConfiguration.Perf2026.IconElementNoGridContainer = true;
    this.InitializeComponent();
}
```
