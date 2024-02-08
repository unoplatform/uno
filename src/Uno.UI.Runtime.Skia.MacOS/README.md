# Gtk-less Uno for macOS

## Requirements

* Minimum: macOS 10.14

## Pros

* Less dependencies
  * Removing GTK3+ (native, requires separate installation)
  * Removing GtkSharp (no release supports arm64 correctly)
  * Removing Silk.NET on macOS (not needed for Metal)

* Reduced number of managed<->native transitions

* No dependency on Xamarin/Microsoft macOS SDK and toolchain

* Use Metal (not OpenGL, which was disabled for Gtk/Skia/macOS)

## Cons

* Some older Mac (before 2012) might not support Metal. Metal is required for macOS 10.14 (Mojave) and more recent versions.
  * add a software fallback ?
