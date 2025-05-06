---
uid: Uno.GettingStarted.Requirements
---

# Supported platforms

Uno Platform applications run on [Android](#android), [iOS](#ios), [Web](#webassembly), [macOS (Desktop)](#macos---desktop), [Linux](#linux), and [Windows](#windows).

See below for the minimum supported versions for each platform.

## WebAssembly

Uno Platform runs in browsers that support WebAssembly, including Chrome, Edge, Edge Dev, Opera, Firefox, and Safari. Desktop and mobile browser versions are supported, using the `net9.0-browserwasm` target framework. See the official WebAssembly site for [more details](https://webassembly.org/roadmap).

Uno Platform runs in browsers that support WebAssembly, including Chromium-based browsers (e.g., Chrome, Edge, Arc, Opera etc.), as well as Firefox and Safari. Desktop and mobile browser versions are supported, using the `net9.0-browserwasm` target framework. See the official WebAssembly site for [more details](https://webassembly.org/roadmap).

## Windows

Two paths are available:

- Applications built with Uno Platform's [Skia Desktop](xref:Uno.Skia.Desktop) target framework, supporting Windows 7 and above, using the `net9.0-desktop` target framework.
- Running apps built with WinAppSDK or WinUI run on Windows 10. Currently Uno.UI's API definition is aligned with [Windows 10 2004 (19041)](https://learn.microsoft.com/windows/uwp/whats-new/windows-10-build-19041), using the `net9.0-windows10.0.19041` target framework. Lower versions can be targeted.

## Android

Uno Platform apps run on devices running Android 5 and above, using the `net9.0-android` target framework.

At compile time, Uno Platform typically supports two versions of the Android SDK, the latest and the immediately previous (e.g. Android 15 and Android 14). It's generally recommended to use the latest version of the SDK.

> [!NOTE]
> This **does not** affect the runtime version. Apps compiled with Android 15 will run properly on devices running Android 10.

## iOS

Uno Platform apps run on iOS 11 and above, using the `net9.0-ios` target framework.

## macOS - Desktop

Uno Platform applications run on all macOS versions supported by .NET, currently macOS 10.15 and above, using the `net9.0-desktop` target framework.

## Linux

Uno Platform applications run on Linux distributions and versions where .NET 8 and later are supported, [listed here](https://learn.microsoft.com/dotnet/core/install/linux), using the `net9.0-desktop` target framework. Supported environments are X11 and Framebuffer.
