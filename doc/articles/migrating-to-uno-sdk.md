---
uid: Uno.Development.MigratingToUnoSdk
---
# Migrating to Uno.Sdk

The Uno.Sdk provides several enhancements to further simplify Uno Projects. It is important to remember that the Uno.Sdk is designed to reduce the noise that you need in your projects, while still ensuring that you have the ability to override values that come from the Sdk itself. This means that values such as the `SupportedOSPlatformVersion` which have been in the template's `Directory.Build.props` in the past, now have a default value set to the minimum supported by Uno Platform. If you need to be more restrictive for your specific project you can provide your overrides for the platform(s) that you need.

## Create a global.json

It is recommended that you create a global.json in your solution root directory.

```json
{
  "msbuild-sdks": {
    "Uno.Sdk": "{Current Version of Uno.WinUI/Uno.Sdk}",
    "Microsoft.Build.NoTargets": "3.7.56"
  }
}
```

> [!NOTE]
> The Microsoft.Build.NoTargets Sdk has been used for the last several versions in the Uno Templates. Depending on which version of the templates this may have been called `{YourProject}.Base` or `{YourProject}.Shared`. With the introduction of the global.json you may want to update the Sdk in the csproj to remove the version and use the version provided by the global.json instead. This makes it easier to manage your Sdk versions centrally.

## Using the Uno.Sdk

Modern .NET Projects use a Sdk standard. By default your project's typically will target the `Microsoft.NET.Sdk`. Some projects such as the Wasm and Server projects in the Uno Templates will target `Microsoft.NET.Sdk.Web`. As previously mentioned the `.Shared` project targets the `Microsoft.Build.NoTargets` Sdk. The difference between these Sdk's is the `Microsoft.Build.NoTargets` much like the `Uno.Sdk` are shipped and versioned on NuGet.org while the first two are shipped and installed as part of the larger .NET Sdk/Runtime itself.

To update your projects you simply need to replace `Microsoft.NET.Sdk` with `Uno.Sdk`. You will do this in the projects that you would reference Uno from. As an example let's say that you called your project `AwesomeProject`, the following projects would need to have this updated:

- AwesomeProject
- AwesomeProject.Mobile
- AwesomeProject.Skia.Gtk
- AwesomeProject.Skia.Linux.FrameBuffer
- AwesomeProject.Skia.WPF
- AwesomeProject.Windows

```xml
<Project Sdk="Uno.Sdk">
```

We will also need to update the `AwesomeProject.Wasm` project however this needs to be done slightly differently currently. Both `Uno.Sdk` and `Microsoft.NET.Sdk.Web` will actually load the `Microsoft.NET.Sdk`. However `Microsoft.NET.Sdk.Web` does not currently check to see if `Microsoft.NET.Sdk` has already been loaded. As a result you will need to do the following:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <Sdk Name="Uno.Sdk" />
```

## Uno Version

The core libraries that ship from the main Uno repo (these are versioned together with Uno.WinUI), should now be controlled by the version of the Uno.Sdk that you are using. If you are using Central Package Management (there is a `Directory.Packages.props` in your solution) this will be very easy as you simply need to open the `Directory.Packages.props`. If you are not using Central Package Management you will need to open the various csproj files that have a Package Reference to one of the Uno NuGet's. You should do a Find/Replace for the version of Uno that you are using and replace it with `$(UnoVersion)`. The `$(UnoVersion)` property is set by the Uno.Sdk. To update Uno in the future simply update the version of the `Uno.Sdk` that you are targeting in the global.json as shown above.

```xml
<PackageVersion Include="Uno.UI.Adapter.Microsoft.Extensions.Logging" Version="$(UnoVersion)" />
<PackageVersion Include="Uno.WinUI" Version="$(UnoVersion)" />
<PackageVersion Include="Uno.WinUI.Lottie" Version="$(UnoVersion)" />
<PackageVersion Include="Uno.WinUI.DevServer" Version="$(UnoVersion)" />
<PackageVersion Include="Uno.WinUI.Skia.Gtk" Version="$(UnoVersion)" />
<PackageVersion Include="Uno.WinUI.Skia.Linux.FrameBuffer" Version="$(UnoVersion)" />
<PackageVersion Include="Uno.WinUI.Skia.Wpf" Version="$(UnoVersion)" />
<PackageVersion Include="Uno.WinUI.WebAssembly" Version="$(UnoVersion)" />
```

## Cleaning up the Directory.Build.props

There are several properties defined in the top of the `Directory.Build.props`. The values shown below are the default values from the previous templates. If these are still the values you have in your Directory.Build.props you can remove them.

```xml
<PropertyGroup>
  <DebugType>portable</DebugType>
  <DebugSymbols>True</DebugSymbols>

  <DefaultLanguage>en</DefaultLanguage>
  <IsAndroid>false</IsAndroid>
  <IsIOS>false</IsIOS>
  <IsMac>false</IsMac>
  <IsMacCatalyst>false</IsMacCatalyst>
  <IsWinAppSdk>false</IsWinAppSdk>
</PropertyGroup>
```

> [!NOTE]
> If you would like to continue to use the `Is{Platform}` properties in your project you should leave these in your Directory.Build.props along with the Choose/When block to set them when they are true.

Beneath the PropertyGroup is a Choose block with conditions for various TargetFramework's. This can be entirely removed with one exception. By default the Sdk will provide the following values for you automatically setting the SupportedOSPlatformVersion and also the TargetPlatformMinVersion on Windows. If these values differ for your project be sure to keep your overrides. Any ItemGroup within the conditional blocks can be removed along with any other properties that are defined there.

| Target | SupportedOSPlatformVersion |
|--------|----------------------------|
| Android | 21 |
| iOS | 14.2 |
| macOS | 10.14 |
| MacCatalyst | 14.0 |
| WinUI | 10.0.18362.0 |


Lastly the [solution-config.props](xref:Build.Solution.TargetFramework-override) file is now automatically located and loaded for you if it exists. Be sure to remove the Import at the bottom of the `Directory.Build.props`.

## Cleaning up the Directory.Build.targets

The Directory.Build.targets in the Uno.Templates has only had a small block to remove native Platform Using's (shown below). This is now safe to remove as it is done directly in the Uno.Sdk.

```xml
<ItemGroup>
  <!-- Removes native usings to avoid Ambiguous reference -->
  <Using Remove="@(Using->HasMetadata('Platform'))" />
</ItemGroup>
```

## Cleaning up the Common Shared project

The Uno.Sdk contains a number of Default Items to further reduce the clutter required inside of your projects. To start you can remove the block that replicates the same default includes that come from the Windows App Sdk.

```xml
<!-- Include all images by default - matches the __WindowsAppSdkDefaultImageIncludes property in the WindowsAppSDK -->
<Content Include="Assets\**;**/*.png;**/*.bmp;**/*.jpg;**/*.dds;**/*.tif;**/*.tga;**/*.gif" Exclude="bin\**;obj\**;**\*.svg" />
<Page Include="**\*.xaml" Exclude="bin\**\*.xaml;obj\**\*.xaml" />
<Compile Update="**\*.xaml.cs">
    <DependentUpon>%(Filename)</DependentUpon>
</Compile>
<PRIResource Include="**\*.resw" />
```

Towards the bottom of your csproj you should also see the following items which should be removed.

```xml
<UnoImage Include="Assets\**\*.svg" />
<UpToDateCheckInput Include="**\*.xaml" Exclude="bin\**\*.xaml;obj\**\*.xaml" />
```

If you have a Windows target you will additionally want to update the Choose block to remove the `$(IsWinAppSdk)`

```xml
<When Condition="$(TargetFramework.Contains('windows10'))">
```

## Cleanup up the Mobile project

The `Uno.Sdk` now provides defaults for the `AndroidManifest` which make it unnecessary to provide the value in the Mobile project head. As a result you can remove this property.

```xml
<AndroidManifest>Android\AndroidManifest.xml</AndroidManifest>
```

After removing the Choose block in the Directory.Build.props you will find that the variables defined there are no longer available. We are working to restore these earlier in the build process however currently you would need to update the Choose block in the Mobile project to use the more verbose MSBuild lookup for the Target Platform Identifier like:

```xml
<When Condition=" $([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android' ">
</When>
<When Condition=" $([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios' ">
</When>
<When Condition=" $([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'maccatalyst' ">
</When>
```

## Cleaning up the Windows project

To start you can clean up the Windows project by removing any of the following properties. These are provided for you by default with the `Uno.Sdk`. If you have customized them in any way you should keep these properties to override the default behavior provided by the `Uno.Sdk`.

```xml
<ApplicationManifest>app.manifest</ApplicationManifest>
<RuntimeIdentifiers>win-x86;win-x64;win-arm64</RuntimeIdentifiers>
<PublishProfile>win-$(Platform).pubxml</PublishProfile>
<UseWinUI>true</UseWinUI>
<EnableMsixTooling>true</EnableMsixTooling>
<EnableWindowsTargeting>true</EnableWindowsTargeting>
```

Next you can remove the following ItemGroup which is no longer needed:

```xml
<ItemGroup>
  <Content Include="Images\**" />
  <Manifest Include="$(ApplicationManifest)" />
</ItemGroup>
```

Finally towards the bottom of the Windows project you will find two blocks that contain work around's for Visual Studio. These can both be removed as they are provided for you as part of the `Uno.Sdk`.

```xml
<!--
  Defining the "Msix" ProjectCapability here allows the Single-project MSIX Packaging
  Tools extension to be activated for this project even if the Windows App SDK Nuget
  package has not yet been restored.
-->
<ItemGroup Condition="'$(DisableMsixProjectCapabilityAddedByProject)'!='true' and '$(EnableMsixTooling)'=='true'">
  <ProjectCapability Include="Msix"/>
</ItemGroup>

<!--
  Defining the "HasPackageAndPublishMenuAddedByProject" property here allows the Solution
  Explorer "Package and Publish" context menu entry to be enabled for this project even if
  the Windows App SDK Nuget package has not yet been restored.
-->
<PropertyGroup Condition="'$(DisableHasPackageAndPublishMenuAddedByProject)'!='true' and '$(EnableMsixTooling)'=='true'">
  <HasPackageAndPublishMenu>true</HasPackageAndPublishMenu>
</PropertyGroup>
```
