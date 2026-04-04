---
uid: Uno.UI.SystemBackdrop
---

# SystemBackdrop

Uno Platform exposes the Window.SystemBackdrop API to let apps opt into native desktop backdrop materials (Mica / MicaAlt / Desktop Acrylic) on supported desktop platforms. On macOS this maps to an `NSVisualEffectView` vibrancy material; on Windows 11 (Win32) this uses the native Mica/DWM system-backdrop APIs.

This page explains what is available in Uno, platform differences, usage patterns, and links to the upstream Windows guidance.

> [!TIP]
> For platform and design guidance, see:
> - [Windows system backdrops (developer guidance)](https://learn.microsoft.com/en-us/windows/apps/develop/ui/system-backdrops)
> - [Mica design guidance](https://learn.microsoft.com/en-us/windows/apps/design/style/mica)

## Supported styles

- Mica (Base)
- Mica (BaseAlt) — an alternate Mica variant used by some Fluent surfaces (e.g. TabView background)
- Desktop Acrylic — translucent acrylic material (where supported)

These styles correspond to the WinUI types `Microsoft.UI.Xaml.Media.MicaBackdrop` (with `Microsoft.UI.Composition.SystemBackdrops.MicaKind`) and `Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop`.

## Platform support

| Platform | Availability | Notes |
|----------|--------------|-------|
| **Windows (Win32)** | ✅ Available (Windows 11, build 22621+) | Mapped to DWM DWMWA_SYSTEMBACKDROP_TYPE via DwmSetWindowAttribute; older builds are ignored and logged. |
| **macOS (Skia)** | ✅ Available | Maps to `NSVisualEffectView` vibrancy materials via native host integration. |
| **Linux (Skia)** | ❌ Not available | No runtime implementation in Uno. |
| **WebAssembly** | ❌ Not available | SystemBackdrop is a desktop visual feature and is not implemented for WASM. |
| **Android** | ❌ Not available | Not implemented on mobile platforms. |
| **iOS** | ❌ Not available | Not implemented on mobile platforms. |

## How to enable a system backdrop

You set the backdrop on the active window using `Window.SystemBackdrop`. Typical usage is done from app code (C#) — for example:

```csharp
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Media;

// Set Mica (Base)
App.MainWindow.SystemBackdrop = new MicaBackdrop { Kind = MicaKind.Base };

// Set Mica (BaseAlt)
App.MainWindow.SystemBackdrop = new MicaBackdrop { Kind = MicaKind.BaseAlt };

// Set Desktop Acrylic
App.MainWindow.SystemBackdrop = new DesktopAcrylicBackdrop();

// Clear backdrop
App.MainWindow.SystemBackdrop = null;
```

Tips:
- Make the page/window background transparent so the backdrop shows through (for example: `Page.Background = new SolidColorBrush(Colors.Transparent)` or via XAML). The SamplesApp test performs a traversal of the visual tree and temporarily replaces opaque backgrounds with transparent brushes so the effect is visible.
- On Windows, ensure your app is running on a supported Windows 11 build (22621+). Uno logs a warning when an unsupported backdrop is requested on older builds.

## Limitations and recommendations

- Appearance will differ between platforms — test on the real OS (Windows / macOS) to validate contrast and accessibility.
- Backdrop materials require that underlying content be at least partially transparent to be visible. Remove or make opaque backgrounds transparent where appropriate.
- Avoid placing important UI with critical contrast directly over translucent materials unless you verify legibility across themes (light/dark) and system settings.

## See also

- Windows system backdrops (developer guidance): https://learn.microsoft.com/en-us/windows/apps/develop/ui/system-backdrops
- Mica design guidance: https://learn.microsoft.com/en-us/windows/apps/design/style/mica

[!include[getting-help](includes/getting-help.md)]
