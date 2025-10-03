---
uid: Uno.Development.MigratingFromNet9ToNet10
---

# How to Upgrade from .NET 9 to .NET 10

Migrating from .NET 9 to .NET 10 is a generally straightforward process. You may find below some specific adjustments to make to your projects and libraries when upgrading.

To upgrade to .NET 10:

- First, read [What's New in .NET 10](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview).
- As recommended in Microsoft’s [.NET RC1 announcement](https://devblogs.microsoft.com/dotnet/dotnet-10-rc-1/#🚀-get-started), install the latest version of [Visual Studio 2026 Insiders](https://visualstudio.microsoft.com/insiders/) or later and run latest stable version of [uno.check](xref:UnoCheck.UsingUnoCheck) with the `--pre-major` parameter to install .NET 10.
- Uno Platform provides an updated [Visual Studio extension](https://aka.platform.uno/vs-extension-marketplace) in the store that supports Visual Studio 2026 and the new `.slnx` solution format.
- Change all your target framework (TFM) references from `net9.0` to `net10.0`, and `net9.0-*` to `net10.0-*`.
- Clean your project by deleting the `bin` and `obj` folders.
