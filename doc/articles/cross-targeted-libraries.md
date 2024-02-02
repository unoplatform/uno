---
uid: Uno.Development.CrossTargetedLibraries
---

# Working with cross-targeted class libraries

Using cross-targeted library projects allows the same code to be compiled for multiple platforms from a single project, and offers advantages over older project formats, such as not having to explicitly include every file.

Uno Platform provides a template which uses this project format. See [this documentation](xref:Guide.HowTo.Create-Control-Library) on how to use it.

## The .csproj format

Cross-targeted libraries use the 'SDK-style' [project file format](https://learn.microsoft.com/dotnet/core/tools/csproj). This format is considerably cleaner than older-style `.csproj` files.

> [!NOTE]
> Visual Studio provides easy edition of new-style `.csproj` files directly without needing to unload the project first.

### Platform-conditional settings

Often you'll want some properties or items to only be defined for specific targets. To do this you put them in a `PropertyGroup` or `ItemGroup` with `Condition=" '$(TargetFramework)' == 'targetname' "` set. You can use the `or` keyword to match multiple targets.

## Adding references

### NuGet references

NuGet references that should be shared by all platforms can be added through the Visual Studio NuGet UI.

If you want to apply NuGet references only to specific platforms, you can do so by manually editing the `csproj` file and putting the `PackageReference` within a conditional `ItemGroup`, eg:

```xml
<ItemGroup Condition="'$(TargetFramework)' == 'net7.0-android'">
 <PackageReference Include="MyLibraryA" Version="3.0.4" PrivateAssets="none" />
</ItemGroup>

<ItemGroup Condition="'$(TargetFramework)' == 'net7.0-ios' or '$(TargetFramework)' == 'net7.0-maccatalyst'">
 <PackageReference Include="MyLibraryB" Version="2.5.11" PrivateAssets="none" />
</ItemGroup>
```

### Project references and SDK references

Adding project references and framework references is not currently working through Visual Studio's interface is not currently working, it gives an error of "Missing value for TargetPlatformWinMDLocation property". You need to add the reference by editing the `csproj` file directly.

Example project reference:

```xml
<ItemGroup>
 <ProjectReference Include="..\CoolControls.Core\CoolControls.Core.csproj" />
</ItemGroup>
```

Example SDK reference:

```xml
<ItemGroup Condition=" '$(TargetFramework)' == 'MonoAndroid13.0' or '$(TargetFramework)' == 'xamarinios10' or '$(TargetFramework)' == 'xamarinmac20' ">
 <Reference Include="System.Numerics" />
 <Reference Include="System.Numerics.Vectors" />
</ItemGroup>
```

## Adding XAML files

All XAML files within the project folder are automatically included in the project, via the 'globbing' defined in the default cross-targeted library template.

In Visual Studio you can add new XAML files via the normal 'Add Items...' UI. Note that this will also add explicit references to the file in the `.csproj`. The explicit references can be safely deleted, they're not necessary.

## Defining conditional symbols

Adding a new conditional symbol via the Visual Studio UI may result in it only being defined for a single target platform. To add it for all platforms (or a specific subset), manually edit the `csproj` file:

```xml
<PropertyGroup>
 <DefineConstants>UNO_1213</DefineConstants>
</PropertyGroup>
```

## Using WebAssembly-specific attributes

When using the cross-targeting template, it becomes possible to use WebAssembly-only features, such as [`HtmlElementAttribute`](interop/wasm-javascript-1.md) or `JSInvoke`. In such cases, you'll need to add a reference to the `Uno.WinUI.Runtime.WebAssembly` nuget package.
