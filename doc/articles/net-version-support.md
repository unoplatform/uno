---
uid: Uno.Development.NetVersionSupport
---

<!-- markdownlint-disable MD001 -->

# .NET version support

This page lists supported .NET versions and [C# language versions](https://learn.microsoft.com/dotnet/csharp/language-reference/configure-language-version) for different target platforms.

## Table of supported versions

# [**Uno Platform 6.3 and later**](#tab/uno63)

| Platform                                   | Default .NET version | Default C# version |  Max .NET version | Max C# version |
|--------------------------------------------|:--------------------:|:------------------:|:-----------------:|:--------------:|
| WebAssembly                                | .NET 9               | 13                 | .NET 10           | 14             |
| Skia Desktop                               | .NET 9               | 13                 | .NET 10           | 14             |
| WinAppSDK                                  | .NET 9               | 13                 | .NET 10           | 14             |
| iOS, Android                               | .NET 9               | 13                 | .NET 10           | 14             |

### Notes

- In Uno Platform 6.3, support for .NET 8 has been removed.

# [**Uno Platform 5 and later**](#tab/uno5)

| Platform                                   | Default .NET version | Default C# version |  Max .NET version | Max C# version |
|--------------------------------------------|:--------------------:|:------------------:|:-----------------:|:--------------:|
| WebAssembly                                | .NET 8               | 12                 | .NET 9            | 13             |
| Skia Desktop                               | .NET 8               | 12                 | .NET 9            | 13             |
| WinAppSDK                                  | .NET 8               | 12                 | .NET 9            | 13             |
| iOS, Android                               | .NET 8               | 12                 | .NET 9            | 13             |

### Notes

- In Uno Platform 5.3, support for .NET 7 has been removed.
- In Uno Platform 5.0, support for .NET 6 (iOS, Android, mac Catalyst), Xamarin.Android, Xamarin.iOS, and Xamarin.macOS were removed.
