---
uid: Uno.Development.NetVersionSupport
---

<!-- markdownlint-disable MD001 -->

# .NET version support

This page lists supported .NET versions and [C# language versions](https://learn.microsoft.com/dotnet/csharp/language-reference/configure-language-version) for different target platforms.

## Table of supported versions

# [**Uno 5**](#tab/uno5)

| Platform                                   | Default .NET version | Default C# version |  Max .NET version | Max C# version |
|--------------------------------------------|:--------------------:|:------------------:|:-----------------:|:--------------:|
| WebAssembly                                | .NET 7               | 11                 | .NET 8            | 12             |
| Skia (Gtk, Framebufffer, WPF)              | .NET 7               | 11                 | .NET 8            | 12             |
| WinAppSDK                                  | .NET 7               | 11                 | .NET 8            | 12             |
| iOS, macOS, Android, Catalyst (.NET Core)  | .NET 7               | 11                 | .NET 8            | 12             |
| UWP                                        | .NET Standard 2.0    | 7.3                | .NET Standard 2.0 | 7.3            |

### Notes

In Uno 5.0, support for .NET 6 (iOS, Android, mac Catalyst), Xamarin.Android, Xamarin.iOS, and Xamarin.macOS was removed.

For UWP, it is possible force a higher version of C# using `LangVersion` in the platform `csproj` (eg `<LangVersion>12.0</LangVersion>`), but some language features may not work properly, such as those that depend on compiler-checked types (eg array slicing, `init`-only properties) or on runtime support (eg default interface implementations). Using [PolySharp](https://www.nuget.org/packages/PolySharp) can help enabling some more recent C# features.

# [**Uno 4 and earlier**](#tab/uno4-earlier)

| Platform                                   | Default .NET version | Default C# version |  Max .NET version | Max C# version |
|--------------------------------------------|:--------------------:|:------------------:|:-----------------:|:--------------:|
| WebAssembly                                | .NET 6               | 10                 | .NET 7            | 11             |
| Skia (Gtk, Framebufffer, WPF)              | .NET 6               | 10                 | .NET 7            | 11             |
| WinAppSDK                                  | .NET 6               | 10                 | .NET 7            | 11             |
| iOS, macOS, Android, Catalyst (.NET Core)  | .NET 6               | 10                 | .NET 7            | 11             |
| iOS, macOS, Android (Xamarin)              | .NET Standard 2.1    | 8                  | .NET Standard 2.1 | 8              |
| UWP                                        | .NET Standard 2.0    | 7.3                | .NET Standard 2.0 | 7.3            |

### Notes

For Xamarin.Android, Xamarin.iOS, and Xamarin.macOS, the supported versions depend on the version of Xamarin installed, which is generally tied to the Visual Studio version if you are using Visual Studio.

For UWP, it is possible force a higher version of C# using `LangVersion` in the platform `csproj` (eg `<LangVersion>12.0</LangVersion>`), but some language features may not work properly, such as those that depend on compiler-checked types (eg array slicing, `init`-only properties) or on runtime support (eg default interface implementations). Using [PolySharp](https://www.nuget.org/packages/PolySharp) can help enabling some more recent C# features.

***
