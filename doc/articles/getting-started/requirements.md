---
uid: Uno.GettingStarted.Requirements
---

# Supported platforms

Uno Platform applications run on [Android](#android), [iOS](#ios), [web](#webassembly), [mac Catalyst](#catalyst), [Linux](#linux), Tizen, and [Windows](#windows).

See below for minimum supported versions for each platform.

## WebAssembly

Uno Platform runs in browsers that support WebAssembly, including Chrome, Edge, Edge Dev, Opera, Firefox and Safari. Desktop and mobile browser versions are supported. See the official WebAssembly site for [more details](https://webassembly.org/roadmap/).

## Windows

Two paths are available:

- Applications built with Uno.UI's [Skia.WPF](xref:Uno.Skia.Gtk) and [Skia.GTK](xref:Uno.Skia.Wpf) heads, which support run on Windows 7 and above.
- Running apps built with WinAppSDK or WinUI run on Windows 10. Currently Uno.UI's API definition is aligned with [Windows 10 2004 (19041)](https://learn.microsoft.com/windows/uwp/whats-new/windows-10-build-19041). Lower versions can be targeted.

## Android

Uno Platform apps run on devices running Android 5 and above.

At compile time, Uno typically supports two versions of the Android SDK, the latest and the immediately previous. At present, this is Android 11 and Android 10. It's generally recommended to use the latest version of the SDK. (Note that this **does not** affect the runtime version - apps compiled with Android 11 will run happily on devices running Android 5.)

## iOS

Uno Platform apps run on iOS 8 and above.

## mac Catalyst

Uno Platform applications run on all macOS versions supported by Mac Catalyst - currently macOS 10.13 and above.

## Linux

Uno Platform applications run on Linux distributions and versions where .NET 7 is supported, [listed here](https://docs.microsoft.com/en-ca/dotnet/core/install/linux).
