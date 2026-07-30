---
uid: Uno.Development.MigratingFromNet9ToNet10
---

# How to Upgrade from .NET 9 to .NET 10

Migrating from .NET 9 to .NET 10 is a generally straightforward process. Below are some specific adjustments you may need to make to your projects and libraries when upgrading.

To upgrade to .NET 10:

- First, read [What's New in .NET 10](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview).
- Install the **.NET 10 SDK** and ensure your IDE is up to date:
  - **Visual Studio** - the latest stable version of [Visual Studio 2026](https://visualstudio.microsoft.com/), which includes native .NET 10 support.
  - **Visual Studio Code** - the latest version of [Visual Studio Code](https://code.visualstudio.com/Download) and the [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) extension.
  - **Rider** - the latest stable version, as .NET 10 support has been available since [Rider 2025.1](https://www.jetbrains.com/rider/whatsnew/2025-1/).
- Run the latest stable version of [uno.check](xref:UnoCheck.UsingUnoCheck) to install the required .NET 10 workloads.
- Uno Platform provides an updated [Visual Studio extension](https://aka.platform.uno/vs-extension-marketplace) in the store that supports Visual Studio 2026 and the new `.slnx` solution format.
- Change all your target framework (TFM) references from `net9.0` to `net10.0`, and from `net9.0-*` to `net10.0-*`.
- Clean your project by deleting the `bin` and `obj` folders.

## Known Issues

For an up-to-date list of **known issues** when using **.NET 10** with Uno Platform, please refer to our [Health Status page](https://aka.platform.uno/health-status).
