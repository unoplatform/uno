---
uid: Uno.Development.NetVersionSupport
---

<!-- markdownlint-disable MD001 -->

# .NET version support

This page lists supported .NET versions and [C# language versions](https://learn.microsoft.com/dotnet/csharp/language-reference/configure-language-version) for different target platforms.

## Table of supported versions

# [**Uno 5 and later**](#tab/uno5)

| Platform                                   | Default .NET version | Default C# version |  Max .NET version | Max C# version |
|--------------------------------------------|:--------------------:|:------------------:|:-----------------:|:--------------:|
| WebAssembly                                | .NET 8               | 12                 | .NET 9            | 13             |
| Skia Desktop                               | .NET 8               | 12                 | .NET 9            | 13             |
| WinAppSDK                                  | .NET 8               | 12                 | .NET 9            | 13             |
| iOS, Android  | .NET 8               | 12                 | .NET 9            | 13             |

### Notes

- In Uno 5.3, support for .NET 7 has been removed.
- In Uno 5.0, support for .NET 6 (iOS, Android, mac Catalyst), Xamarin.Android, Xamarin.iOS, and Xamarin.macOS were removed.
