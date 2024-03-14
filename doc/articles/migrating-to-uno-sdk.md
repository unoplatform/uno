---
uid: Uno.Development.MigratingToUnoSdk
---
# Migrating Projects to Uno.Sdk

The [Uno.Sdk](https://www.nuget.org/packages/uno.sdk) provides several enhancements to further simplify Uno Projects and a single location for updating the Uno Platform core packages version in `global.json`.

> [!Important]
> Migrating to the Uno.Sdk is not required. Existing projects continue to be supported in Uno Platform 5.1 or later.

## What is the Uno.Sdk

The Uno.Sdk is designed to reduce the noise that you need in your projects, while still ensuring that you have the ability to customize the configuration to your needs.

This means that values such as the `SupportedOSPlatformVersion`, which have been in the template's `Directory.Build.props` in previous Uno Platform templates, now have a default value set to the minimum supported by Uno Platform.

If you need to be more restrictive for your specific project you can provide your overrides for the platform(s) that you need.

## Upgrading an existing project to Uno.Sdk

The following sections detail how to upgrade an existing project to use the Uno.Sdk to simplify project files. Those modifications are made in-place and it is strongly recommended to work on a source-controlled environment.

While following this guide, you can compare with a new empty project created using [our Wizard](xref:Uno.GetStarted.Wizard), or using [dotnet new templates](xref:Uno.GetStarted.dotnet-new).

### Create a global.json

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
> The Microsoft.Build.NoTargets Sdk has been used for the last several versions in the Uno Templates. Depending on which version of the templates this may have been called `{YourProject}.Base` or `{YourProject}.Shared`. With the introduction of the `global.json` file, you may want to update the `Sdk` in the csproj to remove the version and use the version provided by the `global.json` instead. This centralizes your Sdk versions.

### Using the Uno.Sdk

Modern .NET Projects use an Sdk, defined as on the root element of a `csproj` file. By default your project's will target typically the `Microsoft.NET.Sdk`.

Some projects such as the Wasm and Server projects in the Uno Templates will target `Microsoft.NET.Sdk.Web`. As previously mentioned the `.Shared` project targets the `Microsoft.Build.NoTargets` Sdk. The difference between these Sdk's is the `Microsoft.Build.NoTargets` much like the `Uno.Sdk` are shipped and versioned on NuGet.org while the first two are shipped and installed as part of the larger .NET Sdk/Runtime itself.

To update your projects, replace `Microsoft.NET.Sdk` (`Microsoft.NET.Sdk.Web` in the case of the Wasm head) with `Uno.Sdk`. You will do this in the projects that you would reference Uno from. As an example let's say that you called your project `AwesomeProject`, you would update the following projects:

- AwesomeProject
- AwesomeProject.Mobile
- AwesomeProject.Skia.Gtk
- AwesomeProject.Skia.Linux.FrameBuffer
- AwesomeProject.Skia.WPF
- AwesomeProject.Wasm
- AwesomeProject.Windows

```xml
<Project Sdk="Uno.Sdk">
```

### The UnoVersion Property

The core libraries that ship from the main Uno repo (these are versioned together with Uno.WinUI.*), can now be controlled by the version of the Uno.Sdk that you are using. This common version is available through the `$(UnoVersion)` property set by the Uno.Sdk.

> [!Note]
> The exception to this naming convention is the `Uno.UI.Adapter.Microsoft.Extensions.Logging` NuGet package, which can also use `$(UnoVersion)`.

If you are using Central Package Management (there is a `Directory.Packages.props` in your solution) this will be very easy as you simply need to open the `Directory.Packages.props`.

If you are not using Central Package Management you will need to open the various csproj files that have a Package Reference to one of the Uno NuGet's. You should do a Find/Replace for the version of Uno that you are using and replace it with the `$(UnoVersion)`.

To [update Uno Platform in the future](xref:Uno.Development.UpgradeUnoNuget) simply update the version of the `Uno.Sdk` that you are targeting in the `global.json` as shown above.

Here is an example on how the `Directory.Packages.props` should look like:

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

### Cleaning up the Directory.Build.props

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

Beneath the `PropertyGroup` is a `Choose` block with conditions for various `TargetFramework`'s. This can be entirely removed with one exception. By default the Sdk will provide the following values for you automatically setting the `SupportedOSPlatformVersion` and also the `TargetPlatformMinVersion` on Windows. If these values differ for your project be sure to keep your overrides. Any `ItemGroup` within the conditional blocks can be removed along with any other properties that are defined there.

| Target | SupportedOSPlatformVersion |
|--------|----------------------------|
| Android | 21 |
| iOS | 14.2 |
| macOS | 10.14 |
| MacCatalyst | 14.0 |
| WinUI | 10.0.18362.0 |

Lastly the [`solution-config.props`](xref:Build.Solution.TargetFramework-override) file is now automatically located and loaded for you if it exists. Be sure to remove the `Import` at the bottom of the `Directory.Build.props`.

### Cleaning up the Directory.Build.targets

The `Directory.Build.targets` in the Uno.Templates has only had a small block to remove native Platform Using's (shown below). This is now safe to remove as it is done directly in the Uno.Sdk.

```xml
<ItemGroup>
  <!-- Removes native usings to avoid Ambiguous reference -->
  <Using Remove="@(Using->HasMetadata('Platform'))" />
</ItemGroup>
```

### Cleaning up the Common Shared project

The Uno.Sdk contains a number of Default Items to further reduce the clutter required inside of your projects. To start you can remove the block that replicates the same default includes that come from the Windows App Sdk:

```xml
<!-- Include all images by default - matches the __WindowsAppSdkDefaultImageIncludes property in the WindowsAppSDK -->
<Content Include="Assets\**;**\*.png;**\*.bmp;**\*.jpg;**\*.dds;**\*.tif;**\*.tga;**\*.gif" Exclude="bin\**;obj\**;**\*.svg" />
<Page Include="**\*.xaml" Exclude="bin\**\*.xaml;obj\**\*.xaml" />
<Compile Update="**\*.xaml.cs">
    <DependentUpon>%(Filename)</DependentUpon>
</Compile>
<PRIResource Include="**\*.resw" />
```

Towards the bottom of your csproj you should also see the following items which should be removed:

```xml
<UnoImage Include="Assets\**\*.svg" />
<UpToDateCheckInput Include="**\*.xaml" Exclude="bin\**\*.xaml;obj\**\*.xaml" />
```

If you have a Windows target you will additionally want to update the `Choose` block to remove the `$(IsWinAppSdk)`:

```xml
<When Condition="$(TargetFramework.Contains('windows10'))">
```

### Cleanup up the Mobile project

The `Uno.Sdk` now provides defaults for the `AndroidManifest` which make it unnecessary to provide the value in the Mobile project head. As a result you can remove this property.

```xml
<AndroidManifest>Android\AndroidManifest.xml</AndroidManifest>
```

After removing the `Choose` block in the `Directory.Build.props` you will find that the variables defined there are no longer available. You will need to update the `Choose` block in the Mobile project to use the more verbose MSBuild lookup for the Target Platform Identifier like:

```xml
<When Condition=" $([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android' ">
</When>
<When Condition=" $([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios' ">
</When>
<When Condition=" $([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'maccatalyst' ">
</When>
```

### Cleaning up the Windows project

To start you can clean up the Windows project by removing any of the following properties. These are provided for you by default with the `Uno.Sdk`. If you have customized them in any way you should keep these properties to override the default behavior provided by the `Uno.Sdk`:

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

## Customizing the behavior of the Uno.Sdk

### Disabling Default Items

As previously discussed, the `Uno.Sdk` will automatically includes files that you previously needed to manage within your projects. These default item's include definitions for including files within the `Content`, `Page`, abd `PRIResource` `ItemGroup`'s. Additionally if you have referenced the `Uno.Resizetizer` it will add default items for the `UnoImage` allowing you to more easily manage your image assets.

You may disable this behavior in one of two ways:

```xml
<PropertyGroup>
  <!-- Globally disable all default includes from the `Uno.Sdk`, `Microsoft.NET.Sdk` and if building on WASM `Microsoft.NET.Sdk.Web` -->
  <EnableDefaultItems>false</EnableDefaultItems>

  <!-- Disable only default items provided by the `Uno.Sdk` -->
  <EnableDefaultUnoItems>false</EnableDefaultUnoItems>
</PropertyGroup>
```

### Configure the `solution-config.props`

By default the `Uno.Sdk` will automatically import the `solution-config.props` if one exists. To disable this behavior you can set the following property and the `Uno.Sdk` will not import the `solution-config.props` file.

```xml
<PropertyGroup>
  <ImportSolutionConfigProps>false</ImportSolutionConfigProps>
</PropertyGroup>
```

To specify a specific location for the `solution-config.props` you can set the following property with the path to the file.

```xml
<PropertyGroup>
  <SolutionConfigPropsPath>path/to/solution-config.props</SolutionConfigPropsPath>
</Property>
```

> [!Note]
> If you specify the `SolutionConfigPropsPath`, it will still only be imported if the file exists which makes it safe to use with source control as it should not exist in CI and therefore would not be imported during a CI build.

### WinAppSdk PRIResource Workaround

Many Uno projects and libraries make use of a `winappsdk-workaround.targets` file that corrects a [bug](https://github.com/microsoft/microsoft-ui-xaml/issues/8857https://github.com/microsoft/microsoft-ui-xaml/issues/8857) found in WinUI. When using the `Uno.Sdk` these targets now are provided for you out of the box. This extra set of workaround targets can be disabled by setting the following property:

```xml
<PropertyGroup>
  <DisableWinUI8857_Workaround>true</DisableWinUI8857_Workaround>
</PropertyGroup>
```

## Cross Targeting Support

By Default when using the Uno.Sdk you get the added benefit of default includes for an easier time building Cross Targeted Applications. The supported file extensions are as shown below:

- *.crossruntime.cs (WASM, Skia, or Reference)
- *.wasm.cs
- *.skia.cs
- *.reference.cs
- *.iOS.cs (iOS & MacCatalyst)
- *.macOS.cs (MacOS not MacCatalyst)
- *.iOSmacOS.cs (iOS, MacCatalyst, & MacOS)
- *.Android.cs

As discussed above setting `EnableDefaultUnoItems` to false will disable these includes.
