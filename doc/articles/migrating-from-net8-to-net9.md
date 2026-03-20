---
uid: Uno.Development.MigratingFromNet8ToNet9
---
# How to upgrade from .NET 8 to .NET 9

Migrating from .NET 8 to .NET 9 is a generally straightforward process. You may find below some specific adjustments to make to your projects and libraries when upgrading.

To upgrade to .NET 9:

- First, read [What's New in .NET 9](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-9/overview)
- Install/Update the IDE of your choice:  
  - [Visual Studio](https://visualstudio.microsoft.com/vs/) - minimal version **2022 17.13** or later  
  - [Visual Studio Code](https://code.visualstudio.com/) - minimal version **1.94** or later with the related [**C# Dev Kit**](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) stable version
  - [JetBrains Rider](https://www.jetbrains.com/rider/) - minimal version **2024.3** or later
- Run [uno.check](xref:UnoCheck.UsingUnoCheck) to install .NET 9.
- Change all your target framework (TFM) references from `net8.0` to `net9.0`, and `net8.0-*` to `net9.0-*`.
- Delete your bin and obj folders

## Considerations for WebAssembly

WebAssembly support for .NET 9 has made significant internal changes to use the official .NET SDK. Read the following [documentation for WebAssembly migrations](https://aka.platform.uno/wasm-net9-upgrade).
