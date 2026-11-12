---
uid: Uno.Development.MigratingFromNet10ToNet11
---

# How to Upgrade from .NET 10 to .NET 11

Uno Platform 7.0 supports .NET 11. .NET 10 remains the default for new projects: it is a Long Term Support (LTS) release, while .NET 11 is a Standard Term Support (STS) release, supported until **November 14, 2028**.

Migrating from .NET 10 to .NET 11 is a generally straightforward process. Below are some specific adjustments you may need to make to your projects and libraries when upgrading.

To upgrade to .NET 11:

- First, read [What's New in .NET 11](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-11/overview) and the [breaking changes in .NET 11](https://learn.microsoft.com/dotnet/core/compatibility/11).
- [Update Uno Platform](xref:Uno.Development.UpgradeUnoNuget) to the latest 7.0 service release. The first Uno Platform 7.0 releases were built against .NET 11 release candidates.
- Install the **.NET 11 SDK** and ensure your IDE is up to date:
  - **Visual Studio** - [Visual Studio 2026](https://visualstudio.microsoft.com/) version **18.12** or later, which includes the .NET 11 SDK.
  - **Visual Studio Code** - the latest version of [Visual Studio Code](https://code.visualstudio.com/Download) and the [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) extension.
  - **Rider** - version **2026.3** or later, the first version with full .NET 11 support.
- Run the latest stable version of [uno.check](xref:UnoCheck.UsingUnoCheck) to install the required .NET 11 workloads.
- Change all your target framework (TFM) references from `net10.0` to `net11.0`, and from `net10.0-*` to `net11.0-*`.
- Clean your project by deleting the `bin` and `obj` folders.

## Android and iOS

On .NET 11, Android and iOS apps run on the CoreCLR runtime instead of Mono. See [Runtimes and compilation in .NET MAUI](https://learn.microsoft.com/dotnet/maui/deployment/runtimes-compilation) for the new defaults.

- Mono-specific build properties, such as `RunAOTCompilation`, `EnableLLVM`, `UseInterpreter` or `MtouchInterpreter`, no longer configure your app, and some of them cause a build warning or error. Remove them when you retarget. See [Mono project properties no longer configure mobile apps](https://learn.microsoft.com/dotnet/core/compatibility/maui/11/mono-project-properties).
- `MONO_*` environment variables and Android `debug.mono.*` properties no longer apply. See [Mono runtime settings no longer configure mobile apps](https://learn.microsoft.com/dotnet/core/compatibility/maui/11/mono-runtime-settings).
- Don't set `UseMonoRuntime` to `true`: Mono isn't supported on .NET 11, and the property produces a build error.

Uno Platform 7.0 already applies the minimum OS versions that .NET 11 needs (Android 7.0 / API 24 and iOS 15) to every target framework. See [Minimum OS versions raised](xref:Uno.Development.MigratingToUno7.ProjectSetup#minimum-os-versions-raised).

## Known Issues

For an up-to-date list of **known issues** when using **.NET 11** with Uno Platform, please refer to our [Health Status page](https://aka.platform.uno/health-status).
