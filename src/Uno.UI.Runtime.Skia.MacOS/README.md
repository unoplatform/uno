# Skia Host for macOS

## Requirements

* Minimum OS version: same as the [dotnet version used](https://learn.microsoft.com/en-us/dotnet/core/install/macos)

## Pros

* Faster startup: Less dependencies
  * Removing GTK3+ (native, requires separate installation)
  * Removing Silk.NET on macOS (not needed for Metal)

* Faster execution: Reduced number of managed<->native transitions

* Faster builds: No dependency on the Xamarin/Microsoft macOS SDK and toolchain

* Faster rendering: Use Metal (not OpenGL, which was disabled for Gtk/Skia/macOS) by default. Software rendering is used as a fallback.
