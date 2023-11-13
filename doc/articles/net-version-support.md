---
uid: Uno.Development.NetVersionSupport
---

# .NET version support

This page lists supported .NET versions and [C# language versions](https://learn.microsoft.com/dotnet/csharp/language-reference/configure-language-version) for different target platforms.

## Table of supported versions

| Platform                                   | Default .NET version | Default C# version |  Max .NET version | Max C# version |
|--------------------------------------------|:--------------------:|:------------------:|:-----------------:|:--------------:|
| WebAssembly                                | .NET 7               | 11                 | .NET 8            | 12             |
| Skia (Gtk, Framebufffer, WPF)              | .NET 7               | 11                 | .NET 8            | 12             |
| WinAppSDK                                  | .NET 7               | 11                 | .NET 8            | 12             |
| iOS, macOS, Android, Catalyst (.NET Core)  | .NET 7               | 11                 | .NET 8            | 12             |
| UWP                                        | .NET Standard 2.0    | 7.3                | .NET Standard 2.0 | 7.3            |

## Notes

In Uno 5.0, support for Xamarin.Android, Xamarin.iOS, and Xamarin.macOS was dropped.

You can force a higher version of C# using `LangVersion` in the platform `csproj` (eg `<LangVersion>12.0</LangVersion>`), but some language features may not work properly, such as those that depend on compiler-checked types (eg array slicing, `init`-only properties) or on runtime support (eg default interface implementations).
