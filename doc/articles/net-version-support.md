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
- [.NET 9.0](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9/overview), the successor to .NET 8, has a special focus on cloud-native apps and performance.
  As a Standard Term Support (STS) release, it is now supported for **24 months (until November 10, 2026)**. See the [STS latest announcement](https://devblogs.microsoft.com/dotnet/dotnet-sts-releases-supported-for-24-months/) for more details.

  > [!NOTE]
  > For **mobile workloads**, there is **no change yet**, support remains at **18 months**. See [MAUI support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/maui) for more details.

- [.NET 10.0](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview), the successor to .NET 9, includes improvements in performance, C# 14 support, and long-term platform stability.
  As a Long Term Support (LTS) release, it will be supported for **three years (until November 2028)**.
  At the moment, it is in preview and the least stable option for new projects.

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
