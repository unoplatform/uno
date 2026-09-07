---
uid: Uno.GettingStarted.Requirements
---

# Supported platforms

Uno Platform applications run on [Android](#android), [iOS](#ios), [Web](#webassembly), [macOS (Desktop)](#macos---desktop), [Linux](#linux), and [Windows](#windows).

See below for the minimum supported versions for each platform.

## WebAssembly

Uno Platform runs in browsers that support WebAssembly, including Chrome, Edge, Edge Dev, Opera, Firefox, and Safari. Desktop and mobile browser versions are supported, using the `net10.0-browserwasm` target framework. See the official WebAssembly site for [more details](https://webassembly.org/roadmap).

Uno Platform runs in browsers that support WebAssembly, including Chromium-based browsers (e.g., Chrome, Edge, Arc, Opera etc.), as well as Firefox and Safari. Desktop and mobile browser versions are supported, using the `net10.0-browserwasm` target framework. See the official WebAssembly site for [more details](https://webassembly.org/roadmap).

## Windows

Two paths are available:

- Applications built with Uno Platform's [Skia Desktop](xref:Uno.Skia.Desktop) target framework, supporting Windows 7 and above, using the `net10.0-desktop` target framework.
- Apps built with WinAppSDK or WinUI run on [Windows 10 2004 (19041)](https://learn.microsoft.com/windows/uwp/whats-new/windows-10-build-19041) and above by default, using the `net10.0-windows10.0.19041` target framework. Uno.UI's API definition is aligned with that build, and it is the default `TargetPlatformMinVersion`. A lower `TargetPlatformMinVersion` can be set explicitly, down to the Windows App SDK floor of [Windows 10 1809 (17763)](https://learn.microsoft.com/windows/uwp/whats-new/windows-10-build-17763).

## Android

Uno Platform apps run on devices running Android 7.0 (API 24) and above, using the `net10.0-android` target framework. API 24 is the minimum required by .NET 11, and Uno Platform applies the same floor on `net10.0-android`.

At compile time, Uno Platform typically supports two versions of the Android SDK, the latest and the immediately previous (e.g. Android 16 and Android 15). It's generally recommended to use the latest version of the SDK, and Google Play requires new apps and updates to target API 36 or higher.

> [!NOTE]
> This **does not** affect the runtime version. Apps compiled against the Android 16 SDK will run properly on devices running Android 10.

## iOS

Uno Platform apps run on iOS 15 and above, using the `net10.0-ios` target framework. tvOS 15 and above is supported through `net10.0-tvos`. iOS 15 is the oldest deployment target Xcode 27 — the toolchain .NET 11 builds with — will produce.

> [!NOTE]
> The Skia renderer uses the iOS 15 frame-rate APIs on every supported version.

## macOS - Desktop

Uno Platform applications run on all macOS versions supported by .NET, currently macOS 10.15 and above, using the `net10.0-desktop` target framework.

## Linux

Uno Platform applications run on Linux distributions and versions where latest .NET versions are supported, [listed here](https://learn.microsoft.com/dotnet/core/install/linux), using the `net10.0-desktop` target framework. Supported environments are X11 and Framebuffer.
