---
uid: Uno.Development.MigratingFromNet7ToNet8
---
# How to upgrade from .NET 7 to .NET 8

Migrating from .NET 7 to .NET 8 is a generally straightforward process. You may find below some specific adjustments to make to your projects and libraries when upgrading.

To upgrade to .NET 8:

- First, read [What's New in .NET 8](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-8)
- Install [Visual Studio 17.8](https://visualstudio.microsoft.com/vs/) or later and run [uno.check](xref:UnoCheck.UsingUnoCheck) to install .NET 8.
- Change all your target framework (TFM) references from `net7.0` to `net9.0`, and `net7.0-*` to `net9.0-*`. If you are using a TFM like net7.0-ios13.6, be sure to match the shipping version of that platform or just remove the platform version (i.e. 13.6).
- Delete your bin and obj folders

## Migrating Shared Class Libraries from net7.0 to net9.0

If you are building on windows and experience the compilation error NETSDK1083 when retargeting your library from net7.0 to net9.0, add the following to the `csproj`:

```xml
<PropertyGroup>
   <RuntimeIdentifiers>win-x86;win-x64;win-arm64</RuntimeIdentifiers>
</PropertyGroup>
```

You may also need to add to your `.Windows.csproj`:

```xml
<PropertyGroup>
   <SelfContained>true</SelfContained>
</PropertyGroup>
```

## Upgrading the IL Linker for WebAssembly

If you're using the [XAML Resource Trimming feature](xref:Uno.Features.ResourcesTrimming), you'll need to upgrade or add the following package to your `.Wasm.csproj`:

```xml
<ItemGroup>
   <PackageReference Include="Microsoft.NET.ILLink.Tasks" Version="8.0.0" />
</ItemGroup>
```
